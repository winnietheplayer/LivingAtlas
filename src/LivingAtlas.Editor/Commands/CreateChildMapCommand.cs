using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Geometry;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Editor.Commands;

public sealed class CreateChildMapCommand : IEditorCommand
{
    private readonly CampaignMapProject _project;
    private readonly MapDocument _parentMap;
    private readonly DistrictShape _district;
    private readonly MapDocument _childMap;

    public string Description => "Create child map " + _childMap.Name;

    public MapDocument ChildMap => _childMap;

    public CreateChildMapCommand(
        CampaignMapProject project,
        MapDocument parentMap,
        DistrictShape district,
        string? customName = null,
        SizeD? customSize = null,
        MapScaleType? scaleType = null,
        Guid? childMapId = null)
    {
        ArgumentNullException.ThrowIfNull(project, nameof(project));
        ArgumentNullException.ThrowIfNull(parentMap, nameof(parentMap));
        ArgumentNullException.ThrowIfNull(district, nameof(district));

        if (project.FindMap(parentMap.Id) == null)
        {
            throw new ArgumentException("Parent map must belong to the project.", nameof(parentMap));
        }

        if (district.ChildMapId.HasValue)
        {
            throw new InvalidOperationException($"District '{district.Id}' already has a child map.");
        }

        Guid id = childMapId ?? Guid.NewGuid();

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Child map id cannot be empty.", nameof(childMapId));
        }

        _project = project;
        _parentMap = parentMap;
        _district = district;

        RectD boundingBox = GetBoundingBox(district.PolygonPoints);
        SizeD size = customSize ?? boundingBox.Size;
        string name = string.IsNullOrWhiteSpace(customName) ? district.Name : customName.Trim();
        MapScaleType st = scaleType ?? MapScaleType.District;

        _childMap = new MapDocument(
            id,
            name,
            st,
            size,
            parentMap.Id,
            parentMap.GridSettings);

        AddStarterContent(_childMap, district, boundingBox, size);
    }

    public void Execute()
    {
        Guid? childMapId = _district.ChildMapId;

        if (childMapId.HasValue)
        {
            Guid existingChildMapId = childMapId.GetValueOrDefault();

            if (existingChildMapId != _childMap.Id)
            {
                throw new InvalidOperationException(
                    $"District '{_district.Id}' already has a different child map.");
            }
        }

        if (_project.FindMap(_childMap.Id) == null)
        {
            _project.AddMap(_childMap);
        }

        _parentMap.AddChildMapId(_childMap.Id);
        _district.SetChildMapId(_childMap.Id);
    }

    public void Undo()
    {
        if (_district.ChildMapId == _childMap.Id)
        {
            _district.SetChildMapId(null);
        }

        _parentMap.RemoveChildMapId(_childMap.Id);
        _project.RemoveMap(_childMap.Id);
    }

    private static void AddStarterContent(
        MapDocument childMap,
        DistrictShape district,
        RectD parentBounds,
        SizeD childMapSize)
    {
        MapLayer boundaryLayer = new MapLayer(
            Guid.NewGuid(),
            "Boundaries",
            MapLayerType.Districts);

        double scaleX = parentBounds.Size.Width > 0 ? childMapSize.Width / parentBounds.Size.Width : 1.0;
        double scaleY = parentBounds.Size.Height > 0 ? childMapSize.Height / parentBounds.Size.Height : 1.0;

        DistrictShape boundary = new DistrictShape(
            Guid.NewGuid(),
            district.Name + " Boundary",
            boundaryLayer.Id,
            district.PolygonPoints.Select(point => ToChildPoint(point, parentBounds, scaleX, scaleY)),
            new[] { "boundary" },
            "district.boundary");

        boundaryLayer.AddObject(boundary);
        childMap.AddLayer(boundaryLayer);

        MapLayer labelLayer = new MapLayer(
            Guid.NewGuid(),
            "Labels",
            MapLayerType.Labels);

        MapLabel label = new MapLabel(
            Guid.NewGuid(),
            district.Name,
            labelLayer.Id,
            new PointD(
                childMap.RealSizeMeters.Width / 2.0,
                childMap.RealSizeMeters.Height / 2.0),
            district.Name,
            new[] { "map-label" },
            "label.map-title");

        labelLayer.AddObject(label);
        childMap.AddLayer(labelLayer);
    }

    private static PointD ToChildPoint(PointD parentPoint, RectD parentBounds, double scaleX, double scaleY)
    {
        return new PointD(
            (parentPoint.X - parentBounds.Left) * scaleX,
            (parentPoint.Y - parentBounds.Top) * scaleY);
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

        return new RectD(
            left,
            top,
            right - left,
            bottom - top);
    }
}