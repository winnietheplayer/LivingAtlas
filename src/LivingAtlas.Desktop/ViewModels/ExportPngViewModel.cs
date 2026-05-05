using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Export;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class ExportPngViewModel : ViewModelBase
{
	private string _outputPath;

	private int _resolutionScale = 1;

	private bool _includeGrid;

	private bool _includeLabels = true;

	private bool _includePointsOfInterest = true;

	private bool _includeChildMapPreviews = true;

	private bool _transparentBackground;

	private string _validationMessage = string.Empty;

	public ExportPngViewModel(string outputPath, bool includeGrid)
	{
		_outputPath = outputPath;
		_includeGrid = includeGrid;
	}

	public IReadOnlyList<int> ResolutionScales { get; } = new[] { 1, 2, 4 };

	public string OutputPath
	{
		get => _outputPath;
		set
		{
			if (SetProperty(ref _outputPath, value, nameof(OutputPath)))
			{
				OnPropertyChanged(nameof(CanExport));
			}
		}
	}

	public int ResolutionScale
	{
		get => _resolutionScale;
		set
		{
			if (SetProperty(ref _resolutionScale, value, nameof(ResolutionScale)))
			{
				OnPropertyChanged(nameof(CanExport));
			}
		}
	}

	public bool IncludeGrid
	{
		get => _includeGrid;
		set => SetProperty(ref _includeGrid, value, nameof(IncludeGrid));
	}

	public bool IncludeLabels
	{
		get => _includeLabels;
		set => SetProperty(ref _includeLabels, value, nameof(IncludeLabels));
	}

	public bool IncludePointsOfInterest
	{
		get => _includePointsOfInterest;
		set => SetProperty(ref _includePointsOfInterest, value, nameof(IncludePointsOfInterest));
	}

	public bool IncludeChildMapPreviews
	{
		get => _includeChildMapPreviews;
		set => SetProperty(ref _includeChildMapPreviews, value, nameof(IncludeChildMapPreviews));
	}

	public bool TransparentBackground
	{
		get => _transparentBackground;
		set => SetProperty(ref _transparentBackground, value, nameof(TransparentBackground));
	}

	public string ValidationMessage
	{
		get => _validationMessage;
		set => SetProperty(ref _validationMessage, value, nameof(ValidationMessage));
	}

	public bool CanExport => !string.IsNullOrWhiteSpace(OutputPath) && ResolutionScales.Contains(ResolutionScale);

	public PngExportOptions ToOptions()
	{
		return new PngExportOptions(OutputPath)
		{
			ResolutionScale = ResolutionScale,
			IncludeGrid = IncludeGrid,
			IncludeLabels = IncludeLabels,
			IncludePointsOfInterest = IncludePointsOfInterest,
			IncludeChildMapPreviews = IncludeChildMapPreviews,
			TransparentBackground = TransparentBackground
		};
	}
}
