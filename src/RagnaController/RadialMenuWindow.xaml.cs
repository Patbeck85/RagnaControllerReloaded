using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController
{
    public partial class RadialMenuWindow : Window, IComponentConnector
    {
        private static readonly Lazy<LocalizationManager> _localization = new(() => LocalizationManager.Instance);
        public static string GetLocalizedString(string key) => LocalizationManager.GetLocalizedString(key);

        private List<RadialItem>? _items;
        private int _selectedIndex = -1;
        private readonly List<Border> _visualItems = new List<Border>();
        private readonly InputCommandQueue? _queue;

        /// <summary>
        /// FEAT-002: Configurable radial menu - loaded from profile settings.
        /// Items can define Key (VirtualKey), Command (chat string), IsEmote flag, and optional ImagePath.
        /// Default configuration includes 8 common RO commands mapped to keyboard shortcuts.
        /// </summary>
        private static readonly RadialItem[] DefaultItems = new RadialItem[]
        {
            new RadialItem { Key = VirtualKey.F1, Name = "Bash", IsEmote = false },
            new RadialItem { Key = VirtualKey.F2, Name = "Magnum Break", IsEmote = false },
            new RadialItem { Key = VirtualKey.F3, Name = "Heilung", IsEmote = false },
            new RadialItem { Key = VirtualKey.F4, Name = "Blitz", IsEmote = false },
            new RadialItem { Key = VirtualKey.F5, Name = "Schnellangriff", IsEmote = false },
            new RadialItem { Key = VirtualKey.F6, Name = "Flehen", IsEmote = false },
            new RadialItem { Key = VirtualKey.F7, Name = "Sanftheilung", IsEmote = false },
            new RadialItem { Key = VirtualKey.F8, Name = "Kurative Magie", IsEmote = false },
        };

        /// <summary>
        /// FEAT-002: Radial menu configuration from profile.
        /// Can be overridden via RadialMenuConfig in Profile JSON.
        /// </summary>
        public sealed class RadialMenuConfig
        {
            public RadialItem[] Items { get; set; } = DefaultItems;
            public double? ItemRadius { get; set; } = null; // pixels from center
            public double? SelectionHighlightAlpha { get; set; } = null; // 0-255
        }

        private readonly RadialMenuConfig _config;

        public RadialMenuWindow(InputCommandQueue queue, RadialMenuConfig config = null) : this()
        {
            _config = config ?? new RadialMenuConfig();
            InitializeFromConfig();
            _queue = queue;
        }

        public RadialMenuWindow(InputCommandQueue queue) : this(queue, null) { }

        public RadialMenuWindow()
        {
            InitializeComponent();
            _items = new List<RadialItem>();
        }

        private void InitializeFromConfig()
        {
            // Use configured items if provided, otherwise defaults
            var items = _config.Items ?? DefaultItems;
            _items = new List<RadialItem>(items);
        }

        public void Connect(int targetId, object connector)
        {
        }

        private void DrawItems()
        {
            if (ItemsCanvas == null) return;
            ItemsCanvas.Children.Clear();
            _visualItems.Clear();

            if (_items == null || _items.Count == 0) return;

            double angleStep = 360.0 / _items.Count;

            for (int i = 0; i < _items.Count; i++)
            {
                double angle = i * angleStep - 90;
                double rad = angle * Math.PI / 180.0;

                bool hasImage = !string.IsNullOrEmpty(_items[i].ImagePath) && File.Exists(_items[i].ImagePath);
                double itemH = hasImage ? 52 : 36;

                double x = 170 + Math.Cos(rad) * 128 - 50;
                double y = 170 + Math.Sin(rad) * 128 - (itemH / 2);

                var stack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                if (hasImage)
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(_items[i].ImagePath, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.DecodePixelHeight = 28;
                        bmp.EndInit();
                        bmp.Freeze();
                        var img = new Image { Source = bmp, Width = 28, Height = 28 };
                        RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                        stack.Children.Add(img);
                    }
                    catch { }
                }

                stack.Children.Add(new TextBlock
                {
                    Text = _items[i].Name,
                    Foreground = new SolidColorBrush(Color.FromRgb(125, 139, 158)),
                    FontSize = hasImage ? 9 : 11,
                    FontWeight = FontWeights.Bold
                });

                var border = new Border
                {
                    Width = 100,
                    Height = itemH,
                    Background = new SolidColorBrush(Color.FromArgb(100, 18, 22, 32)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(42, 50, 69)),
                    BorderThickness = new Thickness(1.5),
                    CornerRadius = new CornerRadius(6),
                    Child = stack
                };

                Canvas.SetLeft(border, x);
                Canvas.SetTop(border, y);
                ItemsCanvas.Children.Add(border);
                _visualItems.Add(border);
            }
        }

        public void UpdateSelection(float x, float y)
        {
            if (RootGrid == null) return;
            if (RootGrid.Opacity < 1) RootGrid.Opacity = 1;

            float mag = MathF.Sqrt(x * x + y * y);
            if (mag < 0.45f)
            {
                _selectedIndex = -1;
                SelectedText.Text = GetLocalizedString("RadialMenu_SelectItem");
                ResetVisuals();
                return;
            }

            double angle = (Math.Atan2(x, y) * 180.0 / Math.PI + 360) % 360;
            if (_items == null || _items.Count == 0) return; // Null check for CS8602

            double sectorSize = 360.0 / _items.Count;
            _selectedIndex = (int)((angle + sectorSize / 2.0) / sectorSize) % _items.Count;

            ResetVisuals();
            if (_selectedIndex >= 0 && _selectedIndex < _visualItems.Count && _selectedIndex < _items.Count)
            {
                var b = _visualItems[_selectedIndex];
                b.BorderBrush = Brushes.White;
                b.Background = new SolidColorBrush(Color.FromArgb(140, 229, 184, 66));
                if (SelectedText != null) SelectedText.Text = _items[_selectedIndex].Name;
            }
        }

        private void ResetVisuals()
        {
            if (_items == null) return; // Null check for CS8602
            foreach (var b in _visualItems)
            {
                b.BorderBrush = new SolidColorBrush(Color.FromRgb(42, 50, 69));
                b.Background = new SolidColorBrush(Color.FromArgb(100, 18, 22, 32));
            }
        }

        public void ExecuteAndClose()
        {
            if (_items == null || _selectedIndex < 0 || _selectedIndex >= _items.Count) return; // Null check for CS8602

            var item = _items[_selectedIndex];
            if (item.IsEmote && !string.IsNullOrWhiteSpace(item.Command))
            {
                _queue.SendChatString(item.Command);
            }
            else if (item.Key != VirtualKey.None)
            {
                _queue.TapKey(item.Key);
            }
            _selectedIndex = -1;
            this.Visibility = Visibility.Hidden;
        }

        public void Reopen(List<RadialItem> newItems)
        {
            _items = newItems;
            DrawItems();
            _selectedIndex = -1;
            this.Visibility = Visibility.Visible;
        }
    }
}