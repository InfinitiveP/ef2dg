using System;
using System.Linq;
using Silk.NET.Maths;

namespace ef2dg
{
    // Editor actions shared by the main menu, the Toolbar and hotkeys
    public static class EditorCommands
    {
        public static void TogglePlayMode(EditorContext ctx, EditorState state)
        {
            if (state.IsPlayMode)
            {
                state.IsPlayMode = false;
                // Restore the zoom we used in the editor
                ctx.GameCamera.Zoom = state.EditorCameraZoom;
            }
            else
            {
                state.IsPlayMode = true;
                // Remember the current editor zoom
                state.EditorCameraZoom = ctx.GameCamera.Zoom;
                // Apply the game zoom from the project settings
                ctx.GameCamera.Zoom = ProjectConfig.Data.DefaultCameraZoom;
            }
        }

        public static void NewScene(EditorContext ctx, EditorState state, Type sceneType)
        {
            var newScene = (Scene)Activator.CreateInstance(sceneType)!;
            // Block generation of the default player for new scenes
            if (newScene is GameScene gs) gs.IsLoadedFromFile = true;
            newScene.InstanceName = newScene.GetType().Name;
            state.OpenedScenes.Add(newScene);
            ctx.ChangeScene?.Invoke(newScene);
        }

        public static Type[] GetSceneTypes() => System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Scene)) && !t.IsAbstract)
            .ToArray();

        public static void CreateEmptyGameObject(EditorContext ctx, EditorState state)
        {
            var camera = ctx.GameCamera.Position;
            var newObj = new GameObject
            {
                Name = $"Entity {ctx.CurrentScene.Entities.Count}",
                Position = new Vector2D<float>(camera.X, camera.Y),
                ParentScene = ctx.CurrentScene
            };
            ctx.CurrentScene.Entities.Add(newObj);
            state.SelectedEntity = newObj;
        }

        public static void DeleteSelected(EditorContext ctx, EditorState state)
        {
            if (state.SelectedEntity == null) return;
            ctx.CurrentScene.Entities.Remove(state.SelectedEntity);
            state.SelectedEntity = null;
        }
    }
}
