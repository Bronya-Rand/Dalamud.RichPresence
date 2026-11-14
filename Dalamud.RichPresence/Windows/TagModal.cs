using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace Dalamud.RichPresence.Windows;

public sealed class TagModal : NativeAddon
{
    private const float RowHeight = 50f;
    private readonly List<(TagDefinition Tag, ResNode Row)> rows = [];
    private Action<TagDefinition>? onTagSelect;
    private ScrollingNode<ResNode>? results;
    private TextNode? noResults;
    private string searchText = string.Empty;
    private string windowTitle = "Tag Search";

    [SetsRequiredMembers]
    public TagModal()
    {
        Title = "Tag Search";
        InternalName = "RPCTagSearch";
    }

    public void OpenFor(string fieldName, Action<TagDefinition> insertTag)
    {
        windowTitle = $"Insert Tag into {fieldName}";
        if (IsOpen)
            Title = windowTitle;
        onTagSelect = insertTag;
        Open();
    }

    public void CancelSelection()
    {
        onTagSelect = null;
        Close();
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        base.OnSetup(addon, atkValueSpan);
        Title = windowTitle;
        SetWindowSize(560f, 400f);
        searchText = string.Empty;
        rows.Clear();

        var search = new TextInputNode
        {
            Position = ContentStartPosition,
            PlaceholderString = "Search tags...",
            Width = ContentSize.X,
            Height = 28f,
            OnInputReceived = query =>
            {
                searchText = query.ExtractText().Trim();
                FilterTags();
            }
        };
        search.AttachNode(this);

        results = new ScrollingNode<ResNode>
        {
            Position = ContentStartPosition + new Vector2(0f, 36f),
            Size = new Vector2(ContentSize.X, ContentSize.Y - 36f),
            AutoHideScrollBar = true
        };
        results.AttachNode(this);

        // Reserve scrollbar space even when filtering reduces the result count.
        var rowWidth = ContentSize.X - 12f;
        foreach (var tag in Constants.AvailableTags)
        {
            var row = BuildTagRow(tag, rowWidth);
            row.RemoveNodeFlags(NodeFlags.AnchorBottom | NodeFlags.AnchorRight);
            row.AddNodeFlags(NodeFlags.AnchorTop | NodeFlags.AnchorLeft);
            row.AttachNode(results.ContentNode);
            rows.Add((tag, row));
        }

        noResults = new TextNode
        {
            String = "No matching tags.",
            Size = new Vector2(rowWidth, 28f)
        };

        noResults.AddNodeFlags(NodeFlags.AnchorTop | NodeFlags.AnchorLeft);
        noResults.AttachNode(results.ContentNode);
        FilterTags();
    }

    private ResNode BuildTagRow(TagDefinition tag, float width)
    {
        var row = new ResNode { Size = new Vector2(width, RowHeight) };
        var textWidth = width - 78f;

        new TextNode
        {
            String = $"{{{tag.Tag}}}",
            Size = new Vector2(textWidth, 22f),
            FontSize = 14
        }.AttachNode(row);

        new TextNode
        {
            String = tag.Description,
            Position = new Vector2(0f, 24f),
            Size = new Vector2(textWidth, 22f),
            TextFlags = TextFlags.MultiLine | TextFlags.WordWrap,
            TextColor = ColorHelper.GetColor(2)
        }.AttachNode(row);

        new TextButtonNode
        {
            String = "Insert",
            Position = new Vector2(width - 70f, (RowHeight - 28f) / 2f),
            Size = new Vector2(70f, 28f),
            OnClick = () =>
            {
                var insert = onTagSelect;
                onTagSelect = null;
                insert?.Invoke(tag);
                Close();
            }
        }.AttachNode(row);

        return row;
    }

    private void FilterTags()
    {
        if (results is null || noResults is null) return;

        var y = 0f;
        foreach (var (tag, row) in rows)
        {
            row.IsVisible = tag.Tag.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                            || tag.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase);
            if (!row.IsVisible) continue;
            row.Y = y;
            y += RowHeight + 4f;
        }

        noResults.IsVisible = y == 0f;
        results.ContentNode.Height = Math.Max(28f, y);
        results.ScrollToStart();
        results.RecalculateSizes();
        results.ScrollBarNode.UpdateScrollParams();
    }

    protected override unsafe void OnHide(AtkUnitBase* addon)
    {
        onTagSelect = null;
        base.OnHide(addon);
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon)
    {
        onTagSelect = null;
        rows.Clear();
        results = null;
        noResults = null;
        base.OnFinalize(addon);
    }
}