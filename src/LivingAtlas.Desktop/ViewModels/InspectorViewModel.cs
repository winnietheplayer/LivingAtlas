using System;
using System.Globalization;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class InspectorViewModel : ViewModelBase
{
	private MapObject? _selectedObject;

	private string _editableName = string.Empty;

	private string _editableLabelText = string.Empty;

	private string _selectionDetails = "No selection";

	private bool _hasSelection;

	private bool _isMapLabelSelected;

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

	public void SetSelection(MapObject? selectedObject)
	{
		SelectedObject = selectedObject;
		HasSelection = selectedObject != null;
		IsMapLabelSelected = selectedObject is MapLabel;
		EditableName = selectedObject?.Name ?? string.Empty;
		EditableLabelText = ((selectedObject is MapLabel mapLabel) ? mapLabel.Text : string.Empty);
		SelectionDetails = ((selectedObject == null) ? "No selection" : FormatSelectionDetails(selectedObject));
	}

	private static string FormatSelectionDetails(MapObject mapObject)
	{
		string tags = mapObject.Tags.Count == 0 ? "-" : string.Join(", ", mapObject.Tags);
		string details;
		if (mapObject is DistrictShape districtShape)
		{
			details = string.Join(Environment.NewLine, new[]
			{
				$"Polygon points: {districtShape.PolygonPoints.Count}",
				"ChildMapId: " + FormatOptionalId(districtShape.ChildMapId)
			});
		}
		else
		{
			details = mapObject switch
			{
				RoadLine roadLine => $"Points: {roadLine.Points.Count}",
				PointOfInterest pointOfInterest => "Position: " + FormatPoint(pointOfInterest.Position),
				MapLabel mapLabel => $"Text: {mapLabel.Text}{Environment.NewLine}Position: {FormatPoint(mapLabel.Position)}",
				_ => string.Empty
			};
		}

		return string.Join(Environment.NewLine, new[]
		{
			$"ObjectType: {mapObject.ObjectType}",
			$"LayerId: {mapObject.LayerId}",
			"Tags: " + tags,
			"Style: " + FormatStyle(mapObject.StyleKey),
			details
		});
	}

	private static string FormatPoint(PointD point)
	{
		return string.Create(CultureInfo.InvariantCulture, $"X: {point.X:F2} m, Y: {point.Y:F2} m");
	}

	private static string FormatStyle(string styleKey)
	{
		return string.IsNullOrWhiteSpace(styleKey) ? "-" : styleKey;
	}

	private static string FormatOptionalId(Guid? id)
	{
		return id?.ToString() ?? "-";
	}
}
