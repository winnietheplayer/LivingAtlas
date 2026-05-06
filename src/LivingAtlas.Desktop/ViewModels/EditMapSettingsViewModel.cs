using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Desktop.ViewModels;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class EditMapSettingsViewModel : ViewModelBase
{
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
                OnRepresentedSizeChanged();
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

    public string RepresentedSizeText => $"{RepresentedWidthFeet:0.##} × {RepresentedHeightFeet:0.##} ft";

    public bool CanApply => IsValid();

    public IEnumerable<MapScaleType> ScaleTypes => Enum.GetValues<MapScaleType>();

    public EditMapSettingsViewModel(MapDocument map)
    {
        _name = map.Name;
        _width = map.RealSizeMeters.Width;
        _height = map.RealSizeMeters.Height;
        _feetPerUnit = map.FeetPerUnit;
        _scaleType = map.ScaleType;
        _gridEnabled = map.GridSettings.IsEnabled;
        _gridCellSize = map.GridSettings.CellSizeMeters;
        _snapToGrid = map.GridSettings.SnapToGrid;
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
}
