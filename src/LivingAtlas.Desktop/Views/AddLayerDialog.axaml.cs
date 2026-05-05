using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using LivingAtlas.Domain.Maps;

namespace LivingAtlas.Desktop.Views;

public partial class AddLayerDialog : Window
{
    private TextBox _nameTextBox;
    private ComboBox _typeComboBox;
    private Button _okButton;

    public AddLayerDialog()
    {
        InitializeComponent();
        
        _nameTextBox = this.FindControl<TextBox>("NameTextBox")!;
        _typeComboBox = this.FindControl<ComboBox>("TypeComboBox")!;
        _okButton = this.FindControl<Button>("OkButton")!;

        _nameTextBox.TextChanged += (s, e) => Validate();
        
        // Populate ComboBox
        _typeComboBox.ItemsSource = Enum.GetValues<MapLayerType>();
        _typeComboBox.SelectedIndex = 0; // Default selection

        Validate();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Validate()
    {
        _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        var name = _nameTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var type = (MapLayerType)_typeComboBox.SelectedItem!;
        Close((name, type));
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}
