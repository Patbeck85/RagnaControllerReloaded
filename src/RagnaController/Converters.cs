using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RagnaController
{
    /// <summary>
    /// Application class.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => value is Visibility.Visible;
    }

    /// <summary>
    /// Application class.
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => value is not Visibility.Visible;
    }

    /// <summary>
    /// TASK-1612: Converts RO character class names to icon image paths.
    /// Maps class names (e.g., "Wizard", "Knight") to Assets\Classes\*.png files.
    /// </summary>
    public class ClassToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var className = value?.ToString() ?? "";
            var iconPath = className switch
            {
                "Wizard" or "Mage" or "High Wizard" or "Sage" or "Professor" => "pack://application:,,,/Assets/Classes/wizard.png",
                "Knight" or "Lord Knight" or "Crusader" => "pack://application:,,,/Assets/Classes/knight.png",
                "Assassin Cross" => "pack://application:,,,/Assets/Classes/assassin_cross.png",
                "Sniper" or "Hunter" => "pack://application:,,,/Assets/Classes/sniper.png",
                _ => "pack://application:,,,/Assets/Classes/unknown.png"
            };
            try
            {
                return new BitmapImage(new Uri(iconPath));
            }
            catch
            {
                return new BitmapImage(new Uri("pack://application:,,,/Assets/Classes/unknown.png"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
