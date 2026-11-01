using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace IdlePlus;

public sealed class Plugin : IDalamudPlugin
{
    private static readonly string[] MainCommands = { "/idle", "/idle+", "/idleplus" };
    private static readonly string[] SettingsCommands = { "/idleplussettings" };

    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commands;
    private readonly Configuration config;
    private readonly WindowSystem windowSystem = new("IdlePlus");
    private readonly PoseWindow mainWindow;
    private readonly SettingsWindow settingsWindow;
    private readonly PoseChanger changer;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commands,
        IFramework framework,
        IObjectTable objects)
    {
        this.pluginInterface = pluginInterface;
        this.commands = commands;

        config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        changer = new PoseChanger(framework, objects);
        settingsWindow = new SettingsWindow(config, SaveConfig, OpenMain);
        mainWindow = new PoseWindow(config, changer, OpenSettings) { IsOpen = config.WindowOpen };
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(settingsWindow);

        foreach (var name in MainCommands)
            commands.AddHandler(name, new CommandInfo(OnCommand)
            {
                HelpMessage = "Toggle the Idle+ window."
            });

        foreach (var name in SettingsCommands)
            commands.AddHandler(name, new CommandInfo(OnSettingsCommand)
            {
                HelpMessage = "Open the Idle+ settings."
            });

        pluginInterface.UiBuilder.Draw += Draw;
        pluginInterface.UiBuilder.OpenMainUi += OpenMain;       // installer "Open"
        pluginInterface.UiBuilder.OpenConfigUi += OpenSettings; // installer "Settings"
    }

    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= Draw;
        pluginInterface.UiBuilder.OpenMainUi -= OpenMain;
        pluginInterface.UiBuilder.OpenConfigUi -= OpenSettings;
        foreach (var name in MainCommands) commands.RemoveHandler(name);
        foreach (var name in SettingsCommands) commands.RemoveHandler(name);
        windowSystem.RemoveAllWindows();
    }

    private void Draw()
    {
        windowSystem.Draw();

        // Remember whether the main window was open (covers closing it with the X button).
        if (config.WindowOpen != mainWindow.IsOpen)
        {
            config.WindowOpen = mainWindow.IsOpen;
            SaveConfig();
        }
    }

    private void OpenMain() => mainWindow.IsOpen = true;

    private void OpenSettings() => settingsWindow.IsOpen = true;

    private void OnCommand(string command, string args) => mainWindow.IsOpen = !mainWindow.IsOpen;

    private void OnSettingsCommand(string command, string args) => OpenSettings();

    private void SaveConfig() => pluginInterface.SavePluginConfig(config);
}
