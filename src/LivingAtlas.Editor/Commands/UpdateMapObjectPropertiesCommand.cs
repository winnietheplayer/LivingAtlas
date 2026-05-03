using System;
using System.Linq;
using LivingAtlas.Domain.Maps;
using LivingAtlas.Domain.Maps.Objects;

namespace LivingAtlas.Editor.Commands;

public sealed class UpdateMapObjectPropertiesCommand : IEditorCommand
{
	private readonly MapDocument _map;

	private readonly Guid _objectId;

	private readonly string _oldName;

	private readonly string _newName;

	private readonly string? _oldLabelText;

	private readonly string? _newLabelText;

	public string Description => (_oldLabelText == null) ? ("Rename " + _oldName) : ("Update " + _oldName);

	public UpdateMapObjectPropertiesCommand(MapDocument map, Guid objectId, string newName, string? newLabelText = null)
	{
		ArgumentNullException.ThrowIfNull(map, "map");
		if (objectId == Guid.Empty)
		{
			throw new ArgumentException("Object id cannot be empty.", "objectId");
		}
		if (string.IsNullOrWhiteSpace(newName))
		{
			throw new ArgumentException("Map object name cannot be empty.", "newName");
		}
		_map = map;
		_objectId = objectId;
		MapObject mapObject = ResolveObject();
		_oldName = mapObject.Name;
		_newName = newName.Trim();
		if (newLabelText != null)
		{
			if (!(mapObject is MapLabel mapLabel))
			{
				throw new ArgumentException("Only map labels can update label text.", "newLabelText");
			}
			if (string.IsNullOrWhiteSpace(newLabelText))
			{
				throw new ArgumentException("Label text cannot be empty.", "newLabelText");
			}
			_oldLabelText = mapLabel.Text;
			_newLabelText = newLabelText.Trim();
		}
	}

	public void Execute()
	{
		Apply(_newName, _newLabelText);
	}

	public void Undo()
	{
		Apply(_oldName, _oldLabelText);
	}

	private void Apply(string name, string? labelText)
	{
		MapObject mapObject = ResolveObject();
		mapObject.Rename(name);
		if (labelText != null)
		{
			((MapLabel)mapObject).SetText(labelText);
		}
	}

	private MapObject ResolveObject()
	{
		foreach (MapLayer layer in _map.Layers)
		{
			MapObject? mapObject = layer.Objects.FirstOrDefault((MapObject candidate) => candidate.Id == _objectId);
			if (mapObject != null)
			{
				return mapObject;
			}
		}
		throw new InvalidOperationException($"Map object '{_objectId}' was not found in map '{_map.Id}'.");
	}
}
