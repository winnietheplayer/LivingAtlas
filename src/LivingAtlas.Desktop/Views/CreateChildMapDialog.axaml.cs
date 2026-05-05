using Avalonia.Controls;
using Avalonia.Interactivity;
using LivingAtlas.Desktop.ViewModels;

namespace LivingAtlas.Desktop.Views;

public partial class CreateChildMapDialog : Window
{
    public CreateChildMapDialog()
    {
        InitializeComponent();
    }

    public CreateChildMapDialog(CreateChildMapViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is CreateChildMapViewModel viewModel && viewModel.IsValid())
        {
            Close(true);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
