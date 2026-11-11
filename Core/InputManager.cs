using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Silk.NET.Input;
using Silk.NET.Maths;

namespace ef2dg
{
    public static class InputManager
    {
        private static IKeyboard _keyboard = null!;
        private static readonly StringBuilder TextInputBuffer = new();

        public static Vector2D<float> UiPointerPosition { get; private set; }
        public static Vector2D<float> WorldPointerPosition { get; private set; }
        public static bool IsUiPointerInside { get; private set; }
        public static bool IsUiPointerLeftDown { get; private set; }
        public static bool IsUiPointerRightDown { get; private set; }
        public static bool IsUiPointerMiddleDown { get; private set; }
        public static float UiPointerWheel { get; private set; }
        public static float UiPointerWheelHorizontal { get; private set; }
        
        // Dictionary: action name -> key
        public static Dictionary<string, Key> Bindings { get; set; } = new Dictionary<string, Key>();

        public static void Init(IKeyboard keyboard)
        {
            if (_keyboard != null) _keyboard.KeyChar -= OnKeyChar;
            _keyboard = keyboard;
            _keyboard.KeyChar += OnKeyChar;
            TextInputBuffer.Clear();
            LoadBindings();
        }

        private static void OnKeyChar(IKeyboard keyboard, char character)
        {
            if (!char.IsControl(character)) TextInputBuffer.Append(character);
        }

        public static string ConsumeTextInput()
        {
            string value = TextInputBuffer.ToString();
            TextInputBuffer.Clear();
            return value;
        }

        // Check by ACTION NAME (for example, "Jump")
        public static bool IsActionPressed(string actionName)
        {
            if (_keyboard == null) return false;
            if (Bindings.TryGetValue(actionName, out Key mappedKey))
            {
                // Crash guard: if the key is Unknown (-1), just ignore it
                if (mappedKey == Key.Unknown || (int)mappedKey == -1) return false; 
                
                return _keyboard.IsKeyPressed(mappedKey);
            }
            return false; 
        }

        // Direct key check (for technical needs)
        public static bool IsKeyPressed(Key key) => _keyboard?.IsKeyPressed(key) ?? false;

        // Scene View provides coordinates already in the screen space of the game UI camera.
        // This lets runtime UI work correctly inside the editor's docked viewport.
        public static void SetUiPointerState(Vector2D<float> position, Vector2D<float> worldPosition, bool inside, bool leftDown, bool rightDown, bool middleDown, float wheel, float wheelHorizontal)
        {
            UiPointerPosition = position;
            WorldPointerPosition = worldPosition;
            IsUiPointerInside = inside;
            IsUiPointerLeftDown = inside && leftDown;
            IsUiPointerRightDown = inside && rightDown;
            IsUiPointerMiddleDown = inside && middleDown;
            UiPointerWheel = inside ? wheel : 0f;
            UiPointerWheelHorizontal = inside ? wheelHorizontal : 0f;
        }

        public static Key GetAnyPressedKey()
        {
            if (_keyboard == null) return Key.Unknown;

            // Iterate over every possible Silk.NET key
            foreach (Key key in System.Enum.GetValues(typeof(Key)))
            {
                // Ignore technical values
                if (key == Key.Unknown || (int)key == -1) continue;

                if (_keyboard.IsKeyPressed(key))
                {
                    return key; // Return the key the user is currently holding
                }
            }
            return Key.Unknown;
        }

        // Save control bindings to a file
        public static void SaveBindings()
        {
            string json = JsonSerializer.Serialize(Bindings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("bindings.json", json);
        }

        // Load control bindings
        public static void LoadBindings()
        {
            if (File.Exists("bindings.json"))
            {
                string json = File.ReadAllText("bindings.json");
                Bindings = JsonSerializer.Deserialize<Dictionary<string, Key>>(json) ?? new Dictionary<string, Key>();
            }

            // Safely add missing keys.
            // If a new action (e.g. "Up") was added in code, it appears in an old config automatically!
            bool needsSave = false;
            
            if (!Bindings.ContainsKey("Left")) { Bindings["Left"] = Key.A; needsSave = true; }
            if (!Bindings.ContainsKey("Right")) { Bindings["Right"] = Key.D; needsSave = true; }
            if (!Bindings.ContainsKey("Up")) { Bindings["Up"] = Key.W; needsSave = true; }       // added later
            if (!Bindings.ContainsKey("Down")) { Bindings["Down"] = Key.S; needsSave = true; }   // added later
            if (!Bindings.ContainsKey("Jump")) { Bindings["Jump"] = Key.Space; needsSave = true; }
            if (!Bindings.ContainsKey("Attack")) { Bindings["Attack"] = Key.F; needsSave = true; }

            // Save the file only if new default keys were added
            if (needsSave) SaveBindings(); 
        }
    }
}
