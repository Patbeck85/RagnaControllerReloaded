using System;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Controller
{
    public sealed class ControllerSnapshot
    {
        // Layer text (COMBO/MAGE/AUTO/GRID MODE)
        public string LayerText { get; set; } = "";
        
        // State label and foreground color
        public string StateLabel { get; set; } = "";
        public SolidColorBrush StateForeground { get; set; } = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        
        // Smart Grid mode indicator (bool for UI binding)
        public bool SmartCursorMenuMode { get; set; }
        
        // Delta time in milliseconds
        public int DeltaMs { get; set; }
        public int TickMs { get; set; }
        
        // Window tracking
        public bool WindowTracked { get; set; }
        public float WindowDpiScale { get; set; }
        
        // Panic/Vacuum/Combo states (for lightbar)
        public bool PanicActive { get; set; }
        public bool VacuumActive { get; set; }
        public bool ComboActive { get; set; }
        // Combat state (string label)
        public string CombatState { get; set; } = "";
        
        // Battery throttle state
        public bool BatteryThrottle { get; set; }
        
        // Focus state
        public bool FocusLocked { get; set; }
        
        // Analog stick positions (-1.0 to 1.0)
        public float LeftX { get; set; }
        public float LeftY { get; set; }
        public float RightX { get; set; }
        public float RightY { get; set; }
        
        // Analog triggers (0.0 to 1.0)
        public float LT { get; set; }
        public float RT { get; set; }
        
        // Face buttons
        public bool L1 { get; set; }
        public bool R1 { get; set; }
        public bool L2 { get; set; }
        public bool R2 { get; set; }
        public bool BtnA { get; set; }
        public bool BtnB { get; set; }
        public bool BtnX { get; set; }
        public bool BtnY { get; set; }
        
        // D-Pad buttons
        public bool DPadUp { get; set; }
        public bool DPadDown { get; set; }
        public bool DPadLeft { get; set; }
        public bool DPadRight { get; set; }
        
        // System buttons
        public bool Start { get; set; }
        public bool Back { get; set; }
        public bool BtnL3 { get; set; }
        public bool BtnR3 { get; set; }
        public bool L3 { get; set; }
        
        // Combo engine state
        public string ActionLabel { get; set; } = "";
        public int ActionId { get; set; }
        
        // AutoTarget state
        public string TargetName { get; set; } = "";
        public string TargetType { get; set; } = "";
        public float TargetDistance { get; set; }
        
        // Mage engine state
        public string Buffs { get; set; } = "";
        public string Cooldowns { get; set; } = "";
        
        // Combo engine overlay text
        public string MobSweepLabel { get; set; } = "";
        public string HandheldModeLabel { get; set; } = "";
        public bool HandheldModeActive { get; set; }
        public string OverlayText { get; set; } = "";
        public string MiniModeLabel { get; set; } = "";
    }
}
