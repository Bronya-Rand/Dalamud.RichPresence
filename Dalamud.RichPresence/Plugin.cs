using System;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.RichPresence.Helpers;
using Dalamud.RichPresence.Models;
using Dalamud.RichPresence.Services;
using Dalamud.RichPresence.Services.Discord;
using Dalamud.RichPresence.Services.IPC;
using Dalamud.RichPresence.Windows;
using DiscordRPC;

namespace Dalamud.RichPresence;

public sealed class Plugin : IDalamudPlugin
{
    #region Dalamud Services
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; set; } = null!;
    [PluginService] internal static ICondition Condition { get; private set; } = null!;
    #endregion

    #region Plugin Managers and Services
    public Configuration Configuration { get; init; }
    private readonly WindowSystem windowSystem = new("RichPresence");
    private ConfigWindow ConfigWindow { get; init; }
    private LuminaService LuminaService { get; init; }
    internal static LocalizationService LocalizationService { get; private set; } = null!;
    internal static WaitingwayIPC WaitingwayIPC { get; private set; } = null!;
    private DiscordService DiscordService { get; init; }
    private CollectContext CollectContext { get; init; }
    #endregion

    #region Initialization Variables
    private const string PluginCommandName = "/prp";
    private DateTime startTime = DateTime.UtcNow;
    private bool inQueue;

    // Discord RPC defaults
    private const string DefaultLargeImageKey = "li_1";
    private const string DefaultSmallImageKey = "class_0";
    private static readonly DiscordRPC.RichPresence DefaultPresence = new()
    {
        Assets = new Assets
        {
            LargeImageKey = DefaultLargeImageKey,
            SmallImageKey = DefaultSmallImageKey,
        },
    };
    #endregion

    public Plugin()
    {
        // Load / create config
        var config = PluginInterface.GetPluginConfig();
        Configuration = Migrate.TryMigrateFromLegacyConfig(config) ?? new Configuration();

        // Initialize services and managers
        LuminaService = new LuminaService(DataManager);
        LocalizationService = new LocalizationService();
        WaitingwayIPC = new WaitingwayIPC();
        DiscordService = new DiscordService();
        CollectContext = new CollectContext(Configuration);

        ConfigWindow = new ConfigWindow(this);

        windowSystem.AddWindow(ConfigWindow);

        PluginInterface.UiBuilder.Draw += windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        SetDefaultPresence();
        Framework.Update += UpdatePresence;

        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        ClientState.TerritoryChanged += OnZoneChange;

        RegisterCommand();
        PluginInterface.LanguageChanged += ReregisterCommand;

        Log.Info("Loaded Discord RPC");
    }

    #region Plugin Commands
    private void RegisterCommand()
    {
        CommandManager.AddHandler(PluginCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Discord Rich Presence configuration window.",
            ShowInHelp = true,
        });
    }
    private static void UnregisterCommand() => CommandManager.RemoveHandler(PluginCommandName);
    private void ReregisterCommand(string _)
    {
        UnregisterCommand();
        RegisterCommand();
    }
    private void OnCommand(string command, string args)
    {
        ToggleConfigUi();
    }

    #endregion

    #region Event Handlers
    private void OnLogin() => UpdateStartTime();
    private void OnLogout(int type, int code)
    {
        CollectContext.ClearCache();
        SetDefaultPresence();
        UpdateStartTime();
    }
    private void OnZoneChange(uint _) => UpdateStartTime();

    #endregion

    private void ToggleConfigUi() => ConfigWindow.Toggle();

    #region RPC Functions
    private void SetDefaultPresence()
    {
        DiscordService.SetPresence(DefaultPresence);
        DiscordService.UpdatePresenceDetails(LocalizationService.Localize("DalamudRichPresenceInMenus", LocalizationLanguage.Client));
        UpdateStartTime();
    }
    private void UpdateStartTime()
    {
        if (Configuration.ResetTimeWhenChangingZones)
            startTime = DateTime.UtcNow;

        if (Configuration.DisplayDiscordTimestamp)
            DiscordService.UpdatePresenceStartTime(startTime);
    }
    private void UpdatePresence(IFramework framework)
    {
        try
        {
            var timestamp = Configuration.DisplayDiscordTimestamp ? new Timestamps(startTime) : null;
            var context = CollectContext;

            if (ObjectTable.LocalPlayer == null)
            {
                if (!Configuration.ShowLoginQueuePosition || !WaitingwayIPC.IsInLoginQueue())
                {
                    if (inQueue)
                    {
                        inQueue = false;
                        SetDefaultPresence();
                    }
                    return;
                }

                var queuePos = WaitingwayIPC.GetQueuePosition();
                if (queuePos < 0) return; // Not loaded, wait for next update

                var queueEstimate = WaitingwayIPC.GetQueueEstimate();
                inQueue = true;

                // Create presence for login queue
                var queuePresence = PresenceBuilder.BuildQueue(
                    new QueueContext(inQueue, queuePos, queueEstimate),
                    timestamp
                );

                if (queuePresence != null)
                    DiscordService.SetPresence(queuePresence);
                else
                    DiscordService.ClearPresence();
                return;
            }

            var presence = PresenceBuilder.Build(
                context.GetPlayerStatus(),
                context.GetPartyStatus(),
                CollectContext.OnlineStatus,
                timestamp,
                Configuration
            );

            if (presence == null)
                DiscordService.ClearPresence();
            else
                DiscordService.SetPresence(presence);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error updating presence");
        }
    }

    #endregion
    public void Dispose()
    {
        PluginInterface.LanguageChanged -= ReregisterCommand;
        UnregisterCommand();

        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
        ClientState.TerritoryChanged -= OnZoneChange;

        Framework.Update -= UpdatePresence;

        PluginInterface.UiBuilder.Draw -= windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;

        windowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();

        LocalizationService.Dispose();
        LuminaService.Dispose();
        DiscordService.Dispose();
    }
}

