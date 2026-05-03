using System;
using System.Collections.Generic;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Projects;

namespace LivingAtlas.Editor.Navigation;

public static class MapBreadcrumbService
{
	public static IReadOnlyList<MapBreadcrumb> BuildBreadcrumbs(CampaignMapProject project, Guid activeMapId)
	{
		ArgumentNullException.ThrowIfNull(project, "project");
		if (activeMapId == Guid.Empty)
		{
			throw new ArgumentException("Active map id cannot be empty.", "activeMapId");
		}
		Stack<MapBreadcrumb> stack = new Stack<MapBreadcrumb>();
		HashSet<Guid> hashSet = new HashSet<Guid>();
		MapDocument mapDocument = project.FindMap(activeMapId) ?? throw new InvalidOperationException($"Active map '{activeMapId}' is not present in project '{project.Id}'.");
		while (true)
		{
			if (!hashSet.Add(mapDocument.Id))
			{
				throw new InvalidOperationException($"Map hierarchy contains a cycle at map '{mapDocument.Id}'.");
			}
			stack.Push(new MapBreadcrumb(mapDocument.Id, mapDocument.Name));
			Guid? parentMapId = mapDocument.ParentMapId;
			if (parentMapId.HasValue)
			{
				Guid valueOrDefault = parentMapId.GetValueOrDefault();
				mapDocument = project.FindMap(valueOrDefault) ?? throw new InvalidOperationException($"Parent map '{valueOrDefault}' is not present in project '{project.Id}'.");
				continue;
			}
			break;
		}
		return stack.ToList();
	}
}
