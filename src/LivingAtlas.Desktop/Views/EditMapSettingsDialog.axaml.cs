using Avalonia.Controls;
using Avalonia.Interactivity;
using LivingAtlas.Desktop.ViewModels;

namespace LivingAtlas.Desktop.Views;

public partial class EditMapSettingsDialog : Window
{
    public EditMapSettingsDialog()
    {
        InitializeComponent();
    }

    public EditMapSettingsDialog(EditMapSettingsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is EditMapSettingsViewModel viewModel && viewModel.IsValid())
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
