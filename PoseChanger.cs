using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace IdlePlus;

/// <summary>
/// Sets the saved pose for any pose type by writing it into the game's client-side memory.
/// If you are in that posture right now, the plugin also makes the game apply it immediately:
/// it sets the saved pose to the one BEFORE your target and then sends a single /cpose, so the game
/// itself advances to the target, plays the animation and tells the server.
/// </summary>
public sealed unsafe class PoseChanger
{
    private readonly IFramework framework;
    private readonly IObjectTable objects;

    public PoseChanger(IFramework framework, IObjectTable objects)
    {
        this.framework = framework;
        this.objects = objects;
    }

    /// <summary>Which pose type matches your current posture (standing, ground sit, chair, bed).</summary>
    public EmoteController.PoseType? GetActivePoseType()
    {
        var player = objects.LocalPlayer;
        if (player == null) return null;
        var chara = (Character*)player.Address;

        // ModeParam encodes the posture: 1 = ground sit, 2 = chair, 3 = doze, anything else = standing.
        return chara->ModeParam switch
        {
            1 => EmoteController.PoseType.GroundSit,
            2 => EmoteController.PoseType.Sit,
            3 => EmoteController.PoseType.Doze,
            _ => EmoteController.PoseType.Idle,
        };
    }

    public void SetPose(EmoteController.PoseType type, byte index, bool applyImmediately = false)
    {
        // Game memory and game commands must be touched on the framework thread, not the UI thread.
        _ = framework.RunOnFrameworkThread(() => SetPoseNow(type, index, applyImmediately));
    }

    private void SetPoseNow(EmoteController.PoseType type, byte index, bool applyImmediately)
    {
        var player = objects.LocalPlayer;
        var state = PlayerState.Instance();
        if (player == null || state == null || !state->IsLoaded) return;

        var count = EmoteController.GetAvailablePoses(type) + 1;
        if (index >= count) return;

        var current = state->SelectedPoses[(int)type];

        if (GetActivePoseType() == type)
        {
            // Already in that posture: step the pose so the game plays the animation.
            if (current == index) return;

            if (count <= 1)
            {
                state->SelectedPoses[(int)type] = index;
                return;
            }

            // Step back one pose, then let the game's own /cpose step forward to the target.
            var previous = (byte)((index + count - 1) % count);
            var chara = (Character*)player.Address;
            state->SelectedPoses[(int)type] = previous;
            chara->EmoteController.CPoseState = previous;
            SendCommand("/cpose");

            // Safety net: if the command didn't take effect, make sure the saved pose is still the wanted one.
            _ = framework.RunOnTick(() =>
            {
                var s = PlayerState.Instance();
                if (s != null && s->IsLoaded && s->SelectedPoses[(int)type] != index)
                    s->SelectedPoses[(int)type] = index;
            }, TimeSpan.FromMilliseconds(750));
            return;
        }

        // Not in that posture: save it, and optionally enter the posture right away.
        state->SelectedPoses[(int)type] = index;

        if (!applyImmediately) return;

        // Idle is the standing state rather than an emote, so to reach it we re-send the command of
        // the posture we are currently in. The game ends an emote when its own command is repeated
        // (EndOnEmote), which returns us to standing on the freshly saved Idle pose.
        var posture = type == EmoteController.PoseType.Idle ? GetActivePoseType() : type;
        if (posture != null && TryGetPostureCommand(posture.Value, out var command))
            SendCommand(command);
    }

    /// <summary>The command that enters a posture, and ends it again when repeated.</summary>
    private static bool TryGetPostureCommand(EmoteController.PoseType type, out string command)
    {
        switch (type)
        {
            case EmoteController.PoseType.Sit:
                command = "/sit"; // alias of /lounge; sits in a nearby chair
                return true;
            case EmoteController.PoseType.GroundSit:
                command = "/groundsit";
                return true;
            case EmoteController.PoseType.Doze:
                command = "/doze";
                return true;
            default:
                command = string.Empty;
                return false;
        }
    }

    private static void SendCommand(string command)
    {
        var module = UIModule.Instance();
        if (module == null) return;

        var text = Utf8String.FromString(command);
        module->ProcessChatBoxEntry(text);
        text->Dtor(true);
    }
}
