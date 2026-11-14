using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.RichPresence.Helpers;
using Dalamud.RichPresence.Services;

namespace Dalamud.RichPresence.Windows
{
    public sealed class ConfigWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        // Editable configuration fields
        private readonly string[] templateDrafts = new string[ConfigUiSettings.Templates.Length];
        private int editingPort;

        // For previewing what gets sent to Discord with live data
        private PlayerContext previewPlayer;
        private OnlineStatusContext previewStatus;

        public ConfigWindow(Plugin plugin) :
            base($"Discord Rich Presence##DiscordRPCSettings")
        {
            Flags = ImGuiWindowFlags.NoCollapse
                    | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.HorizontalScrollbar;

            Size = new Vector2(500, 475);
            SizeCondition = ImGuiCond.Always;

            this.plugin = plugin;
            configuration = plugin.Configuration;
        }

        /// <summary>
        /// Sets the editing fields for the config window.
        /// </summary>
        public override void OnOpen()
        {
            for (var index = 0; index < templateDrafts.Length; index++)
                templateDrafts[index] = ConfigUiSettings.Templates[index].Get(configuration);
            editingPort = ConfigUiSettings.ClampPort(configuration.RpcTcpBridgePort);
        }

        public override void Draw()
        {
            (previewPlayer, previewStatus) = ConfigUiSettings.GetPreviewContext(plugin);

            for (var index = 0; index < templateDrafts.Length; index++)
                DrawTemplateInput(ConfigUiSettings.Templates[index], ref templateDrafts[index]);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawTagGuideHeader();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("Rich Presence Conditions");
            ImGui.Spacing();

            foreach (var setting in ConfigUiSettings.Conditions)
                DrawCheckbox(setting);

            // Only show if on Wine/Proton and AfUnix is not supported
            if (ConfigUiSettings.ShowBridgeSettings)
            {
                ImGui.Text(ConfigUiSettings.WineSectionTitle);
                ImGui.Spacing();

                var useCustomPort = configuration.UseCustomRpcTcpBridgePort;
                if (ImGui.Checkbox($"{ConfigUiSettings.CustomPortLabel}###RPCWineBridgeUseUserPort", ref useCustomPort))
                    ConfigUiSettings.SaveCustomPortEnabled(configuration, useCustomPort,
                        () => plugin.DiscordService.ReloadClient());

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(
                        ConfigUiSettings.CustomPortTooltip);

                ImGui.Spacing();

                using (ImRaii.Disabled(!configuration.UseCustomRpcTcpBridgePort))
                {
                    if (ImGui.InputInt($"{ConfigUiSettings.PortLabel}###RPCWineBridgeUserPort", ref editingPort))
                    {
                        editingPort = ConfigUiSettings.ClampPort(editingPort);
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(
                            ConfigUiSettings.PortTooltip);
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                        ConfigUiSettings.SavePort(configuration, editingPort,
                            () => plugin.DiscordService.ReloadClient());
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.Text("UI");

            DrawCheckbox(ConfigUiSettings.NativeUi);
        }

        private void DrawCheckbox(CheckboxSetting setting)
        {
            var value = setting.Get(configuration);
            if (ImGui.Checkbox($"{setting.Label}###{setting.Id}", ref value))
                setting.Save(configuration, value);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(setting.Tooltip);
        }

        private static void DrawTagGuideHeader()
        {
            if (ImGui.CollapsingHeader("Available Tags"))
            {
                var availableWidth = ImGui.GetContentRegionAvail().X;
                var style = ImGui.GetStyle();

                foreach (var (tag, description) in Constants.AvailableTags)
                {
                    var properTag = '{' + tag + '}';
                    var tagWidth = ImGui.CalcTextSize(tag).X + style.FramePadding.X * 2;

                    // Wrap to next line if this tag won't fit
                    if (ImGui.GetCursorPosX() + tagWidth > availableWidth &&
                        ImGui.GetCursorPosX() > style.WindowPadding.X)
                        ImGui.NewLine();

                    using (ImRaii.PushColor(ImGuiCol.Button, ImGuiColors.ParsedGold with { W = 0.2f }))
                    using (ImRaii.PushColor(ImGuiCol.ButtonHovered, ImGuiColors.ParsedGold with { W = 0.4f }))
                    {
                        if (ImGui.Button(properTag))
                        {
                            ImGui.SetClipboardText(properTag);
                        }
                    }

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip(description);

                    ImGui.SameLine();
                }

                ImGui.NewLine();
            }
        }

        private void DrawTemplateInput(TemplateSetting setting, ref string draft)
        {
            using (ImRaii.PushId(setting.Label))
            {
                ImGui.InputTextWithHint($"##{setting.Label}_input", "Type a custom message", ref draft, 128);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(setting.Tooltip);

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                {
                    draft = setting.DefaultValue;
                    setting.Save(configuration, draft);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Reset to Defaults");

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                {
                    setting.Save(configuration, draft);
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Save Changes");

                ImGui.SameLine();
                ImGui.Text(setting.Label);

                // Preview the result of the template parsing
                var previewText = ParserService.Parse(draft, previewPlayer, previewStatus, configuration);
                ImGui.TextColored(ImGuiColors.DalamudGrey, previewText);
                ImGui.Spacing();
            }
        }

        public void Dispose()
        {
        }
    }
}