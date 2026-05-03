namespace LivingAtlas.Editor.Tools;

public sealed class EditorToolService
{
	public EditorToolType ActiveTool { get; private set; } = EditorToolType.SelectMove;

	public bool IsSelectMoveTool => ActiveTool == EditorToolType.SelectMove;

	public bool IsNavigationTool => IsNavigationToolType(ActiveTool);

	public bool IsDrawingTool => IsDrawingToolType(ActiveTool);

	public bool AllowsSelectionChanges => AllowsSelectionChangesFor(ActiveTool);

	public bool AllowsObjectMove => ActiveTool == EditorToolType.SelectMove;

	public bool AllowsViewportPanFromDrag => AllowsViewportPanFromDragFor(ActiveTool);

	public void SetActiveTool(EditorToolType activeTool)
	{
		ActiveTool = activeTool;
	}

	public static bool IsNavigationToolType(EditorToolType tool)
	{
		return tool == EditorToolType.Pan;
	}

	public static bool IsDrawingToolType(EditorToolType tool)
	{
		if ((uint)(tool - 2) <= 3u)
		{
			return true;
		}
		return false;
	}

	public static bool AllowsSelectionChangesFor(EditorToolType tool)
	{
		return tool == EditorToolType.SelectMove;
	}

	public static bool AllowsViewportPanFromDragFor(EditorToolType tool)
	{
		if ((uint)tool <= 1u)
		{
			return true;
		}
		return false;
	}
}
