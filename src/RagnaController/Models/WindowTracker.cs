using System;

namespace RagnaController.Models
{
    public sealed class WindowTracker
    {
        public string ControllerName { get; set; } = "";
        public string ControllerType { get; set; } = "XBOX";
        public float DpiScale { get; set; } = 1f;
        public bool WindowTracked { get; set; }
        public int TickMs { get; set; }
    }
}
