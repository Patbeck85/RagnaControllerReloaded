using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Core.Hardware;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2408: Unit-Tests für Epic 15 (Controller Hardware Advanced Features).
    /// Deckt StickDriftCompensation, TriggerDeadzoneCurve und MotionAimAssist ab.
    /// </summary>
    public class Epic15HardwareTests
    {
        #region StickDriftCompensation Tests

        [Fact]
        public void StickDriftCompensation_Calibrate_ShouldDetectOffsets()
        {
            // Arrange
            var comp = new StickDriftCompensation();
            float[] leftX = { 0.05f, 0.06f, 0.04f, 0.05f };
            float[] leftY = { -0.02f, -0.01f, -0.03f, -0.02f };
            float[] rightX = { 0.01f, 0.01f, 0.01f, 0.01f };
            float[] rightY = { 0f, 0f, 0f, 0f };

            // Act
            comp.Calibrate(leftX, leftY, rightX, rightY);

            // Assert
            Assert.True(comp.IsCalibrated);
            Assert.Equal(0.05f, comp.LeftStickDriftX, 2);
            Assert.Equal(-0.02f, comp.LeftStickDriftY, 2);
            Assert.Equal(0.01f, comp.RightStickDriftX, 2);
            Assert.Equal(0f, comp.RightStickDriftY, 2);
        }

        [Fact]
        public void StickDriftCompensation_Compensate_ShouldSubtractOffset()
        {
            // Arrange
            var comp = new StickDriftCompensation();
            comp.SetOffsets(0.1f, 0.05f, -0.05f, 0f);

            // Act
            float resultX = StickDriftCompensation.Compensate(0.3f, 0.1f);
            float resultY = StickDriftCompensation.Compensate(0.1f, 0.05f);

            // Assert (mit Toleranz für Float-Präzision)
            Assert.Equal(0.2f, resultX, 2);
            Assert.Equal(0.05f, resultY, 2);
        }

        [Fact]
        public void StickDriftCompensation_Compensate_ShouldClampToRange()
        {
            // Arrange
            var comp = new StickDriftCompensation();
            comp.SetOffsets(-0.5f, 0f, 0f, 0f);

            // Act - großer negativer Wert würde unter -1.0 fallen
            float result = StickDriftCompensation.Compensate(-1.0f, -0.5f);

            // Assert
            Assert.InRange(result, -1.0f, 1.0f);
        }

        [Fact]
        public void StickDriftCompensation_Reset_ShouldClearState()
        {
            // Arrange
            var comp = new StickDriftCompensation();
            comp.SetOffsets(0.1f, 0.1f, 0.1f, 0.1f);

            // Act
            comp.Reset();

            // Assert
            Assert.False(comp.IsCalibrated);
            Assert.Equal(0f, comp.LeftStickDriftX);
            Assert.Equal(0f, comp.LeftStickDriftY);
            Assert.Equal(0f, comp.RightStickDriftX);
            Assert.Equal(0f, comp.RightStickDriftY);
        }

        #endregion

        #region TriggerDeadzoneCurve Tests

        [Fact]
        public void TriggerDeadzoneCurve_Linear_ShouldReturnNormalizedValue()
        {
            // Arrange
            var curve = new TriggerDeadzoneCurve
            {
                Type = TriggerDeadzoneCurve.CurveType.Linear,
                Deadzone = 0.1f,
                Saturation = 1.0f,
            };

            // Act
            float result = curve.Apply(0.5f);

            // Assert: (0.5 - 0.1) / (1.0 - 0.1) = 0.4 / 0.9 = 0.444...
            Assert.InRange(result, 0.44f, 0.45f);
        }

        [Fact]
        public void TriggerDeadzoneCurve_Deadzone_ShouldReturnZeroBelowThreshold()
        {
            // Arrange
            var curve = new TriggerDeadzoneCurve
            {
                Type = TriggerDeadzoneCurve.CurveType.Linear,
                Deadzone = 0.2f,
                Saturation = 1.0f,
            };

            // Act
            float result = curve.Apply(0.1f); // unter Deadzone

            // Assert
            Assert.Equal(0f, result);
        }

        [Fact]
        public void TriggerDeadzoneCurve_Saturation_ShouldReturnOneAboveThreshold()
        {
            // Arrange
            var curve = new TriggerDeadzoneCurve
            {
                Type = TriggerDeadzoneCurve.CurveType.Linear,
                Deadzone = 0.0f,
                Saturation = 0.5f,
            };

            // Act
            float result = curve.Apply(1.0f); // über Saturation

            // Assert
            Assert.Equal(1f, result);
        }

        [Fact]
        public void TriggerDeadzoneCurve_Quadratic_ShouldReturnSquaredValue()
        {
            // Arrange
            var curve = new TriggerDeadzoneCurve
            {
                Type = TriggerDeadzoneCurve.CurveType.Quadratic,
                Deadzone = 0.0f,
                Saturation = 1.0f,
            };

            // Act
            float result = curve.Apply(0.5f);

            // Assert: 0.5^2 = 0.25
            Assert.Equal(0.25f, result, 2);
        }

        [Fact]
        public void TriggerDeadzoneCurve_Exponential_ShouldReturnCubedValue()
        {
            // Arrange
            var curve = new TriggerDeadzoneCurve
            {
                Type = TriggerDeadzoneCurve.CurveType.Exponential,
                Deadzone = 0.0f,
                Saturation = 1.0f,
            };

            // Act
            float result = curve.Apply(0.5f);

            // Assert: 0.5^3 = 0.125
            Assert.Equal(0.125f, result, 2);
        }

        [Fact]
        public void TriggerDeadzoneCurve_Inverted_ShouldInvertValue()
        {
            // Arrange
            var curve = new TriggerDeadzoneCurve
            {
                Type = TriggerDeadzoneCurve.CurveType.Linear,
                Deadzone = 0.0f,
                Saturation = 1.0f,
                Inverted = true,
            };

            // Act
            float result = curve.Apply(0.2f);

            // Assert: (1.0 - 0.2) = 0.8
            Assert.Equal(0.8f, result, 2);
        }

        [Fact]
        public void TriggerDeadzoneCurve_CreateBowCurve_ShouldReturnQuadratic()
        {
            // Act
            var curve = TriggerDeadzoneCurve.CreateBowCurve();

            // Assert
            Assert.Equal(TriggerDeadzoneCurve.CurveType.Quadratic, curve.Type);
            Assert.Equal(0.05f, curve.Deadzone);
            Assert.Equal(0.95f, curve.Saturation);
        }

        [Fact]
        public void TriggerDeadzoneCurve_CreateSpellChargeCurve_ShouldReturnExponential()
        {
            // Act
            var curve = TriggerDeadzoneCurve.CreateSpellChargeCurve();

            // Assert
            Assert.Equal(TriggerDeadzoneCurve.CurveType.Exponential, curve.Type);
            Assert.Equal(0.08f, curve.Deadzone);
            Assert.Equal(0.90f, curve.Saturation);
        }

        #endregion

        #region MotionAimAssist Tests

        [Fact]
        public void MotionAimAssist_Update_ShouldCalculateOutput()
        {
            // Arrange
            var aim = new MotionAimAssist { Sensitivity = 1.0f, Deadzone = 0f, MaxOutput = 100f, Smoothing = 1f };

            // Act
            aim.Update(10f, 20f, 0f, 0.016f); // 10 Grad/s Pitch, 20 Grad/s Yaw, 16ms

            // Assert: 20 * 0.016 = 0.32 (Yaw -> OutputX), 10 * 0.016 = 0.16 (Pitch -> OutputY)
            Assert.InRange(aim.OutputX, 0.3f, 0.35f);
            Assert.InRange(aim.OutputY, 0.15f, 0.17f);
        }

        [Fact]
        public void MotionAimAssist_Deadzone_ShouldFilterSmallMovements()
        {
            // Arrange
            var aim = new MotionAimAssist { Sensitivity = 1.0f, Deadzone = 5.0f, MaxOutput = 100f, Smoothing = 1f };

            // Act - kleine Bewegung unter Deadzone
            aim.Update(1f, 1f, 0f, 0.016f);

            // Assert
            Assert.Equal(0f, aim.OutputX);
            Assert.Equal(0f, aim.OutputY);
        }

        [Fact]
        public void MotionAimAssist_Disabled_ShouldReturnZero()
        {
            // Arrange
            var aim = new MotionAimAssist { IsEnabled = false, Sensitivity = 1.0f, Deadzone = 0f };

            // Act
            aim.Update(100f, 100f, 0f, 0.016f);

            // Assert
            Assert.Equal(0f, aim.OutputX);
            Assert.Equal(0f, aim.OutputY);
        }

        [Fact]
        public void MotionAimAssist_Reset_ShouldClearOutput()
        {
            // Arrange
            var aim = new MotionAimAssist { Sensitivity = 1.0f, Deadzone = 0f, MaxOutput = 100f, Smoothing = 1f };
            aim.Update(10f, 20f, 0f, 0.016f);

            // Act
            aim.Reset();

            // Assert
            Assert.Equal(0f, aim.OutputX);
            Assert.Equal(0f, aim.OutputY);
        }

        #endregion
    }
}
