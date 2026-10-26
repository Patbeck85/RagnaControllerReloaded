using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// SelectProfilesDialog — Einfacher Dialog zur Auswahl eines oder mehrerer Profile.
    /// Wird vom ProfileCollectionsWindow verwendet, um Profile zu einer Collection hinzuzufügen.
    /// </summary>
    public partial class SelectProfilesDialog : Window
    {
        public List<string> SelectedProfiles { get; } = new();

        public SelectProfilesDialog(IEnumerable<string> availableProfiles)
        {
            InitializeComponent();

            foreach (var profile in availableProfiles)
                ProfileList.Items.Add(profile);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedProfiles.Clear();
            foreach (var item in ProfileList.SelectedItems)
                SelectedProfiles.Add(item?.ToString() ?? "");

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
