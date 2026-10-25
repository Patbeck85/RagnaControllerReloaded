using System;
using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-TECH: Shared Stick Drift Utilities.
    /// 
    /// Zentrale Utilities für Stick-Drift-Erkennung und -Kompensation.
    /// Eliminiert Code-Duplikate zwischen HybridEngine, EngineOrchestrator und SettingsWindow.
    /// </summary>
    public static class StickDriftUtils
    {
        /// <summary>
        /// Berechnet den maximalen Stick-Drift aus den aktuellen Stick-Positionen.
        /// </summary>
        public static float CalculateMaxDrift(float leftX, float leftY, float rightX, float rightY)
        {
            return Math.Max(
                Math.Max(Math.Abs(leftX), Math.Abs(leftY)),
                Math.Max(Math.Abs(rightX), Math.Abs(rightY)));
        }

        /// <summary>
        /// Prüft ob ein Stick-Drift-Schwellenwert überschritten ist.
        /// </summary>
        public static bool IsDriftExceeded(float leftX, float leftY, float rightX, float rightY, float threshold)
        {
            return CalculateMaxDrift(leftX, leftY, rightX, rightY) > threshold;
        }

        /// <summary>
        /// Aktualisiert den maximalen Drift-Wert für Kalibrierung.
        /// </summary>
        public static float UpdateMaxDrift(float currentMaxDrift, float leftX, float leftY, float rightX, float rightY)
        {
            var currentDrift = CalculateMaxDrift(leftX, leftY, rightX, rightY);
            return Math.Max(currentMaxDrift, currentDrift);
        }

        /// <summary>
        /// Berechnet eine Drift-Kompensation basierend auf Kalibrierungswerten.
        /// </summary>
        public static (float X, float Y) CompensateDrift(
            float rawX, float rawY, 
            float driftOffsetX, float driftOffsetY)
        {
            return (rawX - driftOffsetX, rawY - driftOffsetY);
        }

        /// <summary>
        /// Wendet Deadzone auf einen Stick-Wert an.
        /// </summary>
        public static float ApplyDeadzone(float value, float deadzone)
        {
            if (Math.Abs(value) < deadzone)
                return 0f;
            
            var sign = Math.Sign(value);
            return sign * (Math.Abs(value) - deadzone) / (1f - deadzone);
        }
    }
}