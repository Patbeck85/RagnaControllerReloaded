using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Data;
using System.Windows.Markup;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static readonly LocalizationManager _instance = new();
        public static LocalizationManager Instance => _instance;

        private Dictionary<string, string> _localizedStrings = new();
        private Dictionary<string, string> _fallbackStrings = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        private string _currentLanguage = "en";
        public string CurrentLanguage 
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    LoadLanguage(value);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                }
            }
        }

        private LocalizationManager() 
        {
            LoadLanguage("en", isFallback: true);
        }

        public string this[string key]
        {
            get
            {
                if (_localizedStrings.TryGetValue(key, out var val)) return val;
                if (_fallbackStrings.TryGetValue(key, out var fallback)) return fallback;
                return $"[{key}]"; // Missing translation indicator
            }
        }

        public static string GetLocalizedString(string key) => Instance[key];

        public void LoadLanguage(string langCode, bool isFallback = false)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Locales", $"{langCode}.json");
                if (!File.Exists(path))
                    path = Path.Combine(AppContext.BaseDirectory, "Locales", "en.json");

                if (File.Exists(path))
                {
                    try
                    {
                        string json = File.ReadAllText(path);
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                                   ?? new Dictionary<string, string>();
                        if (isFallback) _fallbackStrings = dict;
                        else _localizedStrings = dict;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Localization] Failed to load {path}: {ex.Message}");
                    }

                    // Notify WPF that ALL indexer properties have changed -> Live UI Refresh!
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(System.Windows.Data.Binding.IndexerName));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[i18n] Error loading language {langCode}: {ex.Message}");
            }
        }
    }

    // Custom Markup Extension for clean XAML usage
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationManager.Instance,
                Mode = BindingMode.OneWay
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
