using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LivingAtlas.Desktop.Views;

public partial class ConfirmationDialog : Window
{
    private TextBlock _messageTextBlock;
    private Button _confirmButton;

    public ConfirmationDialog()
    {
        InitializeComponent();
        _messageTextBlock = this.FindControl<TextBlock>("MessageTextBlock")!;
        _confirmButton = this.FindControl<Button>("ConfirmButton")!;
    }

    public ConfirmationDialog(string message, string confirmButtonText = "Delete", string title = "Confirm") : this()
    {
        _messageTextBlock.Text = message;
        _confirmButton.Content = confirmButtonText;
        Title = title;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
