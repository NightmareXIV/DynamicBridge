using System.IO;
using Dalamud.Plugin.Ipc;
using DynamicBridge.Configuration;
using DynamicBridge.Core;
using DynamicBridge.Gui;
using ECommons.EzIpcManager;
using JetBrains.Annotations;

namespace DynamicBridge.IPC.Conditions;

public class ConditionsManager : IDisposable
{
	public readonly Dictionary<string, Dictionary<string, ExtraCondition>> conditions = [];

	public ConditionsManager()
	{
		Svc.Log.Debug("Registering Conditions");
		EzIPC.Init(this, "ConditionsManager");
		Svc.PluginInterface.ActivePluginsChanged += PluginsChanged;
	}

	public void PluginsChanged(IActivePluginsChangedEventArgs args)
	{
		if (args.Kind != PluginListInvalidationKind.Unloaded) return;
		foreach (string internalName in args.AffectedInternalNames)
		{
			conditions.Remove(internalName);
		}
	}

	[EzIPC]
	public bool RegisterCondition(ConditionType conditionType, string conditionName, string label, IpcContext context)
	{
		if (context.SourcePlugin is null)
		{
			Svc.Log.Warning($"Attempted to register a condition with no source plugin: \"{conditionName}\"");
			return false;
		}

		var conditionsFromPlugin =
			conditions.GetOrCreate(context.SourcePlugin.InternalName, []);
		if (conditionsFromPlugin.ContainsKey(conditionName))
		{
			Svc.Log.Warning(
				$"Attempted to register a condition that already exists: \"{conditionName}\" from {context.SourcePlugin.Name}");
			return false;
		}

		conditionsFromPlugin[conditionName] = ExtraCondition.Create(conditionType, context.SourcePlugin.InternalName, conditionName, label);
		var configConditionsFromPlugin = C.Extra_Conditions.GetOrCreate(context.SourcePlugin.InternalName, []);
		configConditionsFromPlugin.TryAdd(conditionName, true);
		Svc.Log.Debug(
			$"Registered condition \"{conditionName}\" from {context.SourcePlugin.Name} of type: {conditionsFromPlugin[conditionName].GetType()}");
		return true;
	}

	[EzIPC]
	public bool UpdateFilter(string conditionName, [CanBeNull] List<(string name, string label)> allItems,
		[CanBeNull] HashSet<string> currentItems, IpcContext context)
	{
		if (context.SourcePlugin is null)
		{
			Svc.Log.Warning($"Attempted to register a condition with no source plugin: \"{conditionName}\"");
			return false;
		}
		if (!conditions.TryGetValue(context.SourcePlugin.InternalName, out var conditionsFromPlugin) || !conditionsFromPlugin.TryGetValue(conditionName, out var condition))
		{
			Svc.Log.Warning($"Attempted to update filters on: \"{conditionName}\" from {context.SourcePlugin.Name}, which doesn't exist.");
			return false;
		}
		if (condition is not ExtraConditionFilter filterCondition)
		{
			Svc.Log.Warning($"Attempted to update filters on: \"{conditionName}\" from {context.SourcePlugin.Name}, with incorrect type: {condition.GetType()}");
			return false;
		}

		if (allItems is not null)
		{
			filterCondition.allItems = allItems;
		}

		if (currentItems is not null)
		{
			filterCondition.currentItems = currentItems;
		}
		return true;
	}

	public void Dispose()
	{
		Svc.PluginInterface.ActivePluginsChanged -= PluginsChanged;
	}
}

public abstract class ExtraCondition(string sourcePlugin, string conditionName, string label)
{
	public readonly string sourcePlugin = sourcePlugin;
	public readonly string conditionName = conditionName;
	public readonly string label = label;

	public static ExtraCondition Create(ConditionType conditionType, string sourcePlugin, string conditionName, string label)
	{
		return conditionType switch
		{
			ConditionType.FilterAny => new ExtraConditionFilterAny(sourcePlugin, conditionName, label),
			ConditionType.FilterAll => new ExtraConditionFilterAll(sourcePlugin, conditionName, label),
			_ => throw new ArgumentOutOfRangeException(nameof(conditionType))
		};
	}

	public abstract void Draw(ApplyRule rule);
	public abstract bool IsValid(HashSet<string> items, HashSet<string> notItems);
}

public abstract class ExtraConditionFilter(string sourcePlugin, string conditionName, string label) : ExtraCondition(sourcePlugin, conditionName, label)
{
	public List<(string name, string label)> allItems = [];
	public HashSet<string> currentItems = [];
	private string filter = "";
	private bool onlySelected;

	public override void Draw(ApplyRule rule)
	{
		HashSet<string> selectedItems = rule.Extra_Conditions.GetOrCreate(sourcePlugin, []).GetOrCreate(conditionName, []);
		HashSet<string> notSelectedItems = rule.Not.Extra_Conditions.GetOrCreate(sourcePlugin, []).GetOrCreate(conditionName, []);
		if (ImGui.BeginCombo($"##extraCondition{sourcePlugin}_{conditionName}", selectedItems.PrintRange(notSelectedItems, out var fullList), C.ComboSize))
		{
			ImGui.SetWindowFontScale(0.8f);
			ImGuiEx.SetNextItemFullWidth();
			ImGui.InputTextWithHint($"##fltr{sourcePlugin}_{conditionName}", "Filter...", ref filter, 50);
			ImGui.Checkbox($"Only selected##{sourcePlugin}_{conditionName}", ref onlySelected);
			ImGui.SetWindowFontScale(1f);
			ImGui.Separator();
			foreach (var cond in allItems)
			{
				var name = cond.label;
				if (filter.Length > 0 &&
				    !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;
				if (onlySelected && !selectedItems.Contains(cond.name)) continue;

				GuiRules.DrawSelector(name, cond.name, selectedItems, notSelectedItems);
			}

			ImGui.EndCombo();
		}

		if (fullList != null) ImGuiEx.Tooltip(UI.AnyNotice + fullList);
	}
}

public class ExtraConditionFilterAny(string sourcePlugin, string conditionName, string label) : ExtraConditionFilter(sourcePlugin, conditionName, label)
{
	public override bool IsValid(HashSet<string> items, HashSet<string> notItems) => (items.Count == 0 || currentItems.ContainsAny(items)) && !currentItems.ContainsAny(notItems);
}

public class ExtraConditionFilterAll(string sourcePlugin, string conditionName, string label) : ExtraConditionFilter(sourcePlugin, conditionName, label)
{
	public override bool IsValid(HashSet<string> items, HashSet<string> notItems)
	{
		return currentItems.ContainsAll(items) && (notItems.Count == 0 || !currentItems.ContainsAll(notItems));
	}
}

public enum ConditionType
{
	FilterAny,
	FilterAll
}