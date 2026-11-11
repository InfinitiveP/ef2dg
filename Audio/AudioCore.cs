using Silk.NET.Maths;

namespace ef2dg
{
    public struct AudioCommand
    {
        public string SoundId;
        public Vector2D<float> WorldPosition;
        public float Volume;
        public bool IsMusic;
    }

    public interface IAudioService
    {
        void Init();
        void PlaySFX(string soundId, Vector2D<float> position, float volume = 1.0f);
        void PlayMusic(string musicPath, float volume = 0.5f);
        void StopMusic(); 
        
        // ИСПРАВЛЕНИЕ: Добавляем контракт принудительной остановки в интерфейс
        void StopAll();   

        void PauseAll();  
        void ResumeAll(); 
        void ProcessFrame(Vector2D<float> listenerPosition);
        void Dispose();
    }

    public class NullAudioService : IAudioService
    {
        public void Init() { }
        public void PlaySFX(string soundId, Vector2D<float> position, float volume = 1.0f) { }
        public void PlayMusic(string musicPath, float volume = 0.5f) { }
        public void StopMusic() { }
        
        // Безопасная заглушка для Null-сервиса
        public void StopAll() { } 

        public void PauseAll() { }  
        public void ResumeAll() { } 
        public void ProcessFrame(Vector2D<float> listenerPosition) { }
        public void Dispose() { }
    }

    public static class AudioLocator
    {
        private static IAudioService _service = new NullAudioService();
        public static IAudioService Get() => _service;
        public static void Provide(IAudioService service) => _service = service;
    }
}