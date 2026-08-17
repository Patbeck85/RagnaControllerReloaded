using RagnaController.Models;

namespace RagnaController.Core
{
    public sealed class SnapshotReadyMessage
    {
        public ControllerSnapshot Snapshot { get; }
        
        // Use 'in' to pass the struct by readonly reference (avoid copying 64 bytes)
        public SnapshotReadyMessage(in ControllerSnapshot snapshot) 
        {
            Snapshot = snapshot;
        }
    }

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

    public sealed class BatteryChangedMessage
    {
        public string Level { get; }
        public BatteryChangedMessage(string level) => Level = level;
    }

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
    public sealed class BuffWarningMessage
    {
        public string ActionLabel { get; }
        public BuffWarningMessage(string label) => ActionLabel = label;
    }
}
