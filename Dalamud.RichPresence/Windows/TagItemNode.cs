using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.Classes;
using KamiToolKit.Interfaces;
using KamiToolKit.Nodes;

namespace Dalamud.RichPresence.Windows;

public class TagItemNode : ListItemWithFocusNav<TagDefinition>, IListItemNode
{
    /// <inheritdoc/>
    public static float ItemHeight => 50f;

    /// <summary>
    /// Gets the text node used to display the tag name.
    /// </summary>
    private TextNode TagNameTextNode { get; }

    /// <summary>
    /// Gets the text node used to display the class description.
    /// </summary>
    private TextNode TagDescriptionTextNode { get; }

    protected override void SetNodeData(TagDefinition tag)
    {
        TagNameTextNode.String = $"{{{tag.Tag}}}";
        TagNameTextNode.IsVisible = true;

        TagDescriptionTextNode.String = tag.Description;
        TagDescriptionTextNode.IsVisible = true;
    }

    public TagItemNode()
    {
        TagNameTextNode = new TextNode
        {
            FontSize = 14
        };
        TagNameTextNode.AttachNode(this);

        TagDescriptionTextNode = new TextNode
        {
            TextColor = ColorHelper.GetColor(3),
            TextFlags = TextFlags.MultiLine | TextFlags.WordWrap,
        };
        TagDescriptionTextNode.AttachNode(this);
    }

    protected override void OnSizeChanged()
    {
        base.OnSizeChanged();

        TagNameTextNode.Size = new Vector2(Width - 4f, 22f);

        TagDescriptionTextNode.Size = new Vector2(Width - 4f, 30f);
        TagDescriptionTextNode.Position = new Vector2(0f, (Height / 2f) - 5f);
    }
}