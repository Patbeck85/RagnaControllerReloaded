using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using RagnaController.Core;

namespace RagnaController
{
    public partial class TutorialWindow : Window
    {
        private int _currentSlide = 0;
        private const int TOTAL_SLIDES = 5;

        // Arrays containing the localization keys and image paths for each slide
        private readonly string[] _titles = { "Tut_1_Title", "Tut_2_Title", "Tut_3_Title", "Tut_4_Title", "Tut_5_Title" };
        private readonly string[] _descs = { "Tut_1_Desc", "Tut_2_Desc", "Tut_3_Desc", "Tut_4_Desc", "Tut_5_Desc" };
        private readonly string[] _mediaPaths = { "tut_focus.mp4", "tut_grid.mp4", "tut_cast.mp4", "tut_aim.mp4" }; // Placeholders

        public TutorialWindow()
        {
            InitializeComponent();
            DrawDots();
            UpdateSlide();
        }

        private void DrawDots()
        {
            DotPanel.Children.Clear();
            var gold = (Brush)FindResource("Gold");
            var bgBorder = (Brush)FindResource("BgBorder");
            for (int i = 0; i < TOTAL_SLIDES; i++)
            {
                var dot = new Ellipse
                {
                    Width = 8, Height = 8, Margin = new Thickness(0, 0, 8, 0),
                    Fill = i == _currentSlide ? gold : bgBorder
                };
                DotPanel.Children.Add(dot);
            }
        }

        private void UpdateSlide()
        {
            // Bind texts dynamically using LocalizationManager
            if (TxtTitle != null) TxtTitle.Text = LocalizationManager.Instance[_titles[_currentSlide]];
            if (TxtDesc != null) TxtDesc.Text = LocalizationManager.Instance[_descs[_currentSlide]];

            // Try to load media (fails silently if file doesn't exist yet)
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Tutorials", _mediaPaths[_currentSlide]);
                if (System.IO.File.Exists(path))
                {
                    if (MediaPreview != null) { MediaPreview.Source = new Uri(path, UriKind.Absolute); MediaPreview.Play(); }
                }
                else
                {
                    if (MediaPreview != null) MediaPreview.Source = null;
                }
            }
            catch { }

            // Update UI Buttons
            if (BtnPrev != null) BtnPrev.IsEnabled = _currentSlide > 0;

            var live = (Brush)FindResource("Live");
            var gold = (Brush)FindResource("Gold");

            if (_currentSlide == TOTAL_SLIDES - 1)
            {
                if (BtnNext != null) { BtnNext.Content = LocalizationManager.Instance["Tut_Btn_Finish"]; BtnNext.Foreground = live; }
            }
            else
            {
                if (BtnNext != null) { BtnNext.Content = LocalizationManager.Instance["Tut_Btn_Next"]; BtnNext.Foreground = gold; }
            }

            DrawDots();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlide < TOTAL_SLIDES - 1)
            {
                _currentSlide++;
                UpdateSlide();
            }
            else
            {
                Close(); // Finish clicked
            }
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlide > 0)
            {
                _currentSlide--;
                UpdateSlide();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}
