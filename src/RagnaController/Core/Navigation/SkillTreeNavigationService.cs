using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core.Navigation {
    /// <summary>
    /// TASK-2503: SkillTreeNavigationService — Skill-Punkte per Controller verteilen (Grid-Navigation).
    ///
    /// Ermöglicht die Navigation im RO-Skill-Baum über den Controller:
    ///   • Grid-Navigation zwischen Skill-Slots (D-Pad)
    ///   • Skill-Info anzeigen (A-Button)
    ///   • Skill-Level erhöhen (R1/RT) oder verringern (L1/LT)
    ///   • Skill-Punkte verteilen (verfügbare Punkte anzeigen)
    ///   • Voraussetzungen prüfen (Basisskill-Level, Job-Level)
    /// </summary>
    public sealed class SkillTreeNavigationService
    {
        private readonly InputCommandQueue _queue;
        private readonly IFeedbackProvider _feedback;

        // ── Skill Tree State ────────────────────────────────────────────────
        public bool IsSkillTreeOpen { get; private set; }
        public int SelectedSkillIndex { get; set; } = 0;
        public int AvailableSkillPoints { get; set; } = 0;
        public int BaseLevel { get; set; } = 1;
        public int JobLevel { get; set; } = 1;

        // ── Skills ──────────────────────────────────────────────────────────
        public List<SkillNode> Skills { get; } = new();

        // ── Grid-Layout ─────────────────────────────────────────────────────
        public int GridColumns { get; set; } = 5;
        public int GridRows => (int)Math.Ceiling((double)Skills.Count / GridColumns);

        public int SelectedGridX => SelectedSkillIndex % GridColumns;
        public int SelectedGridY => SelectedSkillIndex / GridColumns;

        public SkillTreeNavigationService(InputCommandQueue queue, IFeedbackProvider feedback)
        {
            _queue = queue;
            _feedback = feedback;
        }

        /// <summary>Öffnet den Skill-Baum-Navigationsmodus.</summary>
        public void OpenSkillTree()
        {
            IsSkillTreeOpen = true;
            SelectedSkillIndex = 0;
            _feedback.Trigger(FeedbackType.PrecisionModeOn);
        }

        /// <summary>Schließt den Skill-Baum-Navigationsmodus.</summary>
        public void CloseSkillTree()
        {
            IsSkillTreeOpen = false;
            _feedback.Trigger(FeedbackType.PrecisionModeOff);
        }

        /// <summary>Fügt einen Skill-Knoten hinzu (für Test/Overlay-Befüllung).</summary>
        public void AddSkill(SkillNode skill)
        {
            Skills.Add(skill);
        }

        /// <summary>Gibt den aktuell ausgewählten Skill zurück.</summary>
        public SkillNode? GetSelectedSkill()
        {
            if (Skills.Count == 0 || SelectedSkillIndex < 0 || SelectedSkillIndex >= Skills.Count)
                return null;
            return Skills[SelectedSkillIndex];
        }

        /// <summary>Prüft ob ein Skill erlernbar ist (Voraussetzungen erfüllt).</summary>
        public bool CanLearnSkill(SkillNode skill)
        {
            if (skill.Level >= skill.MaxLevel)
                return false;
            if (AvailableSkillPoints <= 0)
                return false;
            if (BaseLevel < skill.RequiredBaseLevel)
                return false;
            if (JobLevel < skill.RequiredJobLevel)
                return false;
            // Prüfe Basisskill-Voraussetzung
            if (skill.PrerequisiteSkillId != null)
            {
                var prereq = Skills.FirstOrDefault(s => s.Id == skill.PrerequisiteSkillId);
                if (prereq == null || prereq.Level < prereq.MaxLevel)
                    return false;
            }
            return true;
        }

        /// <summary>Erhöht den Level des ausgewählten Skills um 1.</summary>
        public bool LevelUpSelectedSkill()
        {
            var skill = GetSelectedSkill();
            if (skill == null) return false;
            if (!CanLearnSkill(skill)) return false;

            skill.Level++;
            AvailableSkillPoints--;
            _feedback.Trigger(FeedbackType.TargetLocked); // Skill Level Up Feedback
            return true;
        }

        /// <summary>Verringert den Level des ausgewählten Skills um 1 (Skill-Reset).</summary>
        public bool LevelDownSelectedSkill()
        {
            var skill = GetSelectedSkill();
            if (skill == null || skill.Level <= 0) return false;

            skill.Level--;
            AvailableSkillPoints++;
            _feedback.Trigger(FeedbackType.Warning);
            return true;
        }

        /// <summary>Verarbeitet Controller-Eingaben für Skill-Baum-Navigation.</summary>
        public bool ProcessInput(ParsedInput input)
        {
            if (!IsSkillTreeOpen)
                return false;

            // Skill-Baum verlassen (B / Back)
            if (input.BtnB || input.Back)
            {
                CloseSkillTree();
                return true;
            }

            // Grid-Navigation (D-Pad)
            if (input.DPadUp)
            {
                SelectedSkillIndex = Math.Max(0, SelectedSkillIndex - GridColumns);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }
            if (input.DPadDown)
            {
                SelectedSkillIndex = Math.Min(Skills.Count - 1, SelectedSkillIndex + GridColumns);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }
            if (input.DPadLeft)
            {
                SelectedSkillIndex = Math.Max(0, SelectedSkillIndex - 1);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }
            if (input.DPadRight)
            {
                SelectedSkillIndex = Math.Min(Skills.Count - 1, SelectedSkillIndex + 1);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            // Skill-Info anzeigen (A-Button)
            if (input.BtnA)
            {
                var skill = GetSelectedSkill();
                if (skill != null)
                {
                    _feedback.Trigger(FeedbackType.TargetLocked);
                }
                return true;
            }

            // Skill-Level erhöhen (R1 oder RT > 0.5)
            if (input.R1 || input.RT > 0.5f)
            {
                LevelUpSelectedSkill();
                return true;
            }

            // Skill-Level verringern (L1 oder LT > 0.5)
            if (input.L1 || input.LT > 0.5f)
            {
                LevelDownSelectedSkill();
                return true;
            }

            // Skill-Punkte automatisch verteilen (X-Button, füllt verbleibende Punkte in ausgewählten Skill)
            if (input.BtnX)
            {
                var skill = GetSelectedSkill();
                if (skill != null && skill.Level < skill.MaxLevel)
                {
                    var pointsToSpend = Math.Min(AvailableSkillPoints, skill.MaxLevel - skill.Level);
                    skill.Level += pointsToSpend;
                    AvailableSkillPoints -= pointsToSpend;
                    _feedback.Trigger(FeedbackType.TargetLocked);
                }
                return true;
            }

            return false;
        }

        /// <summary>Berechnet den verbleibenden Skill-Punkt-Bedarf für alle Skills auf Max-Level.</summary>
        public int GetTotalPointsNeededForMax()
        {
            return Skills.Sum(s => s.MaxLevel - s.Level);
        }

        /// <summary>Gibt die prozentuale Fertigstellung des Skill-Baums zurück (0-100).</summary>
        public float GetCompletionPercent()
        {
            if (Skills.Count == 0) return 0f;
            var totalMax = Skills.Sum(s => s.MaxLevel);
            var totalCurrent = Skills.Sum(s => s.Level);
            return totalMax > 0 ? (float)totalCurrent / totalMax * 100f : 0f;
        }
    }

    /// <summary>
    /// Repräsentiert einen Skill-Knoten im Skill-Baum.
    /// </summary>
    public class SkillNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Level { get; set; } = 0;
        public int MaxLevel { get; set; } = 10;
        public int RequiredBaseLevel { get; set; } = 1;
        public int RequiredJobLevel { get; set; } = 1;
        public int? PrerequisiteSkillId { get; set; } = null;
        public string IconPath { get; set; } = "";
        public bool IsPassive { get; set; } = false;
        public int GridX { get; set; } = 0;
        public int GridY { get; set; } = 0;
    }
}
