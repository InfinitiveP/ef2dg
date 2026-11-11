using System;
using System.Collections.Generic;
using System.Text.Json;
using Silk.NET.Maths;

namespace ef2dg
{
    public enum ParameterType { Float, Bool, Trigger }

    public class AnimatorParameter
    {
        public string Name { get; set; } = "";
        public ParameterType Type { get; set; } = ParameterType.Float;
        public float FloatValue { get; set; }
        public bool BoolValue { get; set; }
        public bool TriggerValue { get; set; }
    }

    public class AnimationTransition
    {
        public string TargetState { get; set; } = "";
        public string ParameterName { get; set; } = "";
        public string ConditionOperator { get; set; } = "==";
        public float Threshold { get; set; }
        public bool BoolExpected { get; set; }
    }

    public class AnimationState
    {
        public string TexturePath { get; set; } = "";
        public int FrameWidth { get; set; } = 0;
        public float AnimationSpeed { get; set; } = 10f;
        public bool IsLooping { get; set; } = true;
        public List<AnimationTransition> Transitions { get; set; } = new();
        public Dictionary<int, string> SoundEvents { get; set; } = new();
    }

    public class SpriteComponent : Component
    {
        public bool FlipX { get; set; } = false;

        public Texture Texture { get; private set; } = null!;
        public int CurrentFrame { get; private set; }
        public bool IsAnimationFinished { get; private set; }
        
        public int FrameWidth { get; private set; }
        public float AnimationSpeed { get; private set; }
        public bool IsLooping { get; private set; }
        public Vector2D<float> ActionHitboxSize { get; set; } = new Vector2D<float>(0.2f, 0.3f);
        public Vector2D<float> ActionHitboxOffset { get; set; } = new Vector2D<float>(0.2f, 0.0f);
        public int ActionTriggerFrame { get; set; } = 3;

        private float _frameTimer = 0f;
        private string _currentAnimationName = "";

        private int _lastSoundFrame = -1;
        
        // Public access to the current animation name for the controller
        public string CurrentAnimationName => _currentAnimationName;

        public int TotalFrames => Texture != null && FrameWidth > 0 ? Texture.Width / FrameWidth : 1;

        public Dictionary<string, AnimationState> Animations { get; set; } = new();
        public Dictionary<string, AnimatorParameter> Parameters { get; set; } = new();

        public string AnimationsJson
        {
            get => JsonSerializer.Serialize(Animations);
            set { if (!string.IsNullOrEmpty(value)) Animations = JsonSerializer.Deserialize<Dictionary<string, AnimationState>>(value) ?? new(); }
        }

        public string ParametersJson
        {
            get => JsonSerializer.Serialize(Parameters);
            set { if (!string.IsNullOrEmpty(value)) Parameters = JsonSerializer.Deserialize<Dictionary<string, AnimatorParameter>>(value) ?? new(); }
        }

        public SpriteComponent() { }

        // --- DELAY FIX: check transitions immediately ---
        public void SetFloat(string name, float value)
        {
            if (!Parameters.ContainsKey(name)) Parameters[name] = new AnimatorParameter { Name = name, Type = ParameterType.Float };
            Parameters[name].FloatValue = value;
            CheckTransitions();
        }

        public void SetBool(string name, bool value)
        {
            if (!Parameters.ContainsKey(name)) Parameters[name] = new AnimatorParameter { Name = name, Type = ParameterType.Bool };
            Parameters[name].BoolValue = value;
            CheckTransitions();
        }

        public void SetTrigger(string name)
        {
            if (!Parameters.ContainsKey(name)) Parameters[name] = new AnimatorParameter { Name = name, Type = ParameterType.Trigger };
            Parameters[name].TriggerValue = true;
            CheckTransitions(); // The animator reacts INSTANTLY
        }

