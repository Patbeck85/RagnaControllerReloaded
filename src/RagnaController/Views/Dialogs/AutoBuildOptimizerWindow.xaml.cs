using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RagnaController.Core.Calculators;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// AutoBuildOptimizerWindow — Automatische Stat-Allokations-Empfehlungen.
    /// Nutzt AutoBuildOptimizer für regelbasierte Stat-Build-Vorschläge (White-Hat).
    /// </summary>
    public partial class AutoBuildOptimizerWindow : Window
    {
        private readonly AutoBuildOptimizer _optimizer = new();

        public AutoBuildOptimizerWindow()
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
            BtnOptimize_Click(sender, e);
        }

        private void BtnOptimize_Click(object sender, RoutedEventArgs e)
        {
            if (CmbBaseClass.SelectedItem is not ComboBoxItem baseClassItem ||
                CmbBaseLevel.SelectedItem is not ComboBoxItem baseLevelItem)
                return;

            string baseClass = baseClassItem.Tag?.ToString() ?? "Swordman";
            string? jobClass = null;
            if (CmbJobClass.SelectedItem is ComboBoxItem jobClassItem)
                jobClass = jobClassItem.Tag?.ToString();

            int baseLevel = (int)(baseLevelItem.Tag ?? 1);

            string playstyle = "PvM";
            if (RbPlaystylePvp.IsChecked == true) playstyle = "PvP";
            else if (RbPlaystyleSupport.IsChecked == true) playstyle = "Support";
            else if (RbPlaystyleHybrid.IsChecked == true) playstyle = "Hybrid";

            // Finde passende Templates (StatBuildTemplate hat keine JobClass/MinBaseLevel Properties)
            var templates = _optimizer.Templates
                .Where(t => t.BaseClass.Equals(baseClass, StringComparison.OrdinalIgnoreCase))
                .Where(t => playstyle == null || t.Playstyle == "Hybrid" || t.Playstyle.Equals(playstyle, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => CalculateMatchScore(t, baseLevel, playstyle))
                .Take(1)
                .ToList();

            if (templates.Count > 0)
            {
                var t = templates[0];
                TxtStatStr.Text = ((int)t.StrWeight).ToString();
                TxtStatAgi.Text = ((int)t.Agi).ToString();
                TxtStatVit.Text = ((int)t.Vit).ToString();
                TxtStatInt.Text = ((int)t.Int).ToString();
                TxtStatDex.Text = ((int)t.Dex).ToString();
                TxtStatLuk.Text = ((int)t.Luk).ToString();
            }
            else
            {
                TxtStatStr.Text = "—";
                TxtStatAgi.Text = "—";
                TxtStatVit.Text = "—";
                TxtStatInt.Text = "—";
                TxtStatDex.Text = "—";
                TxtStatLuk.Text = "—";
            }
        }

        private double CalculateMatchScore(StatBuildTemplate t, int baseLevel, string playstyle)
        {
            double score = 0.0;
            int levelDiff = Math.Abs(baseLevel - 1); // StatBuildTemplate hat kein MinBaseLevel, verwende 1 als Default
            score += Math.Max(0, 100 - levelDiff * 2);
            if (t.Playstyle == playstyle) score += 50;
            else if (t.Playstyle == "Hybrid") score += 25;
            return score;
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