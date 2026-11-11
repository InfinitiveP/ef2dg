using System.Collections.Generic;

namespace ef2dg
{
    // Base interface for ANY editor action
    public interface ICommand
    {
        void Execute(); // Perform the action
        void Undo();    // Undo the action
    }

    // History manager that lives in EditorState
    public class CommandHistory
    {
        private Stack<ICommand> _undoStack = new Stack<ICommand>();
        private Stack<ICommand> _redoStack = new Stack<ICommand>();

        // Execute a new command and record it in the history
        public void ExecuteCommand(ICommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear(); // A new action discards the old Redo entries
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