using RagnaController.Models;
using RagnaController.Controller;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class SnapshotReadyMessage
    {
        public ControllerSnapshot Snapshot { get; }
        
        // Use 'in' to pass the struct by readonly reference (avoid copying 64 bytes)
        public SnapshotReadyMessage(in ControllerSnapshot snapshot) 
        {
            Snapshot = snapshot;
        }
    }

    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class EngineStatusMessage
    {
        public EngineStatus Status         { get; }
        public string       ControllerName { get; }
        public EngineStatusMessage(EngineStatus status, string controllerName)
        {
            Status         = status;
            ControllerName = controllerName;
        }
    }

    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class BatteryChangedMessage
    {
        public string Level { get; }
        public BatteryChangedMessage(string level) => Level = level;
    }

    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class ActionFiredMessage
    {
        public string          Label { get; }
        public ActionFiredKind Kind  { get; }
        public ActionFiredMessage(string label, ActionFiredKind kind)
        {
            Label = label;
            Kind  = kind;
        }
    }

    // NEW: Buff Warning Message
    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class BuffWarningMessage
    {
        public string ActionLabel { get; }
        public BuffWarningMessage(string label) => ActionLabel = label;
    }

    // NEW: Profile Switched Message
    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class ProfileSwitchedMessage
    {
        public string ProfileName { get; }
        public ProfileSwitchedMessage(string profileName) => ProfileName = profileName;
    }

    // FEAT-011: Item Manager — ein verwaltetes Item (Potion/Trank) wurde eingesetzt
    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class ItemFiredMessage
    {
        public string ItemName { get; }
        public ItemFiredMessage(string itemName) => ItemName = itemName;
    }
}
