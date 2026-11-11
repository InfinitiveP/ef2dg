using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.Maths;
using Silk.NET.OpenAL;

namespace ef2dg
{
    public class AudioEngine : IAudioService
    {
        private AL _al = null!;
        private ALContext _alc = null!;
        private unsafe Device* _device;
        private unsafe Context* _context;

        private readonly Queue<AudioCommand> _sfxQueue = new Queue<AudioCommand>();
        private const int MaxDuplicatesPerFrame = 2;

        private readonly Dictionary<string, uint> _audioBuffers = new Dictionary<string, uint>();

        private readonly List<uint> _sourcesPool = new List<uint>();

        private uint _musicSource;
        private string _currentMusicPath = "";

        public unsafe void Init()
        {
            try
            {
                _alc = ALContext.GetApi();
                _al = AL.GetApi();

                if (_al == null || _alc == null)
                {
                    Console.WriteLine("[AudioEngine] CRITICAL: OpenAL API is null. Missing soft_oal.dll or openal32.dll!");
                    return;
                }

                _device = _alc.OpenDevice(""); 
                if (_device == null)
                {
                    Console.WriteLine("[AudioEngine] ERROR: Failed to open OpenAL hardware device (No sound card found).");
                    return;
                }

                _context = _alc.CreateContext(_device, null);
                _alc.MakeContextCurrent(_context);

                for (int i = 0; i < 16; i++)
                {
                    uint source = _al.GenSource();
                    _al.SetSourceProperty(source, SourceBoolean.SourceRelative, false);
                    _sourcesPool.Add(source);
                }

                _musicSource = _al.GenSource();
                _al.SetSourceProperty(_musicSource, SourceBoolean.SourceRelative, true);
                _al.SetSourceProperty(_musicSource, SourceVector3.Position, 0f, 0f, 0f);
                _al.SetSourceProperty(_musicSource, SourceBoolean.Looping, true);

                _al.DistanceModel(DistanceModel.LinearDistanceClamped);

                Console.WriteLine("[AudioEngine] OpenAL hardware pipeline initialized (16 SFX channels ready).");
            }
            catch (Exception ex)
            {
                // Теперь ошибка отсутствия DLL будет видна максимально четко
                Console.WriteLine($"[AudioEngine] CRITICAL OpenAL Native Init Error: {ex.Message}");
                _al = null!; // Гарантируем, что переменная сброшена
            }
        }

        private uint GetOrLoadBuffer(string path)
        {
            // БЕЗОПАСНОСТЬ: Если OpenAL не инициализировался, отсекаем загрузку сразу
            if (_al == null)
            {
                Console.WriteLine($"[AudioEngine] SKIP: Cannot load '{path}', OpenAL core is offline.");
                return 0;
            }

            if (_audioBuffers.TryGetValue(path, out uint existingBuffer))
            {
                return existingBuffer;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"[AudioEngine] ERROR: Audio file not found: {path}");
                return 0;
            }

            try
            {
                uint buffer = _al.GenBuffer();

                using (FileStream fs = File.OpenRead(path))
                using (BinaryReader reader = new BinaryReader(fs))
                {
                    string riff = new string(reader.ReadChars(4));
                    reader.ReadInt32(); 
                    string wave = new string(reader.ReadChars(4));
                    
                    if (riff != "RIFF" || wave != "WAVE")
                    {
                        Console.WriteLine($"[AudioEngine] ERROR: Invalid WAV header in file: {path}");
                        return 0;
                    }

                    BufferFormat format = BufferFormat.Mono16;
                    int sampleRate = 44100;
                    byte[] data = Array.Empty<byte>();

                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        string chunkId = new string(reader.ReadChars(4));
                        int chunkSize = reader.ReadInt32();

                        if (chunkId == "fmt ")
                        {
                            int audioFormat = reader.ReadInt16();
                            int channels = reader.ReadInt16();
                            sampleRate = reader.ReadInt32();
                            reader.ReadInt32(); 
                            reader.ReadInt16(); 
                            int bitsPerSample = reader.ReadInt16();

                            if (channels == 1 && bitsPerSample == 8) format = BufferFormat.Mono8;
                            else if (channels == 1 && bitsPerSample == 16) format = BufferFormat.Mono16;
                            else if (channels == 2 && bitsPerSample == 8) format = BufferFormat.Stereo8;
                            else if (channels == 2 && bitsPerSample == 16) format = BufferFormat.Stereo16;

                            if (chunkSize > 16) reader.BaseStream.Seek(chunkSize - 16, SeekOrigin.Current);
                        }
                        else if (chunkId == "data")
                        {
                            data = reader.ReadBytes(chunkSize);
                            break;
                        }
                        else
                        {
                            reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                        }
                    }

                    // Если чанк data оказался пустым или битым
                    if (data.Length == 0)
                    {
                        Console.WriteLine($"[AudioEngine] ERROR: No audio data found inside WAV: {path}");
                        return 0;
                    }

                    unsafe
                    {
                        fixed (byte* pData = data)
                        {
                            _al.BufferData(buffer, format, pData, data.Length, sampleRate);
                        }
                    }
                }

