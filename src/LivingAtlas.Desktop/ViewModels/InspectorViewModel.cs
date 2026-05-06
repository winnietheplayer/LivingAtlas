using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LivingAtlas.Assets;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class InspectorViewModel : ViewModelBase
{
	private static readonly TextureAssetOptionViewModel NoneTextureOption = new TextureAssetOptionViewModel("None", null, DistrictShape.DefaultTextureTileSizeMeters);

	private readonly TextureAssetCatalog _textureAssetCatalog;

	private MapObject? _selectedObject;

	private string _editableName = string.Empty;

	private string _editableLabelText = string.Empty;

	private string _editableStyleKey = string.Empty;

	private string _editableDescription = string.Empty;

	private string _editableCategory = string.Empty;

	private string _editableRoadKind = string.Empty;

	private string _editableDistrictKind = string.Empty;

	private string _editableLabelKind = string.Empty;

	private string _editableTextureTileSizeMeters = FormatNumber(DistrictShape.DefaultTextureTileSizeMeters);

	private IReadOnlyList<string> _availableStylePresets = Array.Empty<string>();

	private IReadOnlyList<TextureAssetOptionViewModel> _availableFillTextureAssets = new[] { NoneTextureOption };

	private TextureAssetOptionViewModel? _selectedFillTextureAsset = NoneTextureOption;

	private string _selectionDetails = "No selection";

	private string _objectTypeText = "No selection";

	private string _layerText = "Layer: -";

	private string _tagsText = "Tags: -";

	private bool _hasSelection;

	private bool _isMapLabelSelected;

	private bool _isPointOfInterestSelected;

	private bool _isRoadLineSelected;

	private bool _isRoadAreaSelected;

	private bool _isRoadKindSelected;

	private bool _isDistrictShapeSelected;

	private bool _isTextureFillSelected;

	public InspectorViewModel()
		: this(TextureAssetCatalog.Empty)
	{
	}

	public InspectorViewModel(TextureAssetCatalog textureAssetCatalog)
	{
		_textureAssetCatalog = textureAssetCatalog ?? TextureAssetCatalog.Empty;
	}

	public MapObject? SelectedObject
	{
		get
		{
			return _selectedObject;
		}
		private set
		{
			SetProperty(ref _selectedObject, value, "SelectedObject");
		}
	}

	public string EditableName
	{
		get
		{
			return _editableName;
		}
		set
		{
			SetProperty(ref _editableName, value, "EditableName");
		}
	}

	public string EditableLabelText
	{
		get
		{
			return _editableLabelText;
		}
		set
		{
			SetProperty(ref _editableLabelText, value, "EditableLabelText");
		}
	}

	public string EditableStyleKey
	{
		get
		{
			return _editableStyleKey;
		}
		set
		{
			SetProperty(ref _editableStyleKey, value, "EditableStyleKey");
		}
	}

	public string EditableDescription
	{
		get
		{
			return _editableDescription;
		}
		set
		{
			SetProperty(ref _editableDescription, value, "EditableDescription");
		}
	}

	public string EditableCategory
	{
		get
		{
			return _editableCategory;
		}
		set
		{
			SetProperty(ref _editableCategory, value, "EditableCategory");
		}
	}

	public string EditableRoadKind
	{
		get
		{
			return _editableRoadKind;
		}
		set
		{
			SetProperty(ref _editableRoadKind, value, "EditableRoadKind");
		}
	}

	public string EditableDistrictKind
	{
		get
		{
			return _editableDistrictKind;
		}
		set
		{
			SetProperty(ref _editableDistrictKind, value, "EditableDistrictKind");
		}
	}

	public string EditableLabelKind
	{
		get
		{
			return _editableLabelKind;
		}
		set
		{
			SetProperty(ref _editableLabelKind, value, "EditableLabelKind");
		}
	}

	public string EditableTextureTileSizeMeters
	{
		get
		{
			return _editableTextureTileSizeMeters;
		}
		set
		{
			SetProperty(ref _editableTextureTileSizeMeters, value, "EditableTextureTileSizeMeters");
		}
	}

	public IReadOnlyList<string> AvailableStylePresets
	{
		get => _availableStylePresets;
		private set => SetProperty(ref _availableStylePresets, value, nameof(AvailableStylePresets));
	}

	public IReadOnlyList<TextureAssetOptionViewModel> AvailableFillTextureAssets
	{
		get => _availableFillTextureAssets;
		private set => SetProperty(ref _availableFillTextureAssets, value, nameof(AvailableFillTextureAssets));
	}

	public TextureAssetOptionViewModel? SelectedFillTextureAsset
	{
		get => _selectedFillTextureAsset;
		set => SetProperty(ref _selectedFillTextureAsset, value, nameof(SelectedFillTextureAsset));
	}

	public IReadOnlyList<string> RoadKindPresets { get; } = new[] { "primary", "secondary", "alley" };

	public IReadOnlyList<string> DistrictKindPresets { get; } = new[] { "generic", "old-city", "industrial", "slums", "noble", "market", "temple", "military" };

	public IReadOnlyList<string> LabelKindPresets { get; } = new[] { "city", "district", "map-title", "note" };

	public string SelectionDetails
	{
		get
		{
			return _selectionDetails;
		}
		private set
		{
			SetProperty(ref _selectionDetails, value, "SelectionDetails");
		}
	}

	public string ObjectTypeText
	{
		get
		{
			return _objectTypeText;
		}
		private set
		{
			SetProperty(ref _objectTypeText, value, "ObjectTypeText");
		}
	}

	public string LayerText
	{
		get
		{
			return _layerText;
		}
		private set
		{
			SetProperty(ref _layerText, value, "LayerText");
		}
	}

	public string TagsText
	{
		get
		{
			return _tagsText;
		}
		private set
		{
			SetProperty(ref _tagsText, value, "TagsText");
		}
	}

	public bool HasSelection
	{
		get
		{
			return _hasSelection;
		}
		private set
		{
			SetProperty(ref _hasSelection, value, "HasSelection");
		}
	}

	public bool IsMapLabelSelected
	{
		get
		{
			return _isMapLabelSelected;
		}
		private set
		{
			SetProperty(ref _isMapLabelSelected, value, "IsMapLabelSelected");
		}
	}

	public bool IsPointOfInterestSelected
	{
		get
		{
			return _isPointOfInterestSelected;
		}
		private set
		{
			SetProperty(ref _isPointOfInterestSelected, value, "IsPointOfInterestSelected");
		}
	}

	public bool IsRoadLineSelected
	{
		get
		{
			return _isRoadLineSelected;
		}
		private set
		{
			SetProperty(ref _isRoadLineSelected, value, "IsRoadLineSelected");
		}
	}

	public bool IsRoadAreaSelected
	{
		get
		{
			return _isRoadAreaSelected;
		}
		private set
		{
			SetProperty(ref _isRoadAreaSelected, value, "IsRoadAreaSelected");
		}
	}

	public bool IsRoadKindSelected
	{
		get
		{
			return _isRoadKindSelected;
		}
		private set
		{
			SetProperty(ref _isRoadKindSelected, value, "IsRoadKindSelected");
		}
	}

	public bool IsDistrictShapeSelected
	{
		get
		{
			return _isDistrictShapeSelected;
		}
		private set
		{
			SetProperty(ref _isDistrictShapeSelected, value, "IsDistrictShapeSelected");
		}
	}

	public bool IsTextureFillSelected
	{
		get
		{
			return _isTextureFillSelected;
		}
		private set
		{
			SetProperty(ref _isTextureFillSelected, value, "IsTextureFillSelected");
		}
	}

	public void SetSelection(MapObject? selectedObject)
	{
		SelectedObject = selectedObject;
		HasSelection = selectedObject != null;
		IsMapLabelSelected = selectedObject is MapLabel;
		IsPointOfInterestSelected = selectedObject is PointOfInterest;
		IsRoadLineSelected = selectedObject is RoadLine;
		IsRoadAreaSelected = selectedObject is RoadArea;
		IsRoadKindSelected = selectedObject is RoadLine or RoadArea;
		IsDistrictShapeSelected = selectedObject is DistrictShape;
		IsTextureFillSelected = selectedObject is DistrictShape or RoadArea;
		EditableName = selectedObject?.Name ?? string.Empty;
		EditableLabelText = ((selectedObject is MapLabel mapLabel) ? mapLabel.Text : string.Empty);
		EditableStyleKey = selectedObject?.StyleKey ?? string.Empty;
		EditableDescription = selectedObject?.Description ?? string.Empty;
		EditableCategory = ((selectedObject is PointOfInterest pointOfInterest) ? pointOfInterest.Category : string.Empty);
		EditableRoadKind = selectedObject switch
		{
			RoadLine roadLine => roadLine.RoadKind,
			RoadArea roadArea => roadArea.RoadKind,
			_ => string.Empty
		};
		EditableDistrictKind = ((selectedObject is DistrictShape districtShape) ? districtShape.DistrictKind : string.Empty);
		EditableLabelKind = ((selectedObject is MapLabel label) ? label.LabelKind : string.Empty);
		UpdateTextureFillSelection(selectedObject);
		AvailableStylePresets = selectedObject != null ? LivingAtlas.Editor.Creation.MapObjectStylePresets.GetPresetsForType(selectedObject.ObjectType) : Array.Empty<string>();
		ObjectTypeText = selectedObject == null ? "No selection" : "Object type: " + selectedObject.ObjectType;
		LayerText = selectedObject == null ? "Layer: -" : "Layer: " + selectedObject.LayerId;
		TagsText = selectedObject == null ? "Tags: -" : "Tags: " + FormatTags(selectedObject);
		SelectionDetails = ((selectedObject == null) ? "No selection" : FormatGeometryDetails(selectedObject));
	}

	private static string FormatTags(MapObject mapObject)
	{
		return mapObject.Tags.Count == 0 ? "-" : string.Join(", ", mapObject.Tags);
	}

	private void UpdateTextureFillSelection(MapObject? selectedObject)
	{
		string? fillTextureAssetId = GetFillTextureAssetId(selectedObject);
		double tileSizeMeters = GetTextureTileSizeMeters(selectedObject);
		List<TextureAssetOptionViewModel> options = CreateTextureOptions(fillTextureAssetId);
		AvailableFillTextureAssets = options;
		SelectedFillTextureAsset = SelectTextureOption(options, fillTextureAssetId);
		EditableTextureTileSizeMeters = FormatNumber(tileSizeMeters);
	}

	private static string? GetFillTextureAssetId(MapObject? selectedObject)
	{
		return selectedObject switch
		{
			DistrictShape districtShape => districtShape.FillTextureAssetId,
			RoadArea roadArea => roadArea.FillTextureAssetId,
			_ => null
		};
	}

	private static double GetTextureTileSizeMeters(MapObject? selectedObject)
	{
		return selectedObject switch
		{
			DistrictShape districtShape => districtShape.TextureTileSizeMeters,
			RoadArea roadArea => roadArea.TextureTileSizeMeters,
			_ => DistrictShape.DefaultTextureTileSizeMeters
		};
	}

	private List<TextureAssetOptionViewModel> CreateTextureOptions(string? selectedAssetId)
	{
		var options = new List<TextureAssetOptionViewModel> { NoneTextureOption };
		options.AddRange(_textureAssetCatalog.Textures
			.OrderBy(asset => asset.Category, StringComparer.OrdinalIgnoreCase)
			.ThenBy(asset => asset.Name, StringComparer.OrdinalIgnoreCase)
			.Select(asset => new TextureAssetOptionViewModel(
				FormatTextureAssetName(asset),
				asset.Id,
				asset.DefaultTileSizeMeters)));

		if (!string.IsNullOrWhiteSpace(selectedAssetId) && !options.Any(option => string.Equals(option.AssetId, selectedAssetId, StringComparison.Ordinal)))
		{
			options.Add(new TextureAssetOptionViewModel("Missing: " + selectedAssetId, selectedAssetId, DistrictShape.DefaultTextureTileSizeMeters));
		}

		return options;
	}

	private static TextureAssetOptionViewModel SelectTextureOption(IReadOnlyList<TextureAssetOptionViewModel> options, string? assetId)
	{
		if (!string.IsNullOrWhiteSpace(assetId))
		{
			TextureAssetOptionViewModel? matchingOption = options.FirstOrDefault(option => string.Equals(option.AssetId, assetId, StringComparison.Ordinal));
			if (matchingOption != null)
			{
				return matchingOption;
			}
		}

		return options.First();
	}

	private static string FormatTextureAssetName(TextureAssetDefinition asset)
	{
		if (string.IsNullOrWhiteSpace(asset.Category))
		{
			return asset.Name;
		}

		return asset.Category + " / " + asset.Name;
	}

	private static string FormatGeometryDetails(MapObject mapObject)
	{
		if (mapObject is DistrictShape districtShape)
		{
			return string.Join(Environment.NewLine, new[]
			{
				$"Polygon points: {districtShape.PolygonPoints.Count}",
				"ChildMapId: " + FormatOptionalId(districtShape.ChildMapId)
			});
		}

		return mapObject switch
		{
			RoadLine roadLine => $"Points: {roadLine.Points.Count}",
			RoadArea roadArea => $"Polygon points: {roadArea.PolygonPoints.Count}",
			PointOfInterest pointOfInterest => "Position: " + FormatPoint(pointOfInterest.Position),
			MapLabel mapLabel => $"Text: {mapLabel.Text}{Environment.NewLine}Position: {FormatPoint(mapLabel.Position)}",
			_ => string.Empty
		};
	}

	private static string FormatPoint(PointD point)
	{
		return string.Create(CultureInfo.InvariantCulture, $"X: {point.X:F2} m, Y: {point.Y:F2} m");
	}

	private static string FormatNumber(double value)
	{
		return value.ToString("0.###", CultureInfo.InvariantCulture);
	}

	private static string FormatOptionalId(Guid? id)
	{
		return id?.ToString() ?? "-";
	}
}
