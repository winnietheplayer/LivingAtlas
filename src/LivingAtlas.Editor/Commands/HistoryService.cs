using System;
using System.Collections.Generic;

namespace LivingAtlas.Editor.Commands;

public sealed class HistoryService
{
	private readonly Stack<IEditorCommand> _undoStack = new Stack<IEditorCommand>();

	private readonly Stack<IEditorCommand> _redoStack = new Stack<IEditorCommand>();

	public bool CanUndo => _undoStack.Count > 0;

	public bool CanRedo => _redoStack.Count > 0;

	public string? UndoDescription => CanUndo ? _undoStack.Peek().Description : null;

	public string? RedoDescription => CanRedo ? _redoStack.Peek().Description : null;

	public void Execute(IEditorCommand command)
	{
		ArgumentNullException.ThrowIfNull(command, "command");
		command.Execute();
		_undoStack.Push(command);
		_redoStack.Clear();
	}

	public IEditorCommand? Undo()
	{
		if (!CanUndo)
		{
			return null;
		}
		IEditorCommand editorCommand = _undoStack.Pop();
		editorCommand.Undo();
		_redoStack.Push(editorCommand);
		return editorCommand;
	}

	public IEditorCommand? Redo()
	{
		if (!CanRedo)
		{
			return null;
		}
		IEditorCommand editorCommand = _redoStack.Pop();
		editorCommand.Execute();
		_undoStack.Push(editorCommand);
		return editorCommand;
	}
}
