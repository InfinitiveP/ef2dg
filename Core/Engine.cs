using System;
using System.Collections.Generic;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using System.Text.Json;

namespace ef2dg
{
    public class Engine
    {
        private enum TransitionPhase { None, Capture, Out, In }

        public static Engine Instance { get; private set; } = null!;
        public static ResourceManager Resources { get; private set; } = null!;
        private IWindow _window = null!;
        private GL _gl = null!;
        private SpriteRenderer _renderer = null!;
        private ResourceManager _resources = null!;
        private Camera _camera = null!;
        private IInputContext _input = null!;
        private ImGuiController _imGuiController = null!;
        private EditorUI _editorUI = new EditorUI();
        private bool _wasPlayMode = false;
        private Framebuffer _framebuffer = null!;
        private Framebuffer _sceneFramebuffer = null!;
        private Framebuffer _transitionSnapshot = null!;
        private LightingRenderer _lighting = null!;
        // Время для мерцания света; идёт и в редакторе, чтобы анимацию было видно при настройке
        private float _renderTime;

        private TransitionPhase _transitionPhase = TransitionPhase.None;
        private TransitionSettings _activeTransition = new TransitionSettings();
        private Action? _transitionAction;
        private float _transitionElapsed;
        private bool _resumeAudioAfterTransition;

        public bool IsTransitionActive => _transitionPhase != TransitionPhase.None;

        // =========================================================
        // АРХИТЕКТУРА СТЕКА СЦЕН И КЭШИРОВАНИЯ РЕДАКТОРА
        // =========================================================
        private Stack<Scene> _sceneStack = new Stack<Scene>();
        
        // Персистентная ссылка, гарантирующая сохранение прогресса редактирования GameScene
        private HashSet<Scene> _initializedScenes = new HashSet<Scene>();

        // Буферы отложенных команд для безопасного переключения в конце кадра
        private Scene? _pendingSwitchScene = null;
        private Scene? _pendingPushScene = null;
        private bool _pendingPopScene = false;

        // Флаг для автоматического возврата в Play Mode после завершения смены сцены
        private bool _pendingResumePlayMode = false;

        // Активная сцена для логики (всегда лежит на вершине стека)
        public Scene CurrentScene => _sceneStack.Count > 0 ? _sceneStack.Peek() : null!;

        private int _frames = 0;
        private double _fpsTimer = 0.0;
        private const float FixedTimeStep = 1f / 60f;
        private float _physicsAccumulator = 0f;

        public Engine()
        {
            Instance = this;
        }

        public void Run()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(1280, 720);
            options.Title = "ef2dg Engine";
            options.API = new GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, new APIVersion(3, 3));

            _window = Window.Create(options);
            _window.Load += OnLoad;
            _window.Update += OnUpdate;
            _window.Render += OnRender;
            _window.FramebufferResize += OnResize;
            _window.Closing += OnClose;
            
