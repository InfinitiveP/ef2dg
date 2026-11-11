using System.Collections.Generic;

namespace ef2dg
{
    // Базовый интерфейс для ЛЮБОГО действия в редакторе
    public interface ICommand
    {
        void Execute(); // Выполнить действие
        void Undo();    // Откатить действие
    }

    // Менеджер истории, который будет жить в EditorState
    public class CommandHistory
    {
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();

        // Выполнить новую команду и записать в историю
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // Если сделали новое действие, старые Redo сгорают
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var cmd = _undoStack.Pop();
                cmd.Undo();
                _redoStack.Push(cmd);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var cmd = _redoStack.Pop();
                cmd.Execute();
                _undoStack.Push(cmd);
            }
        }
    }
}