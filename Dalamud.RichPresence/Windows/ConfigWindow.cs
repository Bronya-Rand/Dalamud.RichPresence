using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.RichPresence.Models;
using Dalamud.Utility;
using System;
using System.Numerics;

namespace Dalamud.RichPresence.Windows
{
    internal class ConfigWindow : Window, IDisposable
    {
        private readonly Configuration configuration;
        public ConfigWindow(Plugin plugin) : base($"{Plugin.LocalizationService.Localize("DalamudRichPresenceConfiguration", LocalizationLanguage.Plugin)}##DiscordRPCSettings")
        {
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar;

            Size = new Vector2(750, 520);
            SizeCondition = ImGuiCond.Always;

            configuration = plugin.Configuration;
        }

        public override void Draw()
        {
            ImGui.Text(Plugin.LocalizationService.Localize("DalamudRichPresencePreface1", LocalizationLanguage.Plugin));
            ImGui.Text(Plugin.LocalizationService.Localize("DalamudRichPresencePreface2", LocalizationLanguage.Plugin));
            ImGui.Separator();

            ImGui.BeginChild("scrolling", ImGuiHelpers.ScaledVector2(0, 400), true, ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGuiHelpers.ScaledVector2(1, 3));

            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowName", LocalizationLanguage.Plugin), ref configuration.ShowName);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowFreeCompany", LocalizationLanguage.Plugin), ref configuration.ShowFreeCompany);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowWorld", LocalizationLanguage.Plugin), ref configuration.ShowWorld);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceAlwaysShowHomeWorld", LocalizationLanguage.Plugin), ref configuration.AlwaysShowHomeWorld);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowDataCenter", LocalizationLanguage.Plugin), ref configuration.ShowDataCenter);
            ImGui.Separator();
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowStartTime", LocalizationLanguage.Plugin), ref configuration.ShowStartTime);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceResetTimeWhenChangingZones", LocalizationLanguage.Plugin), ref configuration.ResetTimeWhenChangingZones);
            ImGui.Separator();
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowLoginQueuePosition", LocalizationLanguage.Plugin), ref configuration.ShowLoginQueuePosition);
            ImGui.TextColored(ImGuiColors.DalamudGrey, Plugin.LocalizationService.Localize("DalamudRichPresenceShowLoginQueuePositionDetail", LocalizationLanguage.Plugin));
            ImGui.Separator();
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowJob", LocalizationLanguage.Plugin), ref configuration.ShowJob);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceAbbreviateJob", LocalizationLanguage.Plugin), ref configuration.AbbreviateJob);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowLevel", LocalizationLanguage.Plugin), ref configuration.ShowLevel);
            ImGui.Separator();
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowParty", LocalizationLanguage.Plugin), ref configuration.ShowParty);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceShowAFK", LocalizationLanguage.Plugin), ref configuration.ShowAfk);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceHideAFKEntirely", LocalizationLanguage.Plugin), ref configuration.HideEntirelyWhenAfk);
            ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceHideInCutscene", LocalizationLanguage.Plugin), ref configuration.HideInCutscene);

            if (Util.IsWine())
            {
                ImGui.Separator();
                ImGui.Checkbox(Plugin.LocalizationService.Localize("DalamudRichPresenceRPCBridgeEnabled", LocalizationLanguage.Plugin), ref configuration.RPCBridgeEnabled);
                ImGui.TextColored(ImGuiColors.DalamudGrey, Plugin.LocalizationService.Localize("DalamudRichPresenceRPCBridgeEnabledDetail", LocalizationLanguage.Plugin));
            }

            ImGui.PopStyleVar();

            ImGui.EndChild();

            ImGui.Separator();

            // TODO: Save and close button
            //if (ImGui.Button(Plugin.LocalizationService.Localize("DalamudRichPresenceSaveAndClose", LocalizationLanguage.Plugin)))
            //{
            //    Plugin.DalamudPluginInterface.SavePluginConfig(this.configuration);
            //    Plugin.configuration = this.configuration;
            //    Plugin.PluginLog.Information("Settings saved.");
            //    this.
            //}
        }

        public void Dispose() => GC.SuppressFinalize(this);
    }
}