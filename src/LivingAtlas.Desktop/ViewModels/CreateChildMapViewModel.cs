using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class CreateChildMapViewModel : ViewModelBase
{
    private string _name;
    private bool _useCustomSize;
    private double _width;
    private double _height;
    private MapScaleType _scaleType;
    private bool _openAfterCreation = true;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value, nameof(Name));
    }

    public bool UseCustomSize
    {
        get => _useCustomSize;
        set => SetProperty(ref _useCustomSize, value, nameof(UseCustomSize));
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

    public bool OpenAfterCreation
    {
        get => _openAfterCreation;
        set => SetProperty(ref _openAfterCreation, value, nameof(OpenAfterCreation));
    }

    public IEnumerable<MapScaleType> ScaleTypes => Enum.GetValues<MapScaleType>();

    public CreateChildMapViewModel(DistrictShape district)
    {
        _name = district.Name;
        _scaleType = MapScaleType.District;
        
        // Initial size from district bounds
        var left = district.PolygonPoints.Min(p => p.X);
        var right = district.PolygonPoints.Max(p => p.X);
        var top = district.PolygonPoints.Min(p => p.Y);
        var bottom = district.PolygonPoints.Max(p => p.Y);
        _width = right - left;
        _height = bottom - top;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && Width > 0 && Height > 0;
    }
}
