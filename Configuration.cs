using System;
using Dalamud.Configuration;

namespace IdlePlus;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public bool WindowOpen = true;

    public bool ShowIdle = true;
    public bool ShowSit = true;
    public bool ShowGroundsit = true;
    public bool ShowDoze = true;

    // The game stores poses 0-based. Off = show 1, 2, 3...  On = show 0, 1, 2...
    public bool ZeroBased = true;

    // Advanced: lets you pick a pose from a dropdown (works by sending /cpose repeatedly).
    public bool AllowPoseChanging = true;

    // When picking a pose for a posture you are not in, also enter that posture
    // right away (/groundsit, /sit, /doze) instead of only saving the pose.
    public bool ApplyPoseImmediately = false;

    // How the pose picker looks when changing is enabled: true = numbered buttons, false = dropdown.
    public bool UseButtons = true;
}
