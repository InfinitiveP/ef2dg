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

        // Флаг, гарантирующий, что трек запустится ровно один раз при входе в Play Mode
        private bool _startedInPlayMode = false;

        public string SoundClipsJson
        {
            get => JsonSerializer.Serialize(SoundClips);
            set { if (!string.IsNullOrEmpty(value)) SoundClips = JsonSerializer.Deserialize<Dictionary<string, string>>(value) ?? new(); }
        }

        public override void Start()
        {
            // Оставляем пустым, так как в Editor Mode музыка при старте не нужна
        }

        public override void Update(float deltaTime)
        {
            // =========================================================
            // АВТОЗАПУСК ПРИ СТАРТЕ РЕЖИМА ИГРЫ
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

        // Позволяет сбросить триггер (например, если потребуется перезапустить сцену)
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