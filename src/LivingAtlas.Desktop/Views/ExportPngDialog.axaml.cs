using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using LivingAtlas.Desktop.ViewModels;

namespace LivingAtlas.Desktop.Views;

public partial class ExportPngDialog : Window
{
	private static readonly FilePickerFileType PngFileType = new("PNG Image")
	{
		Patterns = new[] { "*.png" },
		MimeTypes = new[] { "image/png" }
	};

	public ExportPngDialog()
	{
		InitializeComponent();
	}

	public ExportPngDialog(ExportPngViewModel viewModel)
		: this()
	{
		DataContext = viewModel;
	}

	private async void Browse_Click(object? sender, RoutedEventArgs e)
	{
		if (DataContext is not ExportPngViewModel viewModel)
		{
			return;
		}

		var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
		{
			Title = "Export PNG",
			SuggestedFileName = Path.GetFileName(viewModel.OutputPath),
			DefaultExtension = "png",
			ShowOverwritePrompt = true,
			FileTypeChoices = new[] { PngFileType }
		});

		string? path = file?.TryGetLocalPath();
		if (!string.IsNullOrWhiteSpace(path))
		{
			viewModel.OutputPath = path;
			viewModel.ValidationMessage = string.Empty;
		}
	}

	private void Export_Click(object? sender, RoutedEventArgs e)
	{
		if (DataContext is ExportPngViewModel viewModel && viewModel.CanExport)
		{
			Close(true);
			return;
		}

		if (DataContext is ExportPngViewModel invalidViewModel)
		{
			invalidViewModel.ValidationMessage = "Choose an output path and resolution scale.";
		}
	}

	private void Cancel_Click(object? sender, RoutedEventArgs e)
	{
		Close(false);
	}
}
