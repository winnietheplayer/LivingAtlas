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
                new[]
                {
                    BuildMapItem(project, project.RootMap, activeMapId)
                })
        };
    }

    private static ProjectTreeItemViewModel BuildMapItem(
        CampaignMapProject project,
        MapDocument map,
        Guid? activeMapId)
    {
        List<ProjectTreeItemViewModel> children = map.ChildrenMapIds
            .Select(project.FindMap)
            .Where(childMap => childMap != null)
            .Select(childMap => BuildMapItem(project, childMap!, activeMapId))
            .ToList();

        return new ProjectTreeItemViewModel(
            map.Name,
            map.Id,
            map.Id == activeMapId,
            children);
    }
}