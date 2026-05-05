using System;
using System.Collections.Generic;
using System.Globalization;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class InspectorViewModel : ViewModelBase
{
	private MapObject? _selectedObject;

	private string _editableName = string.Empty;

	private string _editableLabelText = string.Empty;

	private string _editableStyleKey = string.Empty;

	private string _editableDescription = string.Empty;

	private string _editableCategory = string.Empty;

	private string _editableRoadKind = string.Empty;

	private string _editableDistrictKind = string.Empty;

	private string _editableLabelKind = string.Empty;

	private IReadOnlyList<string> _availableStylePresets = Array.Empty<string>();

	private string _selectionDetails = "No selection";

	private string _objectTypeText = "No selection";

	private string _layerText = "Layer: -";

	private string _tagsText = "Tags: -";

	private bool _hasSelection;

	private bool _isMapLabelSelected;

	private bool _isPointOfInterestSelected;

	private bool _isRoadLineSelected;

	private bool _isDistrictShapeSelected;

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

	public IReadOnlyList<string> AvailableStylePresets
	{
		get => _availableStylePresets;
		private set => SetProperty(ref _availableStylePresets, value, nameof(AvailableStylePresets));
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

	public void SetSelection(MapObject? selectedObject)
	{
		SelectedObject = selectedObject;
		HasSelection = selectedObject != null;
		IsMapLabelSelected = selectedObject is MapLabel;
		IsPointOfInterestSelected = selectedObject is PointOfInterest;
		IsRoadLineSelected = selectedObject is RoadLine;
		IsDistrictShapeSelected = selectedObject is DistrictShape;
		EditableName = selectedObject?.Name ?? string.Empty;
		EditableLabelText = ((selectedObject is MapLabel mapLabel) ? mapLabel.Text : string.Empty);
		EditableStyleKey = selectedObject?.StyleKey ?? string.Empty;
		EditableDescription = selectedObject?.Description ?? string.Empty;
		EditableCategory = ((selectedObject is PointOfInterest pointOfInterest) ? pointOfInterest.Category : string.Empty);
		EditableRoadKind = ((selectedObject is RoadLine roadLine) ? roadLine.RoadKind : string.Empty);
		EditableDistrictKind = ((selectedObject is DistrictShape districtShape) ? districtShape.DistrictKind : string.Empty);
		EditableLabelKind = ((selectedObject is MapLabel label) ? label.LabelKind : string.Empty);
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
			PointOfInterest pointOfInterest => "Position: " + FormatPoint(pointOfInterest.Position),
			MapLabel mapLabel => $"Text: {mapLabel.Text}{Environment.NewLine}Position: {FormatPoint(mapLabel.Position)}",
			_ => string.Empty
		};
	}

	private static string FormatPoint(PointD point)
	{
		return string.Create(CultureInfo.InvariantCulture, $"X: {point.X:F2} m, Y: {point.Y:F2} m");
	}

	private static string FormatOptionalId(Guid? id)
	{
		return id?.ToString() ?? "-";
	}
}
