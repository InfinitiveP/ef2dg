using System.Collections.Generic;
using System.Text.Json;

namespace ef2dg
{
    public class AudioSourceComponent : Component
    {
        public float Volume { get; set; } = 1.0f;
        
        public bool PlayOnStart { get; set; } = false;
        public bool IsMusic { get; set; } = false;
        public string DefaultSoundPath { get; set; } = "";

        public Dictionary<string, string> SoundClips { get; set; } = new();

        // Flag that guarantees the track starts exactly once when entering Play Mode
        private bool _startedInPlayMode = false;

        public string SoundClipsJson
        {
            get => JsonSerializer.Serialize(SoundClips);
            set { if (!string.IsNullOrEmpty(value)) SoundClips = JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? new(); }
        }

        public override void Start()
        {
            // Left empty: music is not needed at startup in Editor Mode
        }

        public override void Update(float deltaTime)
        {
            // =========================================================
            // AUTOPLAY WHEN PLAY MODE STARTS
            // =========================================================
            if (PlayOnStart && !_startedInPlayMode && !string.IsNullOrEmpty(DefaultSoundPath))
            {
                _startedInPlayMode = true;

                if (IsMusic)
                {
                    AudioLocator.Get().PlayMusic(DefaultSoundPath, Volume);
                }
                else
                {
                    AudioLocator.Get().PlaySFX(DefaultSoundPath, Owner.Position, Volume);
                }
            }
        }

        // Resets the trigger (for example, if the scene needs to be restarted)
        public void ResetPlayState()
        {
            _startedInPlayMode = false;
        }

        public void PlayOneShot(string clipName)
        {
            if (SoundClips.TryGetValue(clipName, out var path) && !string.IsNullOrEmpty(path))
            {
                AudioLocator.Get().PlaySFX(path, Owner.Position, Volume);
            }
        }
    }
}