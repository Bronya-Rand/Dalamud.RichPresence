using System;
using System.Numerics;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;

namespace Dalamud.RichPresence.Windows.Sections;

public sealed class RpcLinuxNode : ResNode
{
    public RpcLinuxNode(Vector2 contentSize, Configuration configuration, Action reloadClient)
    {
        Size = new Vector2(contentSize.X, 82f);

        var port = ConfigUiSettings.ClampPort(configuration.RpcTcpBridgePort);
        var portInput = new NumericInputNode
        {
            Width = 200f,
            Height = 28f,
            Min = ConfigUiSettings.MinPort,
            Max = ConfigUiSettings.MaxPort,
            Value = port,
            TextTooltip = ConfigUiSettings.PortTooltip,
            OnValueUpdate = value => port = ConfigUiSettings.ClampPort(value)
        };

        var portRow = new HorizontalListNode
        {
            Width = contentSize.X,
            Height = 28f,
            FitHeight = true,
            InitialNodes =
            [
                new TextNode
                {
                    String = ConfigUiSettings.PortLabel,
                    Width = Size.X - 200f - 140f,
                    TextTooltip =
                        ConfigUiSettings.PortTooltip
                },
                portInput,
                new TextButtonNode
                {
                    String = "Save Port",
                    Width = 90f,
                    Height = 28f,
                    OnClick = () => { ConfigUiSettings.SavePort(configuration, port, reloadClient); }
                }
            ]
        };

        new VerticalListNode
        {
            Size = Size,
            FitWidth = true,
            ItemSpacing = 4f,
            InitialNodes =
            [
                new TextNode
                {
                    String = ConfigUiSettings.WineSectionTitle,
                    Width = contentSize.X,
                    Height = 14f,
                    FontSize = 14,
                    TextColor = ColorHelper.GetColor(2)
                },
                new CheckboxNode
                {
                    String = ConfigUiSettings.CustomPortLabel,
                    Width = contentSize.X,
                    Height = 22f,
                    TextTooltip = ConfigUiSettings.CustomPortTooltip,
                    IsChecked = configuration.UseCustomRpcTcpBridgePort,
                    OnClick = value =>
                    {
                        portRow.IsVisible = value;
                        ConfigUiSettings.SaveCustomPortEnabled(configuration, value, reloadClient);
                    }
                },
                portRow
            ]
        }.AttachNode(this);
        portRow.IsVisible = configuration.UseCustomRpcTcpBridgePort;
    }
}