using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RagnaController.Core.AI;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// SkillRecommendationWindow — Geführter Assistent für Skill-Build-Empfehlungen.
    /// Nutzt SkillRecommendationEngine für regelbasierte White-Hat-Empfehlungen.
    /// </summary>
    public partial class SkillRecommendationWindow : Window
    {
        private readonly SkillRecommendationEngine _engine = new();

        public SkillRecommendationWindow()
        {
            InitializeComponent();
            InitializeBaseClasses();
            InitializeLevels();
        }

        private void InitializeBaseClasses()
        {
            var classes = new[]
            {
                ("Swordman", "Schwertkämpfer"),
                ("Magician", "Magier"),
                ("Archer", "Bogenschütze"),
                ("Acolyte", "Akolyt"),
                ("Merchant", "Händler"),
                ("Thief", "Dieb"),
            };
            foreach (var (key, display) in classes)
            {
                CmbBaseClass.Items.Add(new ComboBoxItem { Content = display, Tag = key });
            }
            CmbBaseClass.SelectedIndex = 0;
        }

        private void InitializeLevels()
        {
            for (int i = 1; i <= 99; i++)
            {
                CmbBaseLevel.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }
            CmbBaseLevel.SelectedIndex = 0;
        }

        private void OnInputChanged(object sender, RoutedEventArgs e)
        {
            BtnRecommend_Click(sender, e);
        }

        private void BtnRecommend_Click(object sender, RoutedEventArgs e)
        {
            if (CmbBaseClass.SelectedItem is not ComboBoxItem baseClassItem ||
                CmbBaseLevel.SelectedItem is not ComboBoxItem baseLevelItem)
                return;

            string baseClass = baseClassItem.Tag?.ToString() ?? "Swordman";
            string? jobClass = null;
            if (CmbJobClass.SelectedItem is ComboBoxItem jobClassItem)
                jobClass = jobClassItem.Tag?.ToString();

            int baseLevel = (int)(baseLevelItem.Tag ?? 1);
            int jobLevel = 1;

            string playstyle = "PvM";
            if (RbPlaystylePvp.IsChecked == true) playstyle = "PvP";
            else if (RbPlaystyleSupport.IsChecked == true) playstyle = "Support";
            else if (RbPlaystyleHybrid.IsChecked == true) playstyle = "Hybrid";

            var recommendations = _engine.Recommend(baseClass, jobClass, baseLevel, jobLevel, playstyle, 8);

            ResultsList.Items.Clear();
            foreach (var rec in recommendations)
            {
                var template = rec.Template;
                ResultsList.Items.Add(new
                {
                    Name = template.Description,
                    Description = $"Job: {template.JobClass ?? "Base"}, Level {template.MinBaseLevel}+",
                    ScoreText = $"{rec.MatchScore:P0}"
                });
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}