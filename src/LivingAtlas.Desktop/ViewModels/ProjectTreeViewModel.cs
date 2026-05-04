using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Desktop.ViewModels;

public sealed class ProjectTreeViewModel : ViewModelBase
{
    public string ProjectName { get; }

    public IReadOnlyList<ProjectTreeItemViewModel> RootItems { get; }

    public event EventHandler<(Guid MapId, Guid LayerId, bool IsVisible)>? LayerVisibilityChanged;

    public event EventHandler<(Guid MapId, Guid LayerId, bool IsLocked)>? LayerLockChanged;

    public ProjectTreeViewModel(CampaignMapProject project, Guid? activeMapId = null)
    {
        ArgumentNullException.ThrowIfNull(project, nameof(project));

        ProjectName = project.Name;

        RootItems = new[]
        {
            new ProjectTreeItemViewModel(
                project.Name,
                mapId: null,
                isActive: false,
                children: new[]
                {
                    BuildMapItem(project, project.RootMap, activeMapId, OnLayerVisibilityChanged, OnLayerLockChanged)
                })
        };
    }

    private void OnLayerVisibilityChanged(Guid mapId, Guid layerId, bool isVisible)
    {
        LayerVisibilityChanged?.Invoke(this, (mapId, layerId, isVisible));
    }

    private void OnLayerLockChanged(Guid mapId, Guid layerId, bool isLocked)
    {
        LayerLockChanged?.Invoke(this, (mapId, layerId, isLocked));
    }

    private static ProjectTreeItemViewModel BuildMapItem(
        CampaignMapProject project,
        MapDocument map,
        Guid? activeMapId,
        Action<Guid, Guid, bool> onLayerVisibilityChanged,
        Action<Guid, Guid, bool> onLayerLockChanged)
    {
        List<ProjectTreeItemViewModel> children = new List<ProjectTreeItemViewModel>();

        // Add layers first
        foreach (var layer in map.Layers)
        {
            var layerItem = new ProjectTreeItemViewModel(
                layer.Name,
                layerId: layer.Id,
                isVisible: layer.IsVisible,
                isLocked: layer.IsLocked);

            layerItem.VisibilityChanged += (s, e) => 
                onLayerVisibilityChanged(map.Id, layer.Id, layerItem.IsVisible);

            layerItem.LockChanged += (s, e) => 
                onLayerLockChanged(map.Id, layer.Id, layerItem.IsLocked);

            children.Add(layerItem);
        }

        // Add child maps
        children.AddRange(map.ChildrenMapIds
            .Select(project.FindMap)
            .Where(childMap => childMap != null)
            .Select(childMap => BuildMapItem(project, childMap!, activeMapId, onLayerVisibilityChanged, onLayerLockChanged)));

        return new ProjectTreeItemViewModel(
            map.Name,
            map.Id,
            isActive: map.Id == activeMapId,
            children: children);
    }
}