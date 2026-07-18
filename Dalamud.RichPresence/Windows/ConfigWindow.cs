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
using Dalamud.Utility;

namespace Dalamud.RichPresence.Windows
{
    public sealed class ConfigWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        private readonly Configuration configuration;

        private string editingDetail = string.Empty;
        private string editingState = string.Empty;
        private string editingLargeImageText = string.Empty;
        private string editingSmallImageText = string.Empty;

        // For showing a live preview of what gets sent to Discord.
        private PlayerContext previewPlayer;
        private PartyContext previewParty;
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
        private void UpdatePreviewContext()
        {
            if (Plugin.ObjectTable.LocalPlayer != null)
            {
                previewPlayer = plugin.CollectContext.GetPlayerStatus();
                previewParty = plugin.CollectContext.GetPartyStatus();
                previewStatus = CollectContext.OnlineStatus;
            }
            else
            {
                previewPlayer = new PlayerContext(
                    PlayerName: "Y'shtola Rhul",
                    FcTag: "FFXIV",
                    CurrentWorldId: 0,
                    CurrentWorld: "Eorzea",
                    HomeWorldId: 0,
                    HomeWorld: "Hydaleyn",
                    IsOnHomeWorld: true,
                    DataCenterName: "Scion",
                    TerritoryName: "Limsa Lominsa Lower Decks",
                    TerritoryLoadingImageId: 1,
                    WardId: 0,
                    ClassJobId: 0,
                    ClassJob: "Black Mage",
                    ClassJobAbbreviation: "BLM",
                    Level: 100
                );

                previewParty = new PartyContext(
                    InParty: true,
                    InDuty: false,
                    IsPartyCrossRealm: false,
                    PartySize: 4,
                    PartyMaxSize: 8,
                    HashedPartyId: "mock-party-id"
                );

                previewStatus = new OnlineStatusContext(
                    IsAfk: false,
                    WatchingCutscene: false,
                    StatusName: "Online"
                );
            }
        }

        /// <summary>
        /// Sets the editing fields for the config window.
        /// </summary>
        public override void OnOpen()
        {
            editingDetail = configuration.DiscordDetailField;
            editingState = configuration.DiscordStateField;
            editingLargeImageText = configuration.DiscordLargeImageTextField;
            editingSmallImageText = configuration.DiscordSmallImageTextField;
        }

        public override void Draw()
        {
            UpdatePreviewContext();

            ImGui.Text("Rich Presence Template");
            ImGui.Spacing();

            DrawTemplateInput("Details", "Type a custom message",
                "Sets the top part of the Discord RPC header below the game's name.",
                ref editingDetail,
                v => configuration.DiscordDetailField = v,
                () => { editingDetail = Constants.DefaultDiscordDetailStr; });

            DrawTemplateInput("State", "Type a custom message",
                "Sets the bottom part of the Discord RPC header below the game's name.",
                ref editingState,
                v => configuration.DiscordStateField = v,
                () => { editingState = Constants.DefaultDiscordStateStr; });

            DrawTemplateInput("Large Image Text", "Type a custom message",
                "Sets the text that appears when hovering over the large image.",
                ref editingLargeImageText,
                v => configuration.DiscordLargeImageTextField = v,
                () => { editingLargeImageText = Constants.DefaultDiscordLargeImageStr; });
            DrawTemplateInput("Small Image Text", "Type a custom message",
                "Sets the text that appears when hovering over the small image.",
                ref editingSmallImageText,
                v => configuration.DiscordSmallImageTextField = v,
                () => { editingSmallImageText = Constants.DefaultDiscordSmallImageStr; });

            ImGui.Spacing();
            DrawTagGuideHeader();

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text("Rich Presence Conditions");
            ImGui.Spacing();

            var showJobIcon = configuration.ShowJobIcon;
            if (ImGui.Checkbox("Display Job Icon for Small Image", ref showJobIcon))
            {
                configuration.ShowJobIcon = showJobIcon;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Displays your current job class icon instead of the online status icon for the small image in Discord RPC.");

            var shortenJobName = configuration.AbbreviateJob;
            if (ImGui.Checkbox("Abbreviate Job Name", ref shortenJobName))
            {
                configuration.AbbreviateJob = shortenJobName;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Shortens the Job Name from 'Summoner' to 'SMN'.");

            var showPartyData = configuration.ShowPartyData;
            if (ImGui.Checkbox("Display Party Information", ref showPartyData))
            {
                configuration.ShowPartyData = showPartyData;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Displays how many people are in your party in Discord RPC (e.g. 7 of 8 players).");

            var showTimestamps = configuration.DisplayDiscordTimestamp;
            if (ImGui.Checkbox("Display Timestamps", ref showTimestamps))
            {
                configuration.DisplayDiscordTimestamp = showTimestamps;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Displays the time you have been in a region/on XIV.");

            var showQueue = configuration.ShowLoginQueuePosition;
            if (ImGui.Checkbox("Display Queue Times [Waitingway]", ref showQueue))
            {
                configuration.ShowLoginQueuePosition = showQueue;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Displays the queue time when logging in (requires Waitingway).");

            var showAfkStatus = configuration.ShowAfk;
            if (ImGui.Checkbox("Display AFK Status", ref showAfkStatus))
            {
                configuration.ShowAfk = showAfkStatus;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Displays your AFK status to Discord");

            var hideAllWhileAfk = configuration.HideEntirelyWhenAfk;
            if (ImGui.Checkbox("Hide RPC Info Whilst AFK", ref hideAllWhileAfk))
            {
                configuration.HideEntirelyWhenAfk = hideAllWhileAfk;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hides most information about you when you are AFK to Discord.");

            var hideInCutscenes = configuration.HideInCutscene;
            if (ImGui.Checkbox("Hide RPC Info Whilst In Cutscene", ref hideInCutscenes))
            {
                configuration.HideInCutscene = hideInCutscenes;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Hides most information about you when you are watching a cutscene.");

            var resetTimerOnZoneChange = configuration.ResetTimeWhenChangingZones;
            if (ImGui.Checkbox("Reset Timestamps When Changing Zones", ref resetTimerOnZoneChange))
            {
                configuration.ResetTimeWhenChangingZones = resetTimerOnZoneChange;
                configuration.Save();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Resets the timestamp timer when changing zones/duties. Otherwise, the timer will count based off login time.");

            if (Util.IsWine())
            {
                var useWineBridge = configuration.RpcBridgeEnabled;
                if (ImGui.Checkbox("Use Wine RPC Bridge", ref useWineBridge))
                {
                    configuration.RpcBridgeEnabled = useWineBridge;
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Enables Discord RPC for Wine (macOS/Linux) users.");
            }
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
                    if (ImGui.GetCursorPosX() + tagWidth > availableWidth && ImGui.GetCursorPosX() > style.WindowPadding.X)
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
        private void DrawTemplateInput(string label, string hint, string tooltip, ref string draft, Action<string> setter, Action reset)
        {
            using (ImRaii.PushId(label))
            {
                ImGui.InputTextWithHint($"##{label}_input", hint, ref draft, 128);
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(tooltip);

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Recycle))
                {
                    reset();
                    setter(draft);
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Reset to Defaults");

                ImGui.SameLine();
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Save))
                {
                    setter(draft);
                    configuration.Save();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Save Changes");

                ImGui.SameLine();
                ImGui.Text(label);

                // Preview the result of the template parsing
                var previewText = ParserService.Parse(draft, previewPlayer, previewParty, previewStatus, configuration);
                ImGui.TextColored(ImGuiColors.DalamudGrey, previewText);
                ImGui.Spacing();
            }
        }

        public void Dispose() => GC.SuppressFinalize(this);
    }
}