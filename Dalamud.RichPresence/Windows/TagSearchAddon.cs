using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Components.Search;
using KamiToolKit.Extensions;
using Lumina.Text.ReadOnly;

namespace Dalamud.RichPresence.Windows;

public class TagSearchAddon : AbstractSearchAddon<TagDefinition, TagItemNode>
{
    private string windowTitle = "Tag Search";

    [SetsRequiredMembers]
    public TagSearchAddon()
    {
        Title = "Select Tags";
        InternalName = "RPCTagSearchAddon";
        AllowMultiselect = true;
    }

    public void OpenFor(string fieldName, Action<List<TagDefinition>> insertTag)
    {
        windowTitle = $"Insert Tag into {fieldName}";
        if (IsOpen)
            Title = windowTitle;

        ConfirmedSelections = insertTag;
        Open();
    }

    public void CancelSelection()
    {
        ConfirmedSelections = null;
        Close();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        Title = windowTitle;
        OptionsList = [.. Constants.AvailableTags];

        base.OnSetup(addon, atkValueSpan);
    }

    protected override void OnSearchInputReceived(ReadOnlySeString searchString)
    {
        var searchRegex = searchString.AsRegex();

        ResultsListNode?.OptionsList = [.. OptionsList.Where(option => searchRegex.IsMatch(option.Tag))];
        base.OnSearchInputReceived(searchString);
    }
}