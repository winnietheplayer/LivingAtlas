using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace LivingAtlas.Desktop.Views;

public partial class RenameLayerDialog : Window
{
    public RenameLayerDialog()
    {
        InitializeComponent();
    }

    public RenameLayerDialog(string currentName) : this()
    {
        LayerNameTextBox.Text = currentName;
        UpdateOkButton();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        LayerNameTextBox.Focus();
        LayerNameTextBox.SelectAll();
    }

    private void LayerNameTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateOkButton();
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        Close(LayerNameTextBox.Text?.Trim());
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void UpdateOkButton()
    {
        OkButton.IsEnabled = !string.IsNullOrWhiteSpace(LayerNameTextBox.Text);
    }
}