                _audioBuffers[path] = buffer;
                Console.WriteLine($"[AudioEngine] Loaded WAV buffer: {path}");
                return buffer;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AudioEngine] Error decoding WAV '{path}': {ex.Message}");
                return 0;
            }
        }

        public void PlaySFX(string soundId, Vector2D<float> position, float volume = 1.0f)
        {
            lock (_sfxQueue)
            {
                _sfxQueue.Enqueue(new AudioCommand
                {
                    SoundId = soundId,
                    WorldPosition = position,
                    Volume = volume,
                    IsMusic = false
                });
            }
        }

        public void PlayMusic(string musicPath, float volume = 0.5f)
        {
            if (_currentMusicPath == musicPath) return;

            uint buffer = GetOrLoadBuffer(musicPath);
            if (buffer == 0) return;

            _al.SourceStop(_musicSource);
            _al.SetSourceProperty(_musicSource, SourceInteger.Buffer, buffer);
            _al.SetSourceProperty(_musicSource, SourceFloat.Gain, volume);
            _al.SourcePlay(_musicSource);

            _currentMusicPath = musicPath;
            Console.WriteLine($"[AudioEngine] Playing OpenAL Music: {musicPath}");
        }
        public void StopMusic()
        {
            if (_al == null) return;

            // Аппаратно останавливаем выделенный музыкальный канал
            _al.SourceStop(_musicSource);
            _currentMusicPath = ""; // Сбрасываем путь, чтобы трек можно было запустить повторно
            
            Console.WriteLine("[AudioEngine] Stopped OpenAL Music.");
        }

        public void StopAll()
        {
            if (_al == null) return;

            // 1. Останавливаем фоновую музыку
            StopMusic();

            // 2. Используем .Count для перебора списка List<uint>
            for (int i = 0; i < _sourcesPool.Count; i++)
            {
                _al.SourceStop(_sourcesPool[i]);
            }

            Console.WriteLine("[AudioEngine] Explicitly stopped all SFX channels and Music.");
        }

        public void PauseAll()
        {
            if (_al == null) return;

            // 1. Паузим музыку, если она играла прямо сейчас
            _al.GetSourceProperty(_musicSource, GetSourceInteger.SourceState, out int mState);
            if ((SourceState)mState == SourceState.Playing)
            {
                _al.SourcePause(_musicSource);
            }

            // 2. Паузим все 16 каналов SFX (летящие фаерболы, крики, шаги)
            foreach (uint source in _sourcesPool)
            {
                _al.GetSourceProperty(source, GetSourceInteger.SourceState, out int sState);
                if ((SourceState)sState == SourceState.Playing)
                {
                    _al.SourcePause(source);
                }
            }

            Console.WriteLine("[AudioEngine] All audio channels paused.");
        }

        public void ResumeAll()
        {
            if (_al == null) return;

            // 1. Продолжаем музыку с той же миллисекунды
            _al.GetSourceProperty(_musicSource, GetSourceInteger.SourceState, out int mState);
            if ((SourceState)mState == SourceState.Paused)
            {
                _al.SourcePlay(_musicSource);
            }

            // 2. Продолжаем все недоигранные SFX эффекты
            foreach (uint source in _sourcesPool)
            {
                _al.GetSourceProperty(source, GetSourceInteger.SourceState, out int sState);
                if ((SourceState)sState == SourceState.Paused)
                {
                    _al.SourcePlay(source);
                }
            }

            Console.WriteLine("[AudioEngine] All audio channels resumed.");
        }

        public void ProcessFrame(Vector2D<float> listenerPosition)
        {
            if (_al == null) return;

            _al.SetListenerProperty(ListenerVector3.Position, listenerPosition.X, listenerPosition.Y, 3.0f);

            var soundCounts = new Dictionary<string, int>();

            lock (_sfxQueue)
            {
                while (_sfxQueue.Count > 0)
                {
                    var cmd = _sfxQueue.Dequeue();

                    if (!soundCounts.ContainsKey(cmd.SoundId)) soundCounts[cmd.SoundId] = 0;
                    soundCounts[cmd.SoundId]++;
                    if (soundCounts[cmd.SoundId] > MaxDuplicatesPerFrame) continue;

                    uint buffer = GetOrLoadBuffer(cmd.SoundId); // WAV -> VRAM
                    if (buffer == 0) continue;

                    uint availableSource = 0;
                    foreach (uint source in _sourcesPool)
                    {
                        _al.GetSourceProperty(source, GetSourceInteger.SourceState, out int state);
                        if ((SourceState)state != SourceState.Playing)
                        {
                            availableSource = source;
                            break;
                        }
                    }

                    if (availableSource == 0) continue; 

                    _al.SetSourceProperty(availableSource, SourceInteger.Buffer, buffer);
                    _al.SetSourceProperty(availableSource, SourceFloat.Gain, cmd.Volume);
                    
                    _al.SetSourceProperty(availableSource, SourceVector3.Position, cmd.WorldPosition.X, cmd.WorldPosition.Y, 0f);
                    
                    _al.SetSourceProperty(availableSource, SourceFloat.ReferenceDistance, 1.0f);
                    _al.SetSourceProperty(availableSource, SourceFloat.MaxDistance, 15.0f);

                    _al.SourcePlay(availableSource);

                    float dist = Vector2D.Distance(cmd.WorldPosition, listenerPosition);
                    Console.WriteLine($"[OpenAL Mixer] Play SFX: '{cmd.SoundId}' on Channel {availableSource} (Dist: {dist:F1})");
                }
            }
        }

        public unsafe void Dispose()
        {
            if (_al == null) return;

            _al.SourceStop(_musicSource);
            _al.DeleteSource(_musicSource);

            foreach (uint source in _sourcesPool)
            {
                _al.SourceStop(source);
                _al.DeleteSource(source);
            }

            // Удаляем буферы из памяти
            foreach (uint buffer in _audioBuffers.Values)
            {
                _al.DeleteBuffer(buffer);
            }

            // Уничтожаем контекст
            if (_context != null)
            {
                _alc.MakeContextCurrent(null);
                _alc.DestroyContext(_context);
            }
            if (_device != null)
            {
                _alc.CloseDevice(_device);
            }

            Console.WriteLine("[AudioEngine] OpenAL resources fully disposed.");
        }
    }
}