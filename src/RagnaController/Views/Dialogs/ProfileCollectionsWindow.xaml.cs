using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// TASK-2705: ProfileCollectionsWindow — UI für das Verwalten, Teilen und Abonnieren
    /// von Profile Collections.
    ///
    /// Ermöglicht:
    ///   • Anzeigen aller Profile Collections
    ///   • Neue Collection erstellen, bearbeiten, löschen
    ///   • Profile zu einer Collection hinzufügen/entfernen
    ///   • Collection öffentlich teilen (Gist) / abonnieren
    /// </summary>
    public partial class ProfileCollectionsWindow : Window
    {
        private readonly ProfileManager _profileManager;
        private ProfileCollection? _selectedCollection;
        private readonly ObservableCollection<ProfileCollection> _collections = new();
        private readonly ObservableCollection<string> _availableProfiles = new();
        private readonly ObservableCollection<string> _collectionProfiles = new();

        public ProfileCollectionsWindow(ProfileManager profileManager)
        {
            InitializeComponent();
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));

            CollectionList.ItemsSource = _collections;
            ProfileList.ItemsSource = _collectionProfiles;

            RefreshCollections();
            RefreshAvailableProfiles();
        }

        private void RefreshCollections()
        {
            _collections.Clear();
            if (_profileManager.ProfileCollections != null)
            {
                foreach (var c in _profileManager.ProfileCollections)
                    _collections.Add(c);
            }
            if (_collections.Count > 0 && CollectionList.SelectedIndex < 0)
                CollectionList.SelectedIndex = 0;
        }

        private void RefreshAvailableProfiles()
        {
            _availableProfiles.Clear();
            foreach (var p in _profileManager.GetAllProfiles())
                _availableProfiles.Add(p.Name);
        }

        private void RefreshCollectionProfiles()
        {
            _collectionProfiles.Clear();
            if (_selectedCollection != null)
            {
                foreach (var pn in _selectedCollection.ProfileNames)
                    _collectionProfiles.Add(pn);
            }
            BtnRemoveProfile.IsEnabled = _collectionProfiles.Count > 0;
        }

        private void CollectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionList.SelectedItem is ProfileCollection collection)
            {
                _selectedCollection = collection;
                TxtName.Text = collection.Name;
                TxtDescription.Text = collection.Description;
                ChkPublic.IsChecked = collection.IsPublic;
                RefreshCollectionProfiles();
                BtnDelete.IsEnabled = true;
            }
            else
            {
                _selectedCollection = null;
                ClearEditFields();
                BtnDelete.IsEnabled = false;
            }
        }

        private void ClearEditFields()
        {
            TxtName.Text = "";
            TxtDescription.Text = "";
            ChkPublic.IsChecked = false;
            _collectionProfiles.Clear();
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var newCollection = new ProfileCollection
            {
                Name = "Neue Sammlung",
                Description = "",
                ProfileNames = new List<string>(),
                CreatedAt = DateTime.Now,
                LastModifiedAt = DateTime.Now,
                IsPublic = false,
            };
            _profileManager.AddCollection(newCollection);
            RefreshCollections();
            CollectionList.SelectedItem = newCollection;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null) return;

            _selectedCollection.Name = TxtName.Text.Trim();
            _selectedCollection.Description = TxtDescription.Text.Trim();
            _selectedCollection.IsPublic = ChkPublic.IsChecked == true;
            _selectedCollection.LastModifiedAt = DateTime.Now;

            _profileManager.UpdateCollection(_selectedCollection);
            RefreshCollections();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null) return;

            if (MessageBox.Show($"Collection '{_selectedCollection.Name}' wirklich löschen?",
                    "Löschen bestätigen", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _profileManager.RemoveCollectionById(_selectedCollection.Id);
                _selectedCollection = null;
                RefreshCollections();
                ClearEditFields();
            }
        }

        private void BtnAddProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null) return;

            var availableProfiles = _profileManager.GetAllProfiles()
                .Select(p => p.Name)
                .Where(n => !_selectedCollection.ProfileNames.Contains(n))
                .ToList();

            if (availableProfiles.Count == 0)
            {
                MessageBox.Show("Alle Profile sind bereits in dieser Sammlung.", "Keine Profile verfügbar",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new SelectProfilesDialog(availableProfiles);
            if (dlg.ShowDialog() == true)
            {
                foreach (var pn in dlg.SelectedProfiles)
                {
                    _selectedCollection.AddProfile(pn);
                    _collectionProfiles.Add(pn);
                }
                _profileManager.UpdateCollection(_selectedCollection);
                RefreshCollections();
            }
        }

        private void BtnRemoveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null || ProfileList.SelectedItem == null) return;

            var profileName = (string)ProfileList.SelectedItem;
            _selectedCollection.RemoveProfile(profileName);
            _collectionProfiles.Remove(profileName);
            _profileManager.UpdateCollection(_selectedCollection);
            RefreshCollections();
        }

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCollection == null) return;

            _selectedCollection.IsPublic = true;
            _selectedCollection.LastModifiedAt = DateTime.Now;
            _profileManager.UpdateCollection(_selectedCollection);
            ChkPublic.IsChecked = true;
            MessageBox.Show($"Collection '{_selectedCollection.Name}' wurde öffentlich geteilt.", "Geteilt",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSubscribe_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abonnieren öffentlicher Collections wird in einer zukünftigen Version unterstützt.",
                "Abonnieren", MessageBoxButton.OK, MessageBoxImage.Information);
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
