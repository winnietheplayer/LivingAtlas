using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace LivingAtlas.Desktop.Controls;

internal static class EditorHotkeyGuard
{
    public static bool ShouldIgnoreEditorHotkeys(KeyEventArgs e)
    {
        return IsTextInputElement(e.Source);
    }

    private static bool IsTextInputElement(object? source)
    {
        if (source is TextBox)
        {
            return true;
        }

        if (source is Visual visual)
        {
            return visual.GetVisualAncestors().Any(ancestor => ancestor is TextBox);
        }

        return false;
    }
}
