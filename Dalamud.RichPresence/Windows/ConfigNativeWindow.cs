using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Dalamud.RichPresence.Helpers;
using Dalamud.RichPresence.Services;
using Dalamud.RichPresence.Windows.Sections;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit.BaseTypes;
using KamiToolKit.Classes;
using KamiToolKit.Enums;
using KamiToolKit.Nodes;

namespace Dalamud.RichPresence.Windows;

public sealed class ConfigNativeWindow : NativeAddon
{
    private const float CheckboxHeight = 22f;
    private const float TemplateNodeHeight = 185f;
    private readonly Configuration configuration;
    private readonly Plugin plugin;
    private TagModal? tagModal;

    // Editable configuration fields
    private readonly List<Action> refreshPreviews = [];

    // For previewing what gets sent to Discord with live data
    private PlayerContext previewPlayer;
    private OnlineStatusContext previewStatus;

    [SetsRequiredMembers]
    public ConfigNativeWindow(Plugin plugin)
    {
        Title = "RPC Configuration";
        InternalName = "DiscordRPCSettings";

        this.configuration = plugin.Configuration;
        this.plugin = plugin;
    }

    protected override unsafe void OnSetup(AtkUnitBase* addon, Span<AtkValue> atkValueSpan)
    {
        base.OnSetup(addon, atkValueSpan);
        SetWindowSize(540, 475);

        refreshPreviews.Clear();
        tagModal = new TagModal();

        (previewPlayer, previewStatus) = ConfigUiSettings.GetPreviewContext(plugin);

        new VerticalListNode
        {
            Position = ContentStartPosition,
            Size = ContentSize,
            FitWidth = true,
            ItemSpacing = 8f,
            InitialNodes =
            [
                BuildTemplateFields(),
                new HorizontalLineNode
                {
                    Width = ContentSize.X,
                    Height = 4f
                },
                BuildScrollArea()
            ]
        }.AttachNode(this);
    }

    private ScrollingNode<VerticalListNode> BuildScrollArea()
    {
        var checkboxFields = BuildCheckboxFields();

        var scrollArea = new ScrollingNode<VerticalListNode>
        {
            AutoHideScrollBar = true,
            Width = ContentSize.X,
            Height = ContentSize.Y - TemplateNodeHeight - 20f,
            ContentNode =
            {
                FitContents = true,
                ItemSpacing = 4f,
                InitialNodes =
                [
                    new TextNode
                    {
                        Width = Size.X,
                        Height = 14f,
                        String = "Rich Presence Conditions",
                        FontSize = 14,
                        TextColor = ColorHelper.GetColor(2)
                    },
                    checkboxFields,
                    new HorizontalLineNode
                    {
                        Width = ContentSize.X,
                        Height = 4f,
                        IsVisible = ConfigUiSettings.ShowBridgeSettings
                    },
                    new RpcLinuxNode(ContentSize, configuration, () => plugin.DiscordService.ReloadClient())
                    {
                        IsVisible = ConfigUiSettings.ShowBridgeSettings
                    },
                    new HorizontalLineNode
                    {
                        Width = ContentSize.X,
                        Height = 4f
                    },
                    new VerticalListNode
                    {
                        Width = ContentSize.X,
                        Height = 44f,
                        ItemSpacing = 4f,
                        InitialNodes =
                        [
                            new TextNode
                            {
                                Width = Size.X,
                                Height = 14f,
                                String = "UI",
                                FontSize = 14,
                                TextColor = ColorHelper.GetColor(2)
                            },
                            BuildCheckbox(ConfigUiSettings.NativeUi)
                        ]
                    }
                ]
            }
        };

        return scrollArea;
    }

    private CheckboxNode BuildCheckbox(CheckboxSetting setting) => new()
    {
        String = setting.Label,
        Width = ContentSize.X,
        Height = CheckboxHeight,
        TextTooltip = setting.Tooltip,
        IsChecked = setting.Get(configuration),
        OnClick = value => setting.Save(configuration, value)
    };

    private VerticalListNode BuildCheckboxFields() => new()
    {
        Width = ContentSize.X,
        FitContents = true,
        InitialNodes = [.. ConfigUiSettings.Conditions.Select(BuildCheckbox)]
    };

    private VerticalListNode BuildTemplateFields() => new()
    {
        Size = ContentSize,
        FitWidth = true,
        Height = TemplateNodeHeight,
        ItemSpacing = 6f,
        InitialNodes = [.. ConfigUiSettings.Templates.Select(BuildTemplateInput)]
    };

    private HorizontalListNode BuildTemplateInput(TemplateSetting setting)
    {
        var draft = setting.Get(configuration);
        var previewNode = new TextNode
        {
            Width = 225f,
            Height = 14f,
            TextColor = ColorHelper.GetColor(4)
        };
        refreshPreviews.Add(RefreshPreview);
        RefreshPreview();

        var inputNode = new TextInputNode
        {
            Width = 225f,
            Height = 28f,
            String = draft,
            OnInputReceived = value =>
            {
                draft = value.ToString();
                RefreshPreview();
            },
        };

        void InsertTag(TagDefinition tag)
        {
            draft += $"{{{tag.Tag}}}";
            inputNode.String = draft;
            RefreshPreview();
        }

        return new HorizontalListNode // Body
        {
            Width = Size.X,
            Height = 42f,
            InitialNodes =
            [
                new TextNode
                {
                    Width = ContentSize.X - 225f - 28f - 140f,
                    Height = 28f,
                    String = setting.Label,
                    TextTooltip = setting.Tooltip,
                },
                new VerticalListNode
                {
                    Width = 225f,
                    Height = 42f,
                    InitialNodes =
                    [
                        inputNode,
                        previewNode
                    ]
                },
                new CircleButtonNode
                {
                    Icon = CircleButtonIcon.MagnifyingGlass,
                    Width = 28f,
                    Height = 28f,
                    TextTooltip = $"Insert a tag into {setting.Label}",
                    OnClick = () => tagModal?.OpenFor(setting.Label, InsertTag)
                },
                new TextButtonNode
                {
                    String = "Reset",
                    Width = 70f,
                    Height = 28f,
                    OnClick = () =>
                    {
                        draft = setting.DefaultValue;
                        inputNode.String = draft;
                        setting.Save(configuration, draft);
                        RefreshPreview();
                    }
                },
                new TextButtonNode
                {
                    String = "Save",
                    Width = 70f,
                    Height = 28f,
                    OnClick = () => { setting.Save(configuration, draft); }
                }
            ]
        };

        void RefreshPreview()
        {
            var text = ParserService.Parse(draft, previewPlayer, previewStatus, configuration);
            if (previewNode.String.ToString() != text)
                previewNode.String = text;
        }
    }

    protected override unsafe void OnDraw(AtkUnitBase* addon)
    {
        base.OnDraw(addon);
        (previewPlayer, previewStatus) = ConfigUiSettings.GetPreviewContext(plugin);
        foreach (var refresh in refreshPreviews)
            refresh();
    }

    protected override unsafe void OnHide(AtkUnitBase* addon)
    {
        tagModal?.CancelSelection();
        base.OnHide(addon);
    }

    protected override unsafe void OnFinalize(AtkUnitBase* addon)
    {
        tagModal?.CancelSelection();
        tagModal?.Dispose();
        tagModal = null;
        refreshPreviews.Clear();
        base.OnFinalize(addon);
    }
}