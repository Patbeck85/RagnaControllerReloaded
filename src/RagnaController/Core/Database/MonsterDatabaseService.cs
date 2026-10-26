using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;

namespace RagnaController.Core.Database
{
    /// <summary>
    /// TASK-3001: MonsterDatabaseService — verwaltet die vollständige RO-Monster-Datenbank.
    /// Stellt UI-freundliche Sammlungen, Filter und Suchfunktionen bereit.
    /// White-Hat: Reine Datenbereitstellung, keine Automatisierung im Spiel.
    /// </summary>
    public sealed class MonsterDatabaseService : IDisposable
    {
        private readonly MonsterDatabase _database;
        private bool _disposed;
        private string? _nameFilter;
        private MonsterElement? _elementFilter;
        private MonsterRace? _raceFilter;
        private MonsterSize? _sizeFilter;
        private bool _showMvpOnly;
        private bool _showMinibossOnly;

        public event EventHandler? DataChanged;

        public MonsterDatabaseService(MonsterDatabase? database = null)
        {
            _database = database ?? MonsterDatabase.CreateStarterDatabase();
            RefreshFilteredList();
        }

        public int Count => _database.Count;

        /// <summary>Alle Monster (ungefiltert).</summary>
        public IReadOnlyList<Monster> AllMonsters => _database.GetAllMonsters();

        /// <summary>Gefilterte Monster-Liste.</summary>
        public IReadOnlyList<Monster> FilteredMonsters { get; private set; } = new List<Monster>();

        /// <summary>Suchfilter nach Namen.</summary>
        public string? NameFilter
        {
            get => _nameFilter;
            set
            {
                _nameFilter = value;
                RefreshFilteredList();
            }
        }

        /// <summary>Element-Filter.</summary>
        public MonsterElement? ElementFilter
        {
            get => _elementFilter;
            set
            {
                _elementFilter = value;
                RefreshFilteredList();
            }
        }

        /// <summary>Rasse-Filter.</summary>
        public MonsterRace? RaceFilter
        {
            get => _raceFilter;
            set
            {
                _raceFilter = value;
                RefreshFilteredList();
            }
        }

        /// <summary>Größen-Filter.</summary>
        public MonsterSize? SizeFilter
        {
            get => _sizeFilter;
            set
            {
                _sizeFilter = value;
                RefreshFilteredList();
            }
        }

        /// <summary>Nur MVPs anzeigen.</summary>
        public bool ShowMvpOnly
        {
            get => _showMvpOnly;
            set
            {
                _showMvpOnly = value;
                RefreshFilteredList();
            }
        }

        /// <summary>Nur Minibosse anzeigen.</summary>
        public bool ShowMinibossOnly
        {
            get => _showMinibossOnly;
            set
            {
                _showMinibossOnly = value;
                RefreshFilteredList();
            }
        }

        /// <summary>Alle MVP-Monster.</summary>
        public IReadOnlyList<Monster> MvpMonsters => _database.GetMvps();

        /// <summary>Alle Minibosse.</summary>
        public IReadOnlyList<Monster> Minibosses => _database.GetMinibosses();

        /// <summary>Gibt ein Monster anhand der ID zurück.</summary>
        public Monster? GetMonster(int id) => _database.GetMonster(id);

        /// <summary>Gibt ein Monster anhand des Namens zurück.</summary>
        public Monster? GetMonster(string name) => _database.GetMonster(name);

        /// <summary>Fügt ein Monster zur Datenbank hinzu.</summary>
        public void AddMonster(Monster monster)
        {
            _database.Add(monster);
            RefreshFilteredList();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Entfernt ein Monster anhand der ID.</summary>
        public bool RemoveMonster(int id)
        {
            var removed = _database.Remove(id);
            if (removed)
            {
                RefreshFilteredList();
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
            return removed;
        }

        /// <summary>Importiert mehrere Monster auf einmal.</summary>
        public void ImportMonsters(IEnumerable<Monster> monsters)
        {
            foreach (var m in monsters)
                _database.Add(m);
            RefreshFilteredList();
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Lädt Monster aus dem Rathena-Importer (externer Loader).</summary>
        public void ImportFromRathena(List<Monster> monsters)
        {
            ImportMonsters(monsters);
        }

        private void RefreshFilteredList()
        {
            var monsters = _database.GetAllMonsters().AsEnumerable();

            if (!string.IsNullOrWhiteSpace(_nameFilter))
            {
                var q = _nameFilter.Trim().ToLowerInvariant();
                monsters = monsters.Where(m =>
                    m.Name.ToLowerInvariant().Contains(q) ||
                    m.DisplayName.ToLowerInvariant().Contains(q));
            }

            if (_elementFilter.HasValue)
                monsters = monsters.Where(m => m.Element == _elementFilter.Value);

            if (_raceFilter.HasValue)
                monsters = monsters.Where(m => m.Race == _raceFilter.Value);

            if (_sizeFilter.HasValue)
                monsters = monsters.Where(m => m.Size == _sizeFilter.Value);

            if (_showMvpOnly)
                monsters = monsters.Where(m => m.IsMvp);

            if (_showMinibossOnly)
                monsters = monsters.Where(m => m.IsMiniboss);

            FilteredMonsters = monsters.OrderBy(m => m.Name).ToList().AsReadOnly();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                DataChanged = null;
            }
        }
    }
}
