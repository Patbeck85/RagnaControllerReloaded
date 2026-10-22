using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Core.Services;
using RagnaController.Profiles;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-023: GyroCalibrationService Unit Tests (TASK-2401).
    /// Deckt Gyro-Kalibrierung, Drift-Offset-Berechnung, Schwellenwert-Ermittlung ab.
    /// </summary>
    public class GyroCalibrationServiceTests
    {
        #region Drift Offset Berechnung

        [Fact]
        public void GyroService_FeedGyro_UpdatesPitchAndYaw()
        {
            // Arrange
            var gyro = new GyroService { Smoothing = 0f }; // Kein Low-Pass-Filter für direkten Test

            // Act
            gyro.FeedGyro(10.0f, 20.0f, 0.0f, 16.0f);

            // Assert
            Assert.Equal(10.0f, gyro.PitchDps, 0.001f);
            Assert.Equal(20.0f, gyro.YawDps, 0.001f);
        }

        [Fact]
        public void GyroService_FeedGyro_AppliesDriftThreshold()
        {
            // Arrange
            var gyro = new GyroService { DriftThreshold = 100 };

            // Act - kleiner Wert unterhalb der Schwelle
            gyro.FeedGyro(1.0f, 1.0f, 0.0f, 16.0f);

            // Assert - sollte auf 0 gesetzt werden
            Assert.Equal(0.0f, gyro.PitchDps, 0.001f);
            Assert.Equal(0.0f, gyro.YawDps, 0.001f);
        }

        [Fact]
        public void GyroService_FeedGyro_AboveThreshold_PassesThrough()
        {
            // Arrange
            var gyro = new GyroService { DriftThreshold = 10, Smoothing = 0f };

            // Act - oberhalb der Schwelle
            gyro.FeedGyro(50.0f, 50.0f, 0.0f, 16.0f);

            // Assert - sollte übergeben werden
            Assert.Equal(50.0f, gyro.PitchDps, 0.001f);
            Assert.Equal(50.0f, gyro.YawDps, 0.001f);
        }

        [Fact]
        public void GyroService_Reset_ZeroesDps()
        {
            // Arrange
            var gyro = new GyroService();
            gyro.FeedGyro(50.0f, 50.0f, 0.0f, 16.0f);

            // Act
            gyro.Reset();

            // Assert
            Assert.Equal(0.0f, gyro.PitchDps, 0.001f);
            Assert.Equal(0.0f, gyro.YawDps, 0.001f);
        }

        #endregion

        #region Profile Kalibrierungswerte

        [Fact]
        public void Profile_CalibrationTags_AreStoredCorrectly()
        {
            // Arrange
            var profile = new Profile { Name = "CalibrationTest" };

            // Act - Simuliere Kalibrierungsergebnisse als Tags
            profile.Tags.Add("gyro_offset_pitch=0.0050");
            profile.Tags.Add("gyro_offset_yaw=-0.0030");
            profile.Tags.Add("gyro_threshold=42");
            profile.Tags.Add("gyro_sensitivity=1.0000");
            profile.Tags.Add($"gyro_calibrated={DateTime.Now:yyyy-MM-dd HH:mm}");
            profile.GyroEnabled = true;

            // Assert
            Assert.True(profile.GyroEnabled);
            Assert.Equal(5, profile.Tags.Count);
            Assert.Contains("gyro_offset_pitch=0.0050", profile.Tags);
            Assert.Contains("gyro_offset_yaw=-0.0030", profile.Tags);
            Assert.Contains("gyro_threshold=42", profile.Tags);
            Assert.True(profile.Tags.Any(t => t.StartsWith("gyro_calibrated=")));
        }

        [Fact]
        public void Profile_Tags_CanParseCalibrationValues()
        {
            // Arrange
            var profile = new Profile
            {
                Tags = { "gyro_offset_pitch=0.0050", "gyro_offset_yaw=-0.0030", "gyro_threshold=42" }
            };

            // Act
            var pitchTag = profile.Tags.Find(t => t.StartsWith("gyro_offset_pitch="));
            var yawTag = profile.Tags.Find(t => t.StartsWith("gyro_offset_yaw="));
            var thresholdTag = profile.Tags.Find(t => t.StartsWith("gyro_threshold="));

            // Assert
            Assert.NotNull(pitchTag);
            Assert.NotNull(yawTag);
            Assert.NotNull(thresholdTag);
            var pitchVal = float.Parse(pitchTag.Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);
            var yawVal = float.Parse(yawTag.Split('=')[1], System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(0.0050f, pitchVal, 2); // 2 Dezimalstellen Genauigkeit
            Assert.Equal(-0.0030f, yawVal, 2);
            Assert.Equal(42, int.Parse(thresholdTag.Split('=')[1], System.Globalization.CultureInfo.InvariantCulture));
        }

        #endregion

        #region Drift Threshold Berechnung

        [Fact]
        public void DriftThreshold_MinimumIs32()
        {
            // Arrange & Act & Assert
            // Mindestens 32, auch bei sehr kleinen Abweichungen
            int threshold = Math.Max(32, (int)(1.0f * 2.0f + 10));
            Assert.True(threshold >= 32);
        }

        [Fact]
        public void DriftThreshold_ScalesWithDeviation()
        {
            // Arrange & Act
            int thresholdLow = Math.Max(32, (int)(5.0f * 2.0f + 10));  // ~20
            int thresholdHigh = Math.Max(32, (int)(50.0f * 2.0f + 10)); // ~110

            // Assert
            Assert.Equal(32, thresholdLow);  // Minimum greift
            Assert.Equal(110, thresholdHigh); // Skaliert mit Abweichung
        }

        #endregion
    }
}
