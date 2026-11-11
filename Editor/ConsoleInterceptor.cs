using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ef2dg
{
    public class ConsoleInterceptor : TextWriter
    {
        private TextWriter _originalOut = null!;
        private List<string> _logs = new List<string>();
        private StringBuilder _currentLine = new StringBuilder();
        private const int MaxLines = 1000; // Ограничиваем память (храним 1000 последних строк)

        public override Encoding Encoding => Encoding.UTF8;

        public List<string> Logs => _logs;

        public void Init()
        {
            _originalOut = Console.Out;
            // Перенаправляем весь системный вывод Console.Write/WriteLine в наш класс
            Console.SetOut(this);
            Console.WriteLine("[Console] Internal terminal initialized successfully.");
        }

        public override void Write(char value)
        {
            // Опционально: продолжаем писать и в системную консоль (удобно при дебаге из IDE)
            _originalOut?.Write(value);

            if (value == '\n')
            {
                // Используем lock для полной потокобезопасности
                lock (_logs)
                {
                    _logs.Add(_currentLine.ToString());
                    if (_logs.Count > MaxLines)
                    {
                        _logs.RemoveAt(0);
                    }
                }
                _currentLine.Clear();
            }
            else if (value != '\r')
            {
                _currentLine.Append(value);
            }
        }

        public override void Write(string? value)
        {
            if (value == null) return;
            _originalOut?.Write(value);

            foreach (char c in value)
            {
                if (c == '\n')
                {
                    lock (_logs)
                    {
                        _logs.Add(_currentLine.ToString());
                        if (_logs.Count > MaxLines)
                        {
                            _logs.RemoveAt(0);
                        }
                    }
                    _currentLine.Clear();
                }
                else if (c != '\r')
                {
                    _currentLine.Append(c);
                }
            }
        }

        public void Clear()
        {
            lock (_logs)
            {
                _logs.Clear();
                _currentLine.Clear();
            }
        }
    }
}