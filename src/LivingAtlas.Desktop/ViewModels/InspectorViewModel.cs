using System;
using System.Globalization;
using System.Runtime.CompilerServices;
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
		string text = ((mapObject.Tags.Count == 0) ? "-" : string.Join(", ", mapObject.Tags));
		if (1 == 0)
		{
		}
		string text2;
		if (!(mapObject is DistrictShape districtShape))
		{
			text2 = ((mapObject is RoadLine roadLine) ? $"Points: {roadLine.Points.Count}" : ((mapObject is PointOfInterest pointOfInterest) ? ("Position: " + FormatPoint(pointOfInterest.Position)) : ((!(mapObject is MapLabel mapLabel)) ? string.Empty : $"Text: {mapLabel.Text}{Environment.NewLine}Position: {FormatPoint(mapLabel.Position)}")));
		}
		else
		{
			string newLine = Environment.NewLine;
			InlineArray2<string> buffer = default(InlineArray2<string>);
			buffer[0] = $"Polygon points: {districtShape.PolygonPoints.Count}";
			buffer[1] = "ChildMapId: " + FormatOptionalId(districtShape.ChildMapId);
			text2 = string.Join(newLine, (ReadOnlySpan<string?>)buffer);
		}
		if (1 == 0)
		{
		}
		string text3 = text2;
		string newLine2 = Environment.NewLine;
		InlineArray5<string> buffer2 = default(InlineArray5<string>);
		buffer2[0] = $"ObjectType: {mapObject.ObjectType}";
		buffer2[1] = $"LayerId: {mapObject.LayerId}";
		buffer2[2] = "Tags: " + text;
		buffer2[3] = "Style: " + FormatStyle(mapObject.StyleKey);
		buffer2[4] = text3;
		return string.Join(newLine2, (ReadOnlySpan<string?>)buffer2);
	}

	private static string FormatPoint(PointD point)
	{
		IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
		DefaultInterpolatedStringHandler handler = new DefaultInterpolatedStringHandler(12, 2, invariantCulture);
		handler.AppendLiteral("X: ");
		handler.AppendFormatted(point.X, "F2");
		handler.AppendLiteral(" m, Y: ");
		handler.AppendFormatted(point.Y, "F2");
		handler.AppendLiteral(" m");
		return string.Create(invariantCulture, ref handler);
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
