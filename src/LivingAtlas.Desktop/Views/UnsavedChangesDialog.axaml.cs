using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LivingAtlas.Desktop.Views;

public enum UnsavedChangesDialogResult
{
    Save,
    DontSave,
    Cancel
}

public partial class UnsavedChangesDialog : Window
{
    public UnsavedChangesDialog()
    {
        InitializeComponent();
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        Close(UnsavedChangesDialogResult.Save);
    }

    private void DontSave_Click(object? sender, RoutedEventArgs e)
    {
        Close(UnsavedChangesDialogResult.DontSave);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(UnsavedChangesDialogResult.Cancel);
    }
}
