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
    private MapScaleType _scaleType;
    private bool _gridEnabled;
    private double _gridCellSize;
    private bool _snapToGrid;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, nameof(Name));
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, value, nameof(Width));
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, value, nameof(Height));
    }

    public MapScaleType ScaleType
    {
        get => _scaleType;
        set => SetProperty(ref _scaleType, value, nameof(ScaleType));
    }

    public bool GridEnabled
    {
        get => _gridEnabled;
        set => SetProperty(ref _gridEnabled, value, nameof(GridEnabled));
    }

    public double GridCellSize
    {
        get => _gridCellSize;
        set => SetProperty(ref _gridCellSize, value, nameof(GridCellSize));
    }

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => SetProperty(ref _snapToGrid, value, nameof(SnapToGrid));
    }

    public IEnumerable<MapScaleType> ScaleTypes => Enum.GetValues<MapScaleType>();

    public EditMapSettingsViewModel(MapDocument map)
    {
        _name = map.Name;
        _width = map.RealSizeMeters.Width;
        _height = map.RealSizeMeters.Height;
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
            && GridCellSize > 0;
    }
}
