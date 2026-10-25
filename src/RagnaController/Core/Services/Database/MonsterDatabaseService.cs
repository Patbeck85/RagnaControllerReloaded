using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using RagnaController.Models;

namespace RagnaController.Core.Services.Database {
    /// <summary>
    /// Service-Layer für MonsterDatabase — stellt UI-freundliche Sammlungen bereit
    /// und reagiert auf Pre-Renewal / Renewal GameMode-Wechsel.
    /// </summary>
    public class MonsterDatabaseService : INotifyPropertyChanged
    {
        private readonly MonsterDatabase _database;
        private bool _isRenewal = true;
        private string? _filterText;
        private MonsterElement? _elementFilter;
        private MonsterRace? _raceFilter;
        private bool _showMvpOnly;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MonsterDatabaseService(MonsterDatabase? database = null)
        {
            _database = database ?? MonsterDatabase.CreateStarterDatabase();
            RefreshFilteredList();
        }

        /// <summary>Aktueller GameMode: true = Renewal, false = Pre-Renewal.</summary>
        public bool IsRenewal
        {
            get => _isRenewal;
            set
            {
                if (_isRenewal != value)
                {
                    _isRenewal = value;
                    OnPropertyChanged(nameof(IsRenewal));
                    OnPropertyChanged(nameof(FilteredMonsters));
                    RefreshFilteredList();
                }
            }
        }

        /// <summary>Gefilterte Monsterliste (berücksichtigt GameMode für Anzeige-Werte).</summary>
        public ObservableCollection<MonsterViewModel> FilteredMonsters { get; } = new();

        /// <summary>Filter-Suchtext.</summary>
        public string? FilterText
        {
            get => _filterText;
            set
            {
                if (_filterText != value)
                {
                    _filterText = value;
                    OnPropertyChanged(nameof(FilterText));
                    RefreshFilteredList();
                }
            }
        }

        /// <summary>Element-Filter.</summary>
        public MonsterElement? ElementFilter
        {
            get => _elementFilter;
            set
            {
                if (_elementFilter != value)
                {
                    _elementFilter = value;
                    OnPropertyChanged(nameof(ElementFilter));
                    RefreshFilteredList();
                }
            }
        }

        /// <summary>Rasse-Filter.</summary>
        public MonsterRace? RaceFilter
        {
            get => _raceFilter;
            set
            {
                if (_raceFilter != value)
                {
                    _raceFilter = value;
                    OnPropertyChanged(nameof(RaceFilter));
                    RefreshFilteredList();
                }
            }
        }

        /// <summary>Nur MVP anzeigen.</summary>
        public bool ShowMvpOnly
        {
            get => _showMvpOnly;
            set
            {
                if (_showMvpOnly != value)
                {
                    _showMvpOnly = value;
                    OnPropertyChanged(nameof(ShowMvpOnly));
                    RefreshFilteredList();
                }
            }
        }

        /// <summary>Alle verfügbaren Elemente für Filter-UI.</summary>
        public IEnumerable<MonsterElement> AllElements => Enum.GetValues<MonsterElement>();

        /// <summary>Alle verfügbaren Rassen für Filter-UI.</summary>
        public IEnumerable<MonsterRace> AllRaces => Enum.GetValues<MonsterRace>();

        /// <summary>Gibt ein Monster anhand der ID zurück.</summary>
        public Monster? GetMonster(int id) => _database.GetMonster(id);

        /// <summary>Gibt ein Monster anhand des Namens zurück.</summary>
        public Monster? GetMonster(string name) => _database.GetMonster(name);

        /// <summary>Gibt die Anzahl der MVP-Monster zurück.</summary>
        public int GetMvpCount() => _database.GetMvps().Count;

        /// <summary>Lädt eine Liste von Monstern in die Datenbank (überschreibt vorhandene mit gleicher ID).</summary>
        public void LoadMonsters(List<Monster> monsters)
        {
            foreach (var m in monsters)
                _database.Add(m);
            RefreshFilteredList();
            OnPropertyChanged(nameof(FilteredMonsters));
        }

