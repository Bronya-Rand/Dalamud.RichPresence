using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace Dalamud.RichPresence.Windows;

public sealed class ChooseUiWindow : NativeAddon
{
    private const string DescriptionText =
        "Discord Rich Presence now uses the game’s native interface by default. You can keep using Native UI or switch back to old ImGui interface instead.";

    private readonly Action<bool> onUiChosen;

    [SetsRequiredMembers]
    public ChooseUiWindow(Action<bool> onUiChosen)
    {
        Title = "Choose UI";
        InternalName = "DiscordRPCChooseUIWindow";

        this.onUiChosen = onUiChosen;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        base.OnSetup(addon, atkValueSpan);
        SetWindowSize(420, 180);

        var descriptionNode = new TextNode
        {
            Width = ContentSize.X,
            Height = 45f,
            TextFlags = TextFlags.WordWrap | TextFlags.MultiLine,
            AlignmentType = AlignmentType.Center,
            TextColor = ColorHelper.GetColor(2),
            FontSize = 14,
            LineSpacing = 18,
        };

        var layout = new VerticalListNode
        {
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            FirstItemSpacing = 5f,
            ItemSpacing = 12f,
            InitialNodes =
            [
                descriptionNode,
                new TextNode
                {
                    Width = Size.X,
                    Height = 12f,
                    String = "This setting can be changed later in Settings -> UI.",
                    AlignmentType = AlignmentType.Center,
                    TextColor = ColorHelper.GetColor(3)
                },
                BuildUiButtons()
            ]
        };

        layout.AttachNode(this);

        descriptionNode.String = DescriptionText;
        layout.RecalculateLayout();
    }

    private HorizontalFlexNode BuildUiButtons()
    {
        return new HorizontalFlexNode
        {
            AlignmentFlags = FlexFlags.CenterHorizontally | FlexFlags.FitWidth,
            Position = new Vector2(0f, 28f),
            Size = new Vector2(Size.X - 40f, 28f),
            ItemSpacing = 5f,
            InitialNodes =
            [
                new TextButtonNode
                {
                    String = "Keep Native UI",
                    Height = 28f,
                    OnClick = () => onUiChosen(true)
                },
                new TextButtonNode
                {
                    String = "Switch Back to ImGui",
                    Height = 28f,
                    OnClick = () => onUiChosen(false)
                },
            ],
        };
    }
}