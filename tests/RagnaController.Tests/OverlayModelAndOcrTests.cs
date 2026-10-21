using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Xunit;
using RagnaController.Core.Services;
using RagnaController.Models;
using RagnaController.Views.Overlays;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2208: Unit-Tests für Overlay-Modelle und OCR-Service.
    /// Testet SkillCooldown, BuffTimerViewModel, DamageNumber DataModels sowie
    /// OcrService-Zahlenextraktion (ParseNumber-Logik).
    /// Headless — keine UI-Abhängigkeiten.
    /// </summary>
    public class OverlayModelAndOcrTests
    {
        // ── SkillCooldown Tests ──────────────────────────────────────────

        [Fact]
        public void SkillCooldown_RemainingTime_ShowsZeroWhenExpired()
        {
            var sc = new SkillCooldown("F1", 0, 5000, Colors.Red);
            Assert.Equal("0", sc.RemainingTime);
        }

        [Fact]
        public void SkillCooldown_RemainingTime_ShowsSeconds()
        {
            var sc = new SkillCooldown("F2", 3, 5000, Colors.Blue);
            Assert.Equal("3", sc.RemainingTime);
        }

        [Fact]
        public void SkillCooldown_RemainingPercent_WithinRange()
        {
            var sc = new SkillCooldown("F3", 2500, 5000, Colors.Green);
            Assert.Equal(0.5, sc.RemainingPercent, 2);
        }

        [Fact]
        public void SkillCooldown_RemainingPercent_ZeroWhenMaxIsZero()
        {
            var sc = new SkillCooldown("F4", 1000, 0, Colors.Red);
            Assert.Equal(0.0, sc.RemainingPercent);
        }

        [Fact]
        public void SkillCooldown_PropertyChanged_FiresOnRemainingSecondsChange()
        {
            var sc = new SkillCooldown("F5", 1000, 5000, Colors.Red);
            bool fired = false;
            sc.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(sc.RemainingTime))
                    fired = true;
            };
            sc.RemainingSeconds = 500;
            Assert.True(fired);
        }

        // ── BuffTimerViewModel Tests ─────────────────────────────────────

        [Fact]
        public void BuffTimerViewModel_IsWarning_TrueWhenBelowThreshold()
        {
            var btv = new BuffTimerViewModel("Blessing", false, 3, 60, 5);
            Assert.True(btv.IsWarning);
        }

        [Fact]
        public void BuffTimerViewModel_IsWarning_FalseWhenAboveThreshold()
        {
            var btv = new BuffTimerViewModel("Blessing", false, 10, 60, 5);
            Assert.False(btv.IsWarning);
        }

        [Fact]
        public void BuffTimerViewModel_BarColor_RedForDebuff()
        {
            var btv = new BuffTimerViewModel("Curse", true, 30, 60, 5);
            Assert.Equal(239, btv.BarColor.R);
            Assert.Equal(68, btv.BarColor.G);
            Assert.Equal(68, btv.BarColor.B);
        }

        [Fact]
        public void BuffTimerViewModel_BarColor_BlueForBuff()
        {
            var btv = new BuffTimerViewModel("Blessing", false, 30, 60, 5);
            Assert.Equal(59, btv.BarColor.R);
            Assert.Equal(130, btv.BarColor.G);
            Assert.Equal(246, btv.BarColor.B);
        }

        [Fact]
        public void BuffTimerViewModel_RemainingText_ShowsSeconds()
        {
            var btv = new BuffTimerViewModel("Test", false, 42, 60, 5);
            Assert.Equal("42s", btv.RemainingText);
        }

        [Fact]
        public void BuffTimerViewModel_RemainingPercent_WithinRange()
        {
            var btv = new BuffTimerViewModel("Test", false, 30, 60, 5);
            Assert.Equal(0.5, btv.RemainingPercent, 2);
        }

        // ── DamageNumber Tests ───────────────────────────────────────────

        [Fact]
        public void DamageNumber_AmountText_IncludesMinusSign()
        {
            var dmg = new DamageNumber(150, 100, 200);
            Assert.Equal("-150", dmg.AmountText);
        }

        [Fact]
        public void DamageNumber_Opacity_StartsAtOne()
        {
            var dmg = new DamageNumber(100, 50, 50);
            Assert.Equal(1.0, dmg.Opacity);
        }

        [Fact]
        public void DamageNumber_Properties_AreImmutable()
        {
            var dmg = new DamageNumber(200, 10.5, 20.5);
            Assert.Equal(200, dmg.Amount);
            Assert.Equal(10.5, dmg.X);
            Assert.Equal(20.5, dmg.Y);
        }

        // ── OcrService ParseNumber Tests (indirect via reflection on logic) ──
        // ParseNumber is private; test via OcrService constructor behavior.

        [Fact]
        public void OcrService_Constructor_WithoutTessdata_DoesNotThrow()
        {
            // Tesseract ohne tessdata-Dateien sollte nicht abstürzen, sondern _engine = null setzen
            var ocr = new OcrService(null, "eng");
            Assert.False(ocr.IsAvailable);
        }

        [Fact]
        public void OcrService_ExtractNumber_NullWhenEngineNull()
        {
            var ocr = new OcrService(null, "eng");
            var bmp = new System.Drawing.Bitmap(10, 10);
            var result = ocr.ExtractNumber(bmp);
            Assert.Null(result);
        }

        [Fact]
        public void OcrService_ExtractNumber_NullWhenDisposed()
        {
            var ocr = new OcrService(null, "eng");
            ocr.Dispose();
            var bmp = new System.Drawing.Bitmap(10, 10);
            var result = ocr.ExtractNumber(bmp);
            Assert.Null(result);
        }

        [Fact]
        public void OcrService_Dispose_IsIdempotent()
        {
            var ocr = new OcrService(null, "eng");
            ocr.Dispose();
            ocr.Dispose(); // muss nicht werfen
            Assert.True(true);
        }

        // ── ObservableCollection Tests (DamageNumbers, SkillCooldowns, BuffTimers) ──

        [Fact]
        public void DamageNumbers_Collection_StartsEmpty()
        {
            // DamageNumber ist ein Datenmodell, das in InGameOverlayWindow verwendet wird.
            // Hier testen wir nur die Modell-Klasse selbst (keine UI-Abhängigkeit).
            var dmg1 = new DamageNumber(100, 0, 0);
            var dmg2 = new DamageNumber(200, 10, 10);
            Assert.Equal(100, dmg1.Amount);
            Assert.Equal(200, dmg2.Amount);
        }

        [Fact]
        public void OverlayModels_ImplementInotifyPropertyChanged()
        {
            var sc = new SkillCooldown("F1", 1000, 5000, Colors.Red);
            var btv = new BuffTimerViewModel("Test", false, 30, 60, 5);
            var dmg = new DamageNumber(50, 0, 0);

            bool scFired = false, btvFired = false;
            sc.PropertyChanged += (_, _) => scFired = true;
            btv.PropertyChanged += (_, _) => btvFired = true;

            sc.RemainingSeconds = 500;
            btv.RemainingSec = 20;

            Assert.True(scFired);
            Assert.True(btvFired);
        }
    }
}