            _window.Run();
        }
        public void ChangeScene(Scene newScene)
        {
            if (IsTransitionActive) return;
            // Если смена инициирована во время игры, ставим флаг на автовозврат
            if (_editorUI.State.IsPlayMode) _pendingResumePlayMode = true;
            
            // ЗАЩИТА: Выходим из режима игры для безопасного кэширования
            _editorUI.State.IsPlayMode = false;
            _pendingSwitchScene = newScene;
        }

        public void PushScene(Scene newScene)
        {
            if (IsTransitionActive) return;
            if (_editorUI.State.IsPlayMode) _pendingResumePlayMode = true;
            
            _editorUI.State.IsPlayMode = false;
            _pendingPushScene = newScene;
        }

        public void PopScene()
        {
            if (IsTransitionActive) return;
            if (_editorUI.State.IsPlayMode) _pendingResumePlayMode = true;
            
            _editorUI.State.IsPlayMode = false;
            _pendingPopScene = true;
        }

        public void ChangeScene(Scene newScene, TransitionSettings settings)
        {
            if (settings == null || settings.Kind == TransitionKind.Instant || settings.Duration <= 0f)
            {
                ChangeScene(newScene);
                return;
            }

            RunTransition(() => ApplySceneChange(newScene), settings);
        }

        public void PushScene(Scene newScene, TransitionSettings settings)
        {
            if (settings == null || settings.Kind == TransitionKind.Instant || settings.Duration <= 0f)
            {
                PushScene(newScene);
                return;
            }

            RunTransition(() => ApplyScenePush(newScene), settings);
        }

        public void PopScene(TransitionSettings settings)
        {
            if (settings == null || settings.Kind == TransitionKind.Instant || settings.Duration <= 0f)
            {
                PopScene();
                return;
            }

            RunTransition(ApplyScenePop, settings);
        }

        public bool RunTransition(Action action, TransitionSettings? settings = null)
        {
            if (action == null || IsTransitionActive || _pendingSwitchScene != null || _pendingPushScene != null || _pendingPopScene) return false;

            var requested = settings?.Clone() ?? new TransitionSettings();
            if (requested.Kind == TransitionKind.Instant || requested.Duration <= 0f)
            {
                action();
                return true;
            }

            requested.Duration = MathF.Max(0.05f, requested.Duration);
            _activeTransition = requested;
            _transitionAction = action;
            _transitionElapsed = 0f;
            _transitionPhase = TransitionPhase.Capture;
            _resumeAudioAfterTransition = _editorUI.State.IsPlayMode;
            _physicsAccumulator = 0f;

            if (_resumeAudioAfterTransition) AudioLocator.Get().PauseAll();
            return true;
        }

        public bool RequestSceneTransition(string targetSceneName, bool isOverlay, TransitionSettings? settings = null)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName) || IsTransitionActive) return false;

            var requested = settings?.Clone() ?? new TransitionSettings();
            if (requested.Kind == TransitionKind.Instant || requested.Duration <= 0f)
            {
                Scene? immediateTarget = ResolveScene(targetSceneName);
                if (immediateTarget == null) return false;
                if (isOverlay) PushScene(immediateTarget);
                else ChangeScene(immediateTarget);
                return true;
            }

            Action action = () =>
            {
                Scene? target = ResolveScene(targetSceneName);
                if (target == null) return;
                if (isOverlay) ApplyScenePush(target);
                else ApplySceneChange(target);
            };

            return RunTransition(action, requested);
        }

        private void ApplySceneChange(Scene newScene)
        {
            _sceneStack.Clear();

            if (!_initializedScenes.Contains(newScene))
            {
                newScene.Load(_resources, _input);
                _initializedScenes.Add(newScene);
            }

            _sceneStack.Push(newScene);
            _editorUI.State.SelectedEntity = null;
            _editorUI.State.HoveredGridCell = null;

            string sceneName = string.IsNullOrEmpty(newScene.InstanceName) ? newScene.GetType().Name : newScene.InstanceName;
            Console.WriteLine($"[Engine] Switched context to instance: {sceneName}");
        }

        private void ApplyScenePush(Scene newScene)
        {
            if (!_initializedScenes.Contains(newScene))
            {
                newScene.Load(_resources, _input);
                _initializedScenes.Add(newScene);
            }

            _sceneStack.Push(newScene);
            _editorUI.State.SelectedEntity = null;
            _editorUI.State.HoveredGridCell = null;
            Console.WriteLine($"[Engine] Pushed layer context: {CurrentScene?.GetType().Name}");
        }

        private void ApplyScenePop()
        {
            if (_sceneStack.Count <= 1) return;

            _sceneStack.Pop();
            _editorUI.State.SelectedEntity = null;
            _editorUI.State.HoveredGridCell = null;
            Console.WriteLine($"[Engine] Popped top context. Returned active focus to: {CurrentScene?.GetType().Name}");
        }

        private void ResumePlayModeAfterImmediateSceneOperation()
        {
            if (!_pendingResumePlayMode) return;

            _editorUI.State.IsPlayMode = true;
            _pendingResumePlayMode = false;
            Console.WriteLine("[Engine] Automatically resumed Play Mode after scene operation.");
        }

        private Scene? ResolveScene(string targetSceneName)
        {
            string targetPath = targetSceneName;
            if (!targetPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(targetPath + ".json"))
            {
                targetPath += ".json";
            }

            if (File.Exists(targetPath))
            {
                try
                {
                    string json = File.ReadAllText(targetPath);
                    using var document = JsonDocument.Parse(json);
                    string sceneTypeName = document.RootElement.TryGetProperty("SceneType", out var sceneTypeProperty)
                        ? sceneTypeProperty.GetString() ?? "GameScene"
                        : "GameScene";
                    string fileName = Path.GetFileNameWithoutExtension(targetPath);
                    string instanceName = document.RootElement.TryGetProperty("InstanceName", out var instanceNameProperty)
                        ? instanceNameProperty.GetString() ?? fileName
                        : fileName;

                    Type? sceneType = Type.GetType($"ef2dg.{sceneTypeName}");
                    Scene scene = sceneType != null && sceneType.IsSubclassOf(typeof(Scene))
                        ? (Scene)Activator.CreateInstance(sceneType)!
                        : new GameScene();

                    if (scene is GameScene gameScene) gameScene.IsLoadedFromFile = true;
                    scene.InstanceName = instanceName;
                    scene.Load(_resources, _input);
                    if (scene.Map == null) scene.Map = new Tilemap(0.5f);

                    if (!SceneSerializer.Load(scene, targetPath, _resources))
                    {
                        Console.WriteLine($"[Transition ERROR] Failed to load scene file: {targetPath}");
                        return null;
                    }
                    _initializedScenes.Add(scene);

                    int existingIndex = _editorUI.State.OpenedScenes.FindIndex(openScene =>
                        openScene.InstanceName == instanceName ||
                        openScene.InstanceName == fileName);
                    if (existingIndex >= 0) _editorUI.State.OpenedScenes[existingIndex] = scene;
                    else _editorUI.State.OpenedScenes.Add(scene);

                    return scene;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Transition ERROR] Failed to parse scene '{targetPath}': {ex.Message}");
                    return null;
                }
            }

            Scene? existingScene = _editorUI.State.OpenedScenes.FirstOrDefault(scene =>
                scene.InstanceName == targetSceneName || scene.GetType().Name == targetSceneName);
            if (existingScene != null) return existingScene;

            Type? targetType = Type.GetType($"ef2dg.{targetSceneName}");
            if (targetType == null || !targetType.IsSubclassOf(typeof(Scene)))
            {
                Console.WriteLine($"[Transition ERROR] Target scene '{targetSceneName}' was not found.");
                return null;
            }

            Scene createdScene = (Scene)Activator.CreateInstance(targetType)!;
            createdScene.InstanceName = targetSceneName;
            _editorUI.State.OpenedScenes.Add(createdScene);
            return createdScene;
        }

        private void OnLoad()
        {
            ConsoleWindow.Interceptor.Init();
            ProjectConfig.Load();
            _gl = _window.CreateOpenGL();
            _gl.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // =========================================================
            // ИНТЕГРАЦИЯ ИМПОРТА АССЕТОВ (DRAG-AND-DROP)
            // =========================================================
            _window.FileDrop += (string[] droppedFiles) =>
            {
                // Проверяем, какая папка сейчас открыта в UI редактора
                string targetFolder = _editorUI.State.CurrentAssetFolder;
                if (!Directory.Exists(targetFolder)) return;

                foreach (var originalPath in droppedFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(originalPath);
                        string destinationPath = Path.Combine(targetFolder, fileName);
                        
                        // Копируем файл с заменой, если он уже существует
                        File.Copy(originalPath, destinationPath, true);
                        Console.WriteLine($"[AssetImport] Successfully imported: {fileName} -> {targetFolder}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AssetImport ERROR] Failed to import file: {ex.Message}");
                    }
                }
            };

            _camera = new Camera(_window.Size.X, _window.Size.Y);
            _input = _window.CreateInput();
            
            if (_input.Keyboards.Count > 0)
            {
                var keyboard = _input.Keyboards[0];
                keyboard.KeyDown += KeyDown; 
            }

            _renderer = new SpriteRenderer(_gl);
            _lighting = new LightingRenderer(_gl);
            Resources = new ResourceManager(_gl);
            _resources = Resources;

            var audioEngine = new AudioEngine();
            audioEngine.Init();
            AudioLocator.Provide(audioEngine);

            // Инициализируем стартовый уровень как базовый кэш редактора
            var startScene = new GameScene();
            startScene.Load(_resources, _input);
            _sceneStack.Push(startScene);
            // Запоминаем конкретный объект сцены как уже инициализированный
            _initializedScenes.Add(startScene);

            _initializedScenes.Add(startScene);

            // Создаем контроллер ImGui и передаем делегат ConfigureIO четвертым параметром
            _imGuiController = new ImGuiController(_gl, _window, _input, ImGuiConfigurator.ConfigureIO);

            _framebuffer = new Framebuffer(_gl, 800, 600);
            _sceneFramebuffer = new Framebuffer(_gl, 800, 600);
            _transitionSnapshot = new Framebuffer(_gl, 800, 600);
        }

        private void OnUpdate(double deltaTime)
        {
            // Escape движок не перехватывает: в Play Mode он свободен для биндов игры, выход — File → Exit
            if (!_editorUI.IsPlayMode && _wasPlayMode)
            {
                AudioLocator.Get().PauseAll();
                Console.WriteLine("[Engine] Play Mode suspended. Audio paused.");
            }
            else if (_editorUI.IsPlayMode && !_wasPlayMode)
            {
                AudioLocator.Get().ResumeAll();
                Console.WriteLine("[Engine] Play Mode active. Audio continuing.");
            }

            _wasPlayMode = _editorUI.IsPlayMode;
            DebugSettings.IsEditorMode = !_editorUI.IsPlayMode;
            _imGuiController.Update((float)deltaTime);
            _editorUI.UpdateRuntimePointer(_camera);

            // Логика и физика обрабатываются ТОЛЬКО для активной сцены на вершине стека
            if (_editorUI.IsPlayMode && !IsTransitionActive)
            {
                if (CurrentScene != null) 
                    CurrentScene.ActiveCamera = _camera;

                _physicsAccumulator += (float)deltaTime;
                if (_physicsAccumulator > 0.25f) _physicsAccumulator = 0.25f;

                while (_physicsAccumulator >= FixedTimeStep && !IsTransitionActive)
                {
                    CurrentScene?.FixedUpdate(FixedTimeStep);
                    _physicsAccumulator -= FixedTimeStep;
                }

                if (!IsTransitionActive)
                {
                    CurrentScene?.Update((float)deltaTime);
                    AudioLocator.Get().ProcessFrame(new Vector2D<float>(_camera.Position.X, _camera.Position.Y));
                }
            }
            else if (!IsTransitionActive)
            {
                // Не двигаем камеру, пока печатают текст или жмут Ctrl+S / Ctrl+Z
                var io = ImGui.GetIO();
                if (_input.Keyboards.Count > 0 && !io.WantTextInput && !io.KeyCtrl)
                {
                    var kb = _input.Keyboards[0];
                    var camPos = _camera.Position;
                    float cameraSpeed = 5.0f * (float)deltaTime;

                    if (kb.IsKeyPressed(Key.W)) camPos.Y += cameraSpeed;
                    if (kb.IsKeyPressed(Key.S)) camPos.Y -= cameraSpeed;
                    if (kb.IsKeyPressed(Key.A)) camPos.X -= cameraSpeed;
                    if (kb.IsKeyPressed(Key.D)) camPos.X += cameraSpeed;

                    _camera.Position = camPos;
                }
            }

            if (DebugSettings.ShowPerformanceMonitor)
            {
                _fpsTimer += deltaTime;
                _frames++;
                if (_fpsTimer >= 1.0)
                {
                    double fps = _frames / _fpsTimer;
                    double frameTime = (_fpsTimer / _frames) * 1000.0; 
                    int entityCount = CurrentScene?.Entities.Count ?? 0;
                    _window.Title = $"ef2dg Engine | FPS: {fps:F0} | Frame: {frameTime:F2} ms | Draws: {_renderer.LastFrameDrawCalls} | Quads: {_renderer.LastFrameQuads} | Stack Depth: {_sceneStack.Count}";
                    _frames = 0;
                    _fpsTimer = 0.0;
                }
            }
            else
            {
                _window.Title = "ef2dg Engine";
                _frames = 0;
                _fpsTimer = 0.0;
            }

            // =========================================================
            // БЕЗОПАСНАЯ ФАЗА УПРАВЛЕНИЯ СТЕКОМ (Конец кадра)
            // =========================================================
            if (_pendingSwitchScene != null)
            {
                ApplySceneChange(_pendingSwitchScene);
                _pendingSwitchScene = null;
                ResumePlayModeAfterImmediateSceneOperation();
            }
            else if (_pendingPushScene != null)
            {
                ApplyScenePush(_pendingPushScene);
                _pendingPushScene = null;
                ResumePlayModeAfterImmediateSceneOperation();
            }
            else if (_pendingPopScene)
            {
                _pendingPopScene = false;
                ApplyScenePop();
                ResumePlayModeAfterImmediateSceneOperation();
            }
        }

        private void OnRender(double deltaTime)
        {
            _renderTime += (float)deltaTime;
            SynchronizeFramebuffers();
            _renderer.BeginFrame();
            AdvanceTransition((float)deltaTime);
            RenderSceneToFramebuffer();
            ComposeFinalFrame();
            _renderer.EndFrame();

            if (_transitionPhase == TransitionPhase.Capture)
            {
                _transitionSnapshot.CopyFrom(_framebuffer);
                _transitionPhase = TransitionPhase.Out;
                _transitionElapsed = 0f;
            }

            _framebuffer.Unbind(_window.Size.X, _window.Size.Y);

            _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            var ctx = new EditorContext
            {
                CurrentScene = CurrentScene!,
                Fbo = _framebuffer,
                GameCamera = _camera,
                WindowSize = _window.FramebufferSize,
                Resources = _resources,
                ChangeScene = ChangeScene,
                PushScene = PushScene,
                PopScene = PopScene,
                RequestSceneTransition = RequestSceneTransition,
                Exit = _window.Close
            };

            _editorUI.Draw(ctx);
            _imGuiController.Render();
        }

        private void SynchronizeFramebuffers()
        {
            int width = _framebuffer.Width;
            int height = _framebuffer.Height;
            if (_sceneFramebuffer.Width != width || _sceneFramebuffer.Height != height) _sceneFramebuffer.Resize(width, height);
            if (_transitionSnapshot.Width != width || _transitionSnapshot.Height != height) _transitionSnapshot.Resize(width, height);
        }

        private void AdvanceTransition(float deltaTime)
        {
            if (_transitionPhase != TransitionPhase.Out && _transitionPhase != TransitionPhase.In) return;

            float halfDuration = MathF.Max(0.025f, _activeTransition.Duration * 0.5f);
            _transitionElapsed += MathF.Max(0f, deltaTime);

            if (_transitionPhase == TransitionPhase.Out && _transitionElapsed >= halfDuration)
            {
                _transitionElapsed = 0f;
                try
                {
                    _transitionAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Transition ERROR] Action failed: {ex.Message}");
                }
                _transitionAction = null;
                _transitionPhase = TransitionPhase.In;
            }
            else if (_transitionPhase == TransitionPhase.In && _transitionElapsed >= halfDuration)
            {
                _transitionPhase = TransitionPhase.None;
                _transitionElapsed = 0f;
                if (_resumeAudioAfterTransition && _editorUI.State.IsPlayMode) AudioLocator.Get().ResumeAll();
                _resumeAudioAfterTransition = false;
            }
        }

        private float GetTransitionProgress()
        {
            float halfDuration = MathF.Max(0.025f, _activeTransition.Duration * 0.5f);
            return Math.Clamp(_transitionElapsed / halfDuration, 0f, 1f);
        }

        private void RenderSceneToFramebuffer()
        {
            _sceneFramebuffer.Bind();
            var background = ProjectConfig.Data.BackgroundColor;
            _gl.ClearColor(background.X, background.Y, background.Z, background.W);
            _gl.Clear(ClearBufferMask.ColorBufferBit);

            var stackArray = _sceneStack.ToArray();
            for (int i = stackArray.Length - 1; i >= 0; i--)
            {
                var scene = stackArray[i];
                if (scene == null) continue;

                scene.DrawWorld(_renderer, _camera);

                // Свет каждой сцены ложится на уже нарисованный мир, но не на гизмо и HUD
                if (scene.Lighting.Enabled && (_editorUI.IsPlayMode || DebugSettings.PreviewLightingInEditor))
                {
                    // Завершаем геометрию scene framebuffer до смены render target/blend state.
                    _renderer.FlushAll();
                    _lighting.Apply(scene, _sceneFramebuffer, _camera, _renderTime);
                }

                scene.DrawEditorOverlay(_renderer, _camera);
                scene.DrawUI(_renderer, _camera);
            }

            if (!_editorUI.IsPlayMode && CurrentScene != null)
            {
                if (_editorUI.State.HoveredGridCell is { } cell)
                    CurrentScene.Map?.DrawBrushHighlight(_renderer, _camera, cell, _editorUI.State.SelectedTilePath);

                LightingRenderer.DrawGizmos(CurrentScene, _camera, _renderer, _resources.WhiteTexture,
                    _editorUI.State.SelectedEntity, _sceneFramebuffer.Height);
            }

            _renderer.FlushAll();
        }

        private void ComposeFinalFrame()
        {
            _framebuffer.Bind();
            Vector4D<float> transitionColor = _activeTransition.Color;
            _gl.ClearColor(transitionColor.X, transitionColor.Y, transitionColor.Z, 1f);
            _gl.Clear(ClearBufferMask.ColorBufferBit);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            if (_transitionPhase == TransitionPhase.None || _transitionPhase == TransitionPhase.Capture)
            {
                DrawFramebuffer(_sceneFramebuffer, new Vector2D<float>(0f, 0f), 1f);
                return;
            }

            float progress = GetTransitionProgress();
            bool isOut = _transitionPhase == TransitionPhase.Out;

            switch (_activeTransition.Kind)
            {
                case TransitionKind.Fade:
                    DrawFramebuffer(_sceneFramebuffer, new Vector2D<float>(0f, 0f), 1f);
                    float overlayAlpha = isOut ? progress : 1f - progress;
                    DrawColorOverlay(transitionColor, overlayAlpha);
                    break;

                case TransitionKind.CrossFade:
                    if (isOut)
                    {
                        DrawFramebuffer(_transitionSnapshot, new Vector2D<float>(0f, 0f), 1f - progress * 0.5f);
                    }
                    else
                    {
                        DrawFramebuffer(_sceneFramebuffer, new Vector2D<float>(0f, 0f), progress);
                        DrawFramebuffer(_transitionSnapshot, new Vector2D<float>(0f, 0f), (1f - progress) * 0.5f);
                    }
                    break;

                case TransitionKind.SlideLeft:
                case TransitionKind.SlideRight:
                case TransitionKind.SlideUp:
                case TransitionKind.SlideDown:
                    DrawSlideTransition(progress, isOut);
                    break;

                default:
                    DrawFramebuffer(_sceneFramebuffer, new Vector2D<float>(0f, 0f), 1f);
                    break;
            }
        }

        private void DrawSlideTransition(float progress, bool isOut)
        {
            float width = _camera.ViewWidth;
            float height = _camera.ViewHeight;
            Vector2D<float> offset;

            switch (_activeTransition.Kind)
            {
                case TransitionKind.SlideLeft:
                    offset = new Vector2D<float>(isOut ? -progress * width : (1f - progress) * width, 0f);
                    break;
                case TransitionKind.SlideRight:
                    offset = new Vector2D<float>(isOut ? progress * width : -(1f - progress) * width, 0f);
                    break;
                case TransitionKind.SlideUp:
                    offset = new Vector2D<float>(0f, isOut ? progress * height : -(1f - progress) * height);
                    break;
                default:
                    offset = new Vector2D<float>(0f, isOut ? -progress * height : (1f - progress) * height);
                    break;
            }

            DrawFramebuffer(isOut ? _transitionSnapshot : _sceneFramebuffer, offset, 1f);
        }

        private void DrawFramebuffer(Framebuffer framebuffer, Vector2D<float> offset, float alpha)
        {
            var texture = new Texture(_gl, framebuffer.TextureId, framebuffer.Width, framebuffer.Height);
            var position = new Vector2D<float>(_camera.Position.X + offset.X, _camera.Position.Y + offset.Y);
            var size = new Vector2D<float>(_camera.ViewWidth, _camera.ViewHeight);
            _renderer.DrawColored(texture, position, size, 0f, _camera,
                new Vector2D<float>(0f, 1f), new Vector2D<float>(1f, -1f), false,
                new Vector4D<float>(1f, 1f, 1f, Math.Clamp(alpha, 0f, 1f)));
            _renderer.Flush();
        }

        private void DrawColorOverlay(Vector4D<float> color, float alpha)
        {
            var position = new Vector2D<float>(_camera.Position.X, _camera.Position.Y);
            var size = new Vector2D<float>(_camera.ViewWidth, _camera.ViewHeight);
            _renderer.DrawColored(_resources.WhiteTexture, position, size, 0f, _camera,
                new Vector2D<float>(0f, 0f), new Vector2D<float>(1f, 1f), false,
                new Vector4D<float>(color.X, color.Y, color.Z, Math.Clamp(alpha * color.W, 0f, 1f)));
            _renderer.Flush();
        }

        private void OnResize(Vector2D<int> newSize)
        {
            _gl.Viewport(newSize);
            _camera.UpdateSize(newSize.X, newSize.Y);
        }

        private void KeyDown(IKeyboard keyboard, Key key, int keyCode)
        {
            if (key == Key.F1) DebugSettings.ShowPlayerHitbox = !DebugSettings.ShowPlayerHitbox;
            if (key == Key.F2) DebugSettings.ShowEnemyHitbox = !DebugSettings.ShowEnemyHitbox;
            if (key == Key.F3) DebugSettings.ShowMapHitbox = !DebugSettings.ShowMapHitbox;
            if (key == Key.F4) DebugSettings.ShowAttackZone = !DebugSettings.ShowAttackZone;
            if (key == Key.F5) DebugSettings.ShowPerformanceMonitor = !DebugSettings.ShowPerformanceMonitor;
        }

        private void OnClose()
        {
            AudioLocator.Get().Dispose();
            _imGuiController?.Dispose();
            _framebuffer?.Dispose();
            _sceneFramebuffer?.Dispose();
            _transitionSnapshot?.Dispose();
            _lighting?.Dispose();
            _renderer.Dispose();
            _resources.Dispose();
            _input.Dispose();
            _gl?.Dispose();
        }
    }
}
