using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Editor.Commands;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class CreateChildMapViewModel : ViewModelBase
{
    private readonly double _parentFootprintWidth;
    private readonly double _parentFootprintHeight;
    private readonly double _parentFeetPerUnit;
    private string _name;
    private bool _useCustomSize;
    private double _width;
    private double _height;
    private double _feetPerUnit;
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
        set
        {
            if (SetProperty(ref _useCustomSize, value, nameof(UseCustomSize)) && !value)
            {
                ApplyComputedSize();
            }
        }
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
        set
        {
            if (SetProperty(ref _scaleType, value, nameof(ScaleType)))
            {
                FeetPerUnit = MapDocument.GetDefaultFeetPerUnit(value);
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
                OnComputedChildSizeChanged();

                if (!UseCustomSize && IsPositiveFinite(value))
                {
                    ApplyComputedSize();
                }
            }
        }
    }

    public bool OpenAfterCreation
    {
        get => _openAfterCreation;
        set => SetProperty(ref _openAfterCreation, value, nameof(OpenAfterCreation));
    }

    public IEnumerable<MapScaleType> ScaleTypes => Enum.GetValues<MapScaleType>();

    public double ParentFootprintWidth => _parentFootprintWidth;

    public double ParentFootprintHeight => _parentFootprintHeight;

    public double ParentFeetPerUnit => _parentFeetPerUnit;

    public double ParentPhysicalWidthFeet => _parentFootprintWidth * _parentFeetPerUnit;

    public double ParentPhysicalHeightFeet => _parentFootprintHeight * _parentFeetPerUnit;

    public double ComputedChildWidth => IsPositiveFinite(FeetPerUnit) ? ParentPhysicalWidthFeet / FeetPerUnit : 0.0;

    public double ComputedChildHeight => IsPositiveFinite(FeetPerUnit) ? ParentPhysicalHeightFeet / FeetPerUnit : 0.0;

    public string ParentFootprintLocalSizeText => $"{ParentFootprintWidth:0.##} x {ParentFootprintHeight:0.##} units";

    public string ParentScaleText => $"1 parent unit = {ParentFeetPerUnit:0.##} ft";

    public string ParentPhysicalSizeText => $"{ParentPhysicalWidthFeet:0.##} x {ParentPhysicalHeightFeet:0.##} ft";

    public string ChildScaleText => $"1 child unit = {FeetPerUnit:0.##} ft";

    public string ComputedChildLocalSizeText => $"{ComputedChildWidth:0.##} x {ComputedChildHeight:0.##} units";

    public CreateChildMapViewModel(DistrictShape district)
        : this(district, MapScaleType.City, MapDocument.GetDefaultFeetPerUnit(MapScaleType.City))
    {
    }

    public CreateChildMapViewModel(DistrictShape district, MapDocument parentMap)
        : this(district, parentMap.ScaleType, parentMap.FeetPerUnit)
    {
    }

    private CreateChildMapViewModel(DistrictShape district, MapScaleType parentScaleType, double parentFeetPerUnit)
    {
        ArgumentNullException.ThrowIfNull(district, nameof(district));

        _name = district.Name;
        _scaleType = MapDocument.GetDefaultChildScaleType(parentScaleType);
        _feetPerUnit = MapDocument.GetDefaultFeetPerUnit(_scaleType);
        _parentFeetPerUnit = parentFeetPerUnit;
        
        RectD bounds = GetBoundingBox(district.PolygonPoints);
        _parentFootprintWidth = bounds.Size.Width;
        _parentFootprintHeight = bounds.Size.Height;
        _width = ComputedChildWidth;
        _height = ComputedChildHeight;
    }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && Width > 0 && Height > 0 && FeetPerUnit > 0;
    }

    private void ApplyComputedSize()
    {
        SetProperty(ref _width, ComputedChildWidth, nameof(Width));
        SetProperty(ref _height, ComputedChildHeight, nameof(Height));
    }

    private void OnComputedChildSizeChanged()
    {
        OnPropertyChanged(nameof(ChildScaleText));
        OnPropertyChanged(nameof(ComputedChildWidth));
        OnPropertyChanged(nameof(ComputedChildHeight));
        OnPropertyChanged(nameof(ComputedChildLocalSizeText));
    }

    private static RectD GetBoundingBox(IReadOnlyList<PointD> points)
    {
        double left = points[0].X;
        double right = points[0].X;
        double top = points[0].Y;
        double bottom = points[0].Y;

        for (int i = 1; i < points.Count; i++)
        {
            PointD point = points[i];

            left = Math.Min(left, point.X);
            right = Math.Max(right, point.X);
            top = Math.Min(top, point.Y);
            bottom = Math.Max(bottom, point.Y);
        }

        return new RectD(left, top, right - left, bottom - top);
    }

    private static bool IsPositiveFinite(double value)
    {
        return value > 0.0 && !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