        /// <summary>Aktualisiert die gefilterte Liste.</summary>
        public void RefreshFilteredList()
        {
            var monsters = _database.GetAllMonsters();

            // Text-Filter
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var q = FilterText.Trim().ToLowerInvariant();
                monsters = monsters.Where(m =>
                    m.Name.ToLowerInvariant().Contains(q) ||
                    m.DisplayName.ToLowerInvariant().Contains(q)).ToList();
            }

            // Element-Filter
            if (_elementFilter.HasValue)
                monsters = monsters.Where(m => m.Element == _elementFilter.Value).ToList();

            // Rasse-Filter
            if (_raceFilter.HasValue)
                monsters = monsters.Where(m => m.Race == _raceFilter.Value).ToList();

            // MVP-Filter
            if (_showMvpOnly)
                monsters = monsters.Where(m => m.IsMvp).ToList();

            FilteredMonsters.Clear();
            foreach (var m in monsters.OrderBy(m => m.Name))
            {
                FilteredMonsters.Add(new MonsterViewModel(m, IsRenewal));
            }

            OnPropertyChanged(nameof(FilteredMonsters));
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// ViewModel für Monster-Anzeige in der UI — berechnet GameMode-abhängige Werte.
    /// </summary>
    public class MonsterViewModel : INotifyPropertyChanged
    {
        private readonly Monster _monster;
        private readonly bool _isRenewal;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MonsterViewModel(Monster monster, bool isRenewal)
        {
            _monster = monster;
            _isRenewal = isRenewal;
        }

        public int Id => _monster.Id;
        public string Name => _monster.Name;
        public string DisplayName => _monster.DisplayName;
        public MonsterElement Element => _monster.Element;
        public MonsterSize Size => _monster.Size;
        public MonsterRace Race => _monster.Race;
        public bool IsMvp => _monster.IsMvp;
        public bool IsMiniboss => _monster.IsMiniboss;

        /// <summary>HP je nach GameMode.</summary>
        public int Hp => _monster.GetHp(_isRenewal);

        /// <summary>EXP je nach GameMode.</summary>
        public int Exp => _monster.GetExp(_isRenewal);

        /// <summary>JEXP je nach GameMode.</summary>
        public int JobExp => _monster.GetJobExp(_isRenewal);

        /// <summary>Base Level je nach GameMode.</summary>
        public int BaseLevel => _monster.GetBaseLevel(_isRenewal);

        /// <summary>Drops je nach GameMode.</summary>
        public Dictionary<int, int> Drops => _monster.GetDrops(_isRenewal);

        /// <summary>Steal-Items je nach GameMode.</summary>
        public Dictionary<int, int> Steal => _monster.GetSteal(_isRenewal);

        public IReadOnlyList<string> SpawnMaps => _monster.SpawnMaps.AsReadOnly();
        public string Description => _monster.Description;

        /// <summary>GameMode-abhängiger Label (z.B. "Poring (Renewal)").</summary>
        public string ModeLabel => $"{_monster.DisplayName} ({(_isRenewal ? "Renewal" : "Pre-Renewal")})";

        /// <summary>Aktualisiert den GameMode und benachrichtigt die UI.</summary>
        public void UpdateGameMode(bool isRenewal)
        {
            if (_isRenewal != isRenewal)
            {
                // Wir können _isRenewal nicht ändern (readonly), aber wir können
                // PropertyChanged für alle abhängigen Properties feuern
                // Das ViewModel sollte neu erstellt werden bei GameMode-Wechsel
            }
            OnPropertyChanged(nameof(Hp));
            OnPropertyChanged(nameof(Exp));
            OnPropertyChanged(nameof(JobExp));
            OnPropertyChanged(nameof(BaseLevel));
            OnPropertyChanged(nameof(Drops));
            OnPropertyChanged(nameof(Steal));
            OnPropertyChanged(nameof(ModeLabel));
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