        public void Play(string name, bool force = false)
        {
            if (!force && _currentAnimationName == name) return;
            if (!Animations.TryGetValue(name, out var state)) return;
            if (string.IsNullOrEmpty(state.TexturePath)) return;

            Texture = Engine.Resources.GetTexture(state.TexturePath);
            FrameWidth = state.FrameWidth > 0 ? state.FrameWidth : Texture.Width;
            AnimationSpeed = state.AnimationSpeed > 0 ? state.AnimationSpeed : 10f;
            IsLooping = state.IsLooping;

            CurrentFrame = 0;
            _frameTimer = 0f;
            _currentAnimationName = name;
            IsAnimationFinished = false;
            
            _lastSoundFrame = -1; // Fix: reset the sound tracker on a new animation
        }

        public override void Update(float deltaTime)
        {
            CheckTransitions();

            if (IsAnimationFinished && !IsLooping) return;

            // =========================================================
            // SOUND EVENT CHECK FOR THE CURRENT FRAME
            // =========================================================
            if (CurrentFrame != _lastSoundFrame && Animations.TryGetValue(_currentAnimationName, out var state))
            {
                _lastSoundFrame = CurrentFrame;

                // If a sound is bound to the current frame in this animation's settings
                if (state.SoundEvents.TryGetValue(CurrentFrame, out var soundKey))
                {
                    // Find the AudioSource on the same object and ask it to play
                    var audioSource = Owner?.GetComponent<AudioSourceComponent>();
                    audioSource?.PlayOneShot(soundKey);
                }
            }
            // =========================================================

            IsAnimationFinished = false;
            if (TotalFrames <= 1 || AnimationSpeed <= 0) return;

            _frameTimer += deltaTime;
            if (_frameTimer >= 1f / AnimationSpeed)
            {
                CurrentFrame++;
                if (CurrentFrame >= TotalFrames)
                {
                    if (IsLooping) CurrentFrame = 0;
                    else CurrentFrame = TotalFrames - 1;
                    
                    IsAnimationFinished = true;
                    _lastSoundFrame = -1; // Reset the tracker when looping
                }
                _frameTimer = 0f;
            }
        }

        private void CheckTransitions()
        {
            if (!Animations.TryGetValue(_currentAnimationName, out var currentState)) return;

            foreach (var transition in currentState.Transitions)
            {
                if (CheckCondition(transition))
                {
                    if (Parameters.TryGetValue(transition.ParameterName, out var p) && p.Type == ParameterType.Trigger)
                    {
                        p.TriggerValue = false;
                    }
                    
                    Play(transition.TargetState);
                    break;
                }
            }
        }

        private bool CheckCondition(AnimationTransition t)
        {
            if (t.ParameterName == "ExitTime") return IsAnimationFinished;

            if (!Parameters.TryGetValue(t.ParameterName, out var p)) return false;

            if (p.Type == ParameterType.Float)
            {
                return t.ConditionOperator switch
                {
                    ">" => p.FloatValue > t.Threshold,
                    "<" => p.FloatValue < t.Threshold,
                    ">=" => p.FloatValue >= t.Threshold,
                    "<=" => p.FloatValue <= t.Threshold,
                    "==" => MathF.Abs(p.FloatValue - t.Threshold) < 0.001f,
                    "!=" => MathF.Abs(p.FloatValue - t.Threshold) >= 0.001f,
                    _ => false
                };
            }
            else if (p.Type == ParameterType.Bool)
            {
                return t.ConditionOperator switch
                {
                    "==" => p.BoolValue == t.BoolExpected,
                    "!=" => p.BoolValue != t.BoolExpected,
                    _ => false
                };
            }
            else if (p.Type == ParameterType.Trigger)
            {
                return p.TriggerValue;
            }

            return false;
        }

        public override void Draw(SpriteRenderer renderer, Camera camera)
        {
            if (Texture == null || Owner == null) return;
            Vector2D<float> pos = Owner.Position;
            Vector2D<float> scale = Owner.Scale;
            float rot = Owner.Rotation;

            float uvWidth = (float)FrameWidth / Texture.Width;
            float uvOffsetX = CurrentFrame * uvWidth;
            Vector2D<float> uvOffset = new Vector2D<float>(uvOffsetX, 0.0f);
            Vector2D<float> uvScale = new Vector2D<float>(uvWidth, 1.0f);
            
            renderer.Draw(Texture, pos, scale, rot, camera, uvOffset, uvScale, FlipX);
        }
    }
}