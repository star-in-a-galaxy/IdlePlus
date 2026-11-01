using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace IdlePlus;

public sealed class SettingsWindow : Window
{
    private readonly Configuration config;
    private readonly Action save;
    private readonly Action openMain;

    public SettingsWindow(Configuration config, Action save, Action openMain)
        : base("Idle+ Settings##IdlePlusSettings")
    {
        this.config = config;
        this.save = save;
        this.openMain = openMain;

        Size = new Vector2(300, 260);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(220, 150),
        };
    }

    public override void Draw()
    {
        if (ImGui.Button("Open Idle+ window"))
            openMain();

        ImGui.Separator();

        var changed = false;

        ImGui.TextUnformatted("Show poses");
        changed |= ImGui.Checkbox("Idle", ref config.ShowIdle);
        changed |= ImGui.Checkbox("Sit", ref config.ShowSit);
        changed |= ImGui.Checkbox("Groundsit", ref config.ShowGroundsit);
        changed |= ImGui.Checkbox("Doze", ref config.ShowDoze);

        ImGui.Separator();
        changed |= ImGui.Checkbox("Start numbering at 0", ref config.ZeroBased);

        ImGui.Separator();
        ImGui.TextUnformatted("Advanced");
        changed |= ImGui.Checkbox("Click a number to change pose", ref config.AllowPoseChanging);
        ImGui.TextWrapped("Set current Idle, Sit, Groundsit and Doze pose at any time");

        changed |= ImGui.Checkbox("Apply pose immediately", ref config.ApplyPoseImmediately);
        ImGui.TextWrapped("Picking a pose for a posture you are not in enters it right away (/sit, /groundsit, /doze).");

        ImGui.TextUnformatted("Style selector");
        if (ImGui.RadioButton("Numbered buttons", config.UseButtons))
        {
            config.UseButtons = true;
            changed = true;
        }
        if (ImGui.RadioButton("Dropdown", !config.UseButtons))
        {
            config.UseButtons = false;
            changed = true;
        }

        if (changed) save();
    }
}
