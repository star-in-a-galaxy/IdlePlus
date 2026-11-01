using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace IdlePlus;

public sealed class PoseWindow : Window
{
    private readonly Configuration config;
    private readonly PoseChanger changer;

    public PoseWindow(Configuration config, PoseChanger changer, Action openSettings)
        : base("Idle+##IdlePlusMain")
    {
        this.config = config;
        this.changer = changer;

        Size = new Vector2(260, 140);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(170, 80),
        };

        // Cog button in the title bar, next to the close button.
        TitleBarButtons.Add(new TitleBarButton
        {
            Icon = FontAwesomeIcon.Cog,
            Click = _ => openSettings(),
            ShowTooltip = () => ImGui.SetTooltip("Open settings"),
        });
    }

    public override unsafe void Draw()
    {
        var state = PlayerState.Instance();
        if (state == null || !state->IsLoaded)
        {
            ImGui.TextDisabled("Not logged in.");
            return;
        }

        var rows = new List<(string Label, EmoteController.PoseType Type)>();
        if (config.ShowIdle) rows.Add(("Idle", EmoteController.PoseType.Idle));
        if (config.ShowSit) rows.Add(("Sit", EmoteController.PoseType.Sit));
        if (config.ShowGroundsit) rows.Add(("Groundsit", EmoteController.PoseType.GroundSit));
        if (config.ShowDoze) rows.Add(("Doze", EmoteController.PoseType.Doze));

        if (rows.Count == 0)
        {
            ImGui.TextDisabled("No poses enabled (see settings).");
            return;
        }

        if (config.AllowPoseChanging && config.UseButtons)
            DrawButtons(state, rows);
        else
            DrawTable(state, rows);
    }

    // Label on its own line, then one numbered button per pose (wraps onto more lines if the window is narrow).
    private unsafe void DrawButtons(PlayerState* state, List<(string Label, EmoteController.PoseType Type)> rows)
    {
        var offset = config.ZeroBased ? 0 : 1;
        var buttonSize = new Vector2(30f * ImGuiHelpers.GlobalScale, 0f);
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        foreach (var (label, type) in rows)
        {
            int current = state->CurrentPose(type);
            var count = EmoteController.GetAvailablePoses(type) + 1;

            ImGui.TextUnformatted(label);

            var available = ImGui.GetContentRegionAvail().X;
            var used = 0f;
            for (var i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    if (used + spacing + buttonSize.X <= available)
                    {
                        ImGui.SameLine();
                        used += spacing;
                    }
                    else
                    {
                        used = 0f;
                    }
                }

                var isCurrent = i == current;
                if (isCurrent)
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.ButtonActive));

                if (ImGui.Button($"{i + offset}##{label}{i}", buttonSize) && !isCurrent)
                    changer.SetPose(type, (byte)i, config.ApplyPoseImmediately);

                if (isCurrent)
                    ImGui.PopStyleColor();

                used += buttonSize.X;
            }

            ImGui.Spacing();
        }
    }

    // Label on the left, right-aligned number (or dropdown) on the right.
    private unsafe void DrawTable(PlayerState* state, List<(string Label, EmoteController.PoseType Type)> rows)
    {
        if (!ImGui.BeginTable("##poses", 2, ImGuiTableFlags.SizingStretchProp))
            return;

        ImGui.TableSetupColumn("label", ImGuiTableColumnFlags.WidthStretch, 1f);
        ImGui.TableSetupColumn("number", ImGuiTableColumnFlags.WidthStretch, 1f);

        var offset = config.ZeroBased ? 0 : 1;

        foreach (var (label, type) in rows)
        {
            int current = state->CurrentPose(type);
            var text = (current + offset).ToString();

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(label);

            ImGui.TableNextColumn();
            if (config.AllowPoseChanging)
            {
                // Right-aligned dropdown showing the current number.
                var comboWidth = 70f * ImGuiHelpers.GlobalScale;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, ImGui.GetContentRegionAvail().X - comboWidth));
                ImGui.SetNextItemWidth(comboWidth);
                if (ImGui.BeginCombo($"##combo{label}", text))
                {
                    var count = EmoteController.GetAvailablePoses(type) + 1;
                    for (var i = 0; i < count; i++)
                    {
                        if (ImGui.Selectable((i + offset).ToString(), i == current))
                            changer.SetPose(type, (byte)i, config.ApplyPoseImmediately);
                    }
                    ImGui.EndCombo();
                }
            }
            else
            {
                // Right-align the numbers so they line up in a column.
                var width = ImGui.CalcTextSize(text).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0f, ImGui.GetContentRegionAvail().X - width));
                ImGui.TextUnformatted(text);
            }
        }

        ImGui.EndTable();
    }
}
