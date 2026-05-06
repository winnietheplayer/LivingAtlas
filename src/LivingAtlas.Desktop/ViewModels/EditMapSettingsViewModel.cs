using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Projects;
using LivingAtlas.Editor.Hierarchy;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class EditMapSettingsViewModel : ViewModelBase
{
    private readonly ChildMapScaleDiagnostics? _childScaleDiagnostics;

    private string _name;
    private double _width;
    private double _height;
    private double _feetPerUnit;
    private MapScaleType _scaleType;
    private bool _gridEnabled;
    private double _gridCellSize;
    private bool _snapToGrid;

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value, nameof(Name)))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public double Width
    {
        get => _width;
        set
        {
            if (SetProperty(ref _width, value, nameof(Width)))
            {
                OnRepresentedSizeChanged();
                OnChildScaleDiagnosticsChanged();
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public double Height
    {
        get => _height;
        set
        {
            if (SetProperty(ref _height, value, nameof(Height)))
            {
                OnRepresentedSizeChanged();
                OnChildScaleDiagnosticsChanged();
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public MapScaleType ScaleType
    {
        get => _scaleType;
        set
        {
            if (SetProperty(ref _scaleType, value, nameof(ScaleType)))
            {
                FeetPerUnit = MapDocument.GetDefaultFeetPerUnit(value);
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public double FeetPerUnit
    {
        get => _feetPerUnit;
        set
        {
            if (SetProperty(ref _feetPerUnit, value, nameof(FeetPerUnit)))
            {
                OnPropertyChanged(nameof(ScaleText));
                OnPropertyChanged(nameof(GridCellFeet));
                OnPropertyChanged(nameof(GridPhysicalSizeText));
                OnRepresentedSizeChanged();
                OnChildScaleDiagnosticsChanged();
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public bool GridEnabled
    {
        get => _gridEnabled;
        set
        {
            if (SetProperty(ref _gridEnabled, value, nameof(GridEnabled)))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public double GridCellSize
    {
        get => _gridCellSize;
        set
        {
            if (SetProperty(ref _gridCellSize, value, nameof(GridCellSize)))
            {
                OnPropertyChanged(nameof(GridCellFeet));
                OnPropertyChanged(nameof(GridPhysicalSizeText));
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set
        {
            if (SetProperty(ref _snapToGrid, value, nameof(SnapToGrid)))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    public double RepresentedWidthFeet => Width * FeetPerUnit;

    public double RepresentedHeightFeet => Height * FeetPerUnit;

    public string LocalSizeText => $"{Width:0.##} × {Height:0.##} units";

    public string ScaleText => $"1 unit = {FeetPerUnit:0.##} ft";

    public double GridCellFeet => GridCellSize * FeetPerUnit;

    public string GridPhysicalSizeText => $"{GridCellSize:0.##} units = {GridCellFeet:0.##} ft";

    public bool HasChildScaleDiagnostics => _childScaleDiagnostics != null;

    public bool HasChildScaleWarning => HasChildScaleDiagnostics && ChildScaleHasMismatch();

    public string ChildScaleDiagnosticsText
    {
        get
        {
            if (_childScaleDiagnostics == null)
            {
                return string.Empty;
            }

            return $"Parent: {_childScaleDiagnostics.ParentMapName} ({_childScaleDiagnostics.ParentScaleType}, 1 unit = {_childScaleDiagnostics.ParentFeetPerUnit:0.##} ft) | "
                + $"footprint {_childScaleDiagnostics.ParentFootprintLocalSize.Width:0.##}x{_childScaleDiagnostics.ParentFootprintLocalSize.Height:0.##} units "
                + $"= {_childScaleDiagnostics.ParentFootprintPhysicalSizeFeet.Width:0.##}x{_childScaleDiagnostics.ParentFootprintPhysicalSizeFeet.Height:0.##} ft | "
                + $"expected child {ExpectedChildWidth:0.##}x{ExpectedChildHeight:0.##} units, actual {Width:0.##}x{Height:0.##} units";
        }
    }

    public string ChildScaleWarningText => HasChildScaleWarning
        ? $"Child map scale mismatch: expected {ExpectedChildWidth:0.##}x{ExpectedChildHeight:0.##} units, actual {Width:0.##}x{Height:0.##}."
        : string.Empty;

    public string RepresentedSizeText => $"{RepresentedWidthFeet:0.##} × {RepresentedHeightFeet:0.##} ft";

    public bool CanApply => IsValid();

    public IEnumerable<MapScaleType> ScaleTypes => Enum.GetValues<MapScaleType>();

    public EditMapSettingsViewModel(MapDocument map, CampaignMapProject? project = null)
    {
        _name = map.Name;
        _width = map.RealSizeMeters.Width;
        _height = map.RealSizeMeters.Height;
        _feetPerUnit = map.FeetPerUnit;
        _scaleType = map.ScaleType;
        _gridEnabled = map.GridSettings.IsEnabled;
        _gridCellSize = map.GridSettings.CellSizeMeters;
        _snapToGrid = map.GridSettings.SnapToGrid;
        _childScaleDiagnostics = project == null ? null : ScaleDiagnosticsService.GetChildMapDiagnostics(project, map);
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) 
            && Width > 0 
            && Height > 0 
            && FeetPerUnit > 0
            && GridCellSize > 0;
    }

    private void OnRepresentedSizeChanged()
    {
        OnPropertyChanged(nameof(LocalSizeText));
        OnPropertyChanged(nameof(RepresentedWidthFeet));
        OnPropertyChanged(nameof(RepresentedHeightFeet));
        OnPropertyChanged(nameof(RepresentedSizeText));
    }

    private double ExpectedChildWidth => _childScaleDiagnostics == null || FeetPerUnit <= 0.0
        ? 0.0
        : _childScaleDiagnostics.ParentFootprintPhysicalSizeFeet.Width / FeetPerUnit;

    private double ExpectedChildHeight => _childScaleDiagnostics == null || FeetPerUnit <= 0.0
        ? 0.0
        : _childScaleDiagnostics.ParentFootprintPhysicalSizeFeet.Height / FeetPerUnit;

    private bool ChildScaleHasMismatch()
    {
        if (_childScaleDiagnostics == null)
        {
            return false;
        }

        return ScaleDiagnosticsService.HasSizeMismatch(
            new SizeD(Math.Max(0.0, Width), Math.Max(0.0, Height)),
            new SizeD(ExpectedChildWidth, ExpectedChildHeight));
    }

    private void OnChildScaleDiagnosticsChanged()
    {
        OnPropertyChanged(nameof(HasChildScaleWarning));
        OnPropertyChanged(nameof(ChildScaleDiagnosticsText));
        OnPropertyChanged(nameof(ChildScaleWarningText));
    }
}
