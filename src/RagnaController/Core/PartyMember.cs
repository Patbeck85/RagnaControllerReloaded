using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-2703: PartyMember — Modell für ein einzelnes Party-Mitglied im Guild/Party Overlay.
    ///
    /// Enthält Name, Klasse, HP, SP, Buffs, Debuffs, Distanz und Online-Status.
    /// Wird vom PartyOverlayService verwaltet und vom InGameOverlayWindow angezeigt.
    /// </summary>
    public sealed class PartyMember : INotifyPropertyChanged
    {
        private string _name = "";
        private int _hpPercent = 100;
        private int _spPercent = 100;
        private int _distance;
        private bool _isOnline = true;
        private bool _isDead;
        private string _className = "Novice";
        private string _map = "";
        private readonly List<string> _buffs = new();
        private readonly List<string> _debuffs = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Charakter-Name des Mitglieds.</summary>
        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        /// <summary>Klasse (z.B. "Knight", "Wizard", "Priest").</summary>
        public string ClassName
        {
            get => _className;
            set { _className = value; OnPropertyChanged(nameof(ClassName)); }
        }

        /// <summary>HP in Prozent (0-100).</summary>
        public int HpPercent
        {
            get => _hpPercent;
            set { _hpPercent = Math.Clamp(value, 0, 100); OnPropertyChanged(nameof(HpPercent)); }
        }

        /// <summary>SP in Prozent (0-100).</summary>
        public int SpPercent
        {
            get => _spPercent;
            set { _spPercent = Math.Clamp(value, 0, 100); OnPropertyChanged(nameof(SpPercent)); }
        }

        /// <summary>Distanz zum Spieler in Zellen (0 = nah, größer = weiter).</summary>
        public int Distance
        {
            get => _distance;
            set { _distance = Math.Max(0, value); OnPropertyChanged(nameof(Distance)); }
        }

        /// <summary>Ob das Mitglied online ist.</summary>
        public bool IsOnline
        {
            get => _isOnline;
            set { _isOnline = value; OnPropertyChanged(nameof(IsOnline)); }
        }

        /// <summary>Ob das Mitglied tot ist.</summary>
        public bool IsDead
        {
            get => _isDead;
            set { _isDead = value; OnPropertyChanged(nameof(IsDead)); }
        }

        /// <summary>Aktuelle Karte (z.B. "prontera", "pay_fild01").</summary>
        public string Map
        {
            get => _map;
            set { _map = value; OnPropertyChanged(nameof(Map)); }
        }

        /// <summary>Aktive Buffs (Namen).</summary>
        public IReadOnlyList<string> Buffs => _buffs;

        /// <summary>Aktive Debuffs (Namen).</summary>
        public IReadOnlyList<string> Debuffs => _debuffs;

        /// <summary>Fügt einen Buff hinzu (wenn noch nicht vorhanden).</summary>
        public void AddBuff(string buffName)
        {
            if (string.IsNullOrWhiteSpace(buffName)) return;
            if (!_buffs.Contains(buffName))
            {
                _buffs.Add(buffName);
                OnPropertyChanged(nameof(Buffs));
            }
        }

        /// <summary>Entfernt einen Buff.</summary>
        public void RemoveBuff(string buffName)
        {
            if (_buffs.Remove(buffName))
                OnPropertyChanged(nameof(Buffs));
        }

        /// <summary>Fügt einen Debuff hinzu (wenn noch nicht vorhanden).</summary>
        public void AddDebuff(string debuffName)
        {
            if (string.IsNullOrWhiteSpace(debuffName)) return;
            if (!_debuffs.Contains(debuffName))
            {
                _debuffs.Add(debuffName);
                OnPropertyChanged(nameof(Debuffs));
            }
        }

        /// <summary>Entfernt einen Debuff.</summary>
        public void RemoveDebuff(string debuffName)
        {
            if (_debuffs.Remove(debuffName))
                OnPropertyChanged(nameof(Debuffs));
        }

        /// <summary>Leert alle Buffs.</summary>
        public void ClearBuffs()
        {
            if (_buffs.Count > 0)
            {
                _buffs.Clear();
                OnPropertyChanged(nameof(Buffs));
            }
        }

        /// <summary>Leert alle Debuffs.</summary>
        public void ClearDebuffs()
        {
            if (_debuffs.Count > 0)
            {
                _debuffs.Clear();
                OnPropertyChanged(nameof(Debuffs));
            }
        }

        /// <summary>Erstellt eine Kopie dieses PartyMembers.</summary>
        public PartyMember Clone()
        {
            var clone = new PartyMember
            {
                Name = Name,
                ClassName = ClassName,
                HpPercent = HpPercent,
                SpPercent = SpPercent,
                Distance = Distance,
                IsOnline = IsOnline,
                IsDead = IsDead,
                Map = Map,
            };
            foreach (var b in _buffs) clone.AddBuff(b);
            foreach (var d in _debuffs) clone.AddDebuff(d);
            return clone;
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// TASK-2703: PartyOverlayService — verwaltet Party-Mitglieder für das Guild/Party Overlay.
    ///
    /// Stellt eine beobachtbare Sammlung von PartyMember bereit, die vom
    /// InGameOverlayWindow an die UI gebunden werden kann.
    /// </summary>
    public sealed class PartyOverlayService
    {
        private readonly Dictionary<string, PartyMember> _membersByName = new(StringComparer.OrdinalIgnoreCase);

        public PartyOverlayService()
        {
            // Eigener Spieler als Member 0
            _membersByName["Self"] = new PartyMember { Name = "Self", IsOnline = true, IsDead = false };
        }

        /// <summary>Alle Party-Mitglieder (einschließlich Self).</summary>
        public IReadOnlyList<PartyMember> Members => _membersByName.Values.ToList();

        /// <summary>Anzahl der Mitglieder.</summary>
        public int Count => _membersByName.Count;

        /// <summary>Fügt ein Party-Mitglied hinzu oder aktualisiert es.</summary>
        public void UpsertMember(string name, string className, int hpPercent, int spPercent, bool isOnline, bool isDead, int distance = 0, string? map = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            if (_membersByName.TryGetValue(name, out var existing))
            {
                existing.ClassName = className;
                existing.HpPercent = hpPercent;
                existing.SpPercent = spPercent;
                existing.IsOnline = isOnline;
                existing.IsDead = isDead;
                existing.Distance = distance;
                if (map != null) existing.Map = map;
            }
            else
            {
                var member = new PartyMember
                {
                    Name = name,
                    ClassName = className,
                    HpPercent = hpPercent,
                    SpPercent = spPercent,
                    IsOnline = isOnline,
                    IsDead = isDead,
                    Distance = distance,
                    Map = map ?? "",
                };
                _membersByName[name] = member;
            }
        }

        /// <summary>Entfernt ein Party-Mitglied (z.B. bei Party-Verlassen).</summary>
        public bool RemoveMember(string name)
        {
            if (name.Equals("Self", StringComparison.OrdinalIgnoreCase)) return false;
            return _membersByName.Remove(name);
        }

        /// <summary>Entfernt alle Mitglieder außer Self.</summary>
        public void ClearParty()
        {
            var keysToRemove = new List<string>();
            foreach (var kv in _membersByName)
            {
                if (!kv.Key.Equals("Self", StringComparison.OrdinalIgnoreCase))
                    keysToRemove.Add(kv.Key);
            }
            foreach (var key in keysToRemove)
                _membersByName.Remove(key);
        }

        /// <summary>Gibt ein Mitglied nach Namen zurück (null wenn nicht gefunden).</summary>
        public PartyMember? GetMember(string name)
        {
            return _membersByName.TryGetValue(name, out var member) ? member : null;
        }

        /// <summary>Gibt den schwächsten Nicht-Self-Mitglied nach HP zurück.</summary>
        public PartyMember? GetWeakestMember()
        {
            PartyMember? weakest = null;
            foreach (var member in _membersByName.Values)
            {
                if (member.Name.Equals("Self", StringComparison.OrdinalIgnoreCase)) continue;
                if (!member.IsOnline || member.IsDead) continue;
                if (weakest == null || member.HpPercent < weakest.HpPercent)
                    weakest = member;
            }
            return weakest;
        }

        /// <summary>Gibt alle toten Mitglieder zurück.</summary>
        public List<PartyMember> GetDeadMembers()
        {
            var dead = new List<PartyMember>();
            foreach (var member in _membersByName.Values)
            {
                if (member.IsDead)
                    dead.Add(member);
            }
            return dead;
        }

        /// <summary>Gibt alle Mitglieder mit HP unter einem Schwellenwert zurück.</summary>
        public List<PartyMember> GetLowHpMembers(int thresholdPercent = 50)
        {
            var low = new List<PartyMember>();
            foreach (var member in _membersByName.Values)
            {
                if (member.Name.Equals("Self", StringComparison.OrdinalIgnoreCase)) continue;
                if (!member.IsOnline || member.IsDead) continue;
                if (member.HpPercent <= thresholdPercent)
                    low.Add(member);
            }
            return low;
        }

        /// <summary>Fügt einem Mitglied einen Buff hinzu.</summary>
        public void AddBuffToMember(string memberName, string buffName)
        {
            var member = GetMember(memberName);
            member?.AddBuff(buffName);
        }

        /// <summary>Fügt einem Mitglied einen Debuff hinzu.</summary>
        public void AddDebuffToMember(string memberName, string debuffName)
        {
            var member = GetMember(memberName);
            member?.AddDebuff(debuffName);
        }

        /// <summary>Entfernt einen Buff von einem Mitglied.</summary>
        public void RemoveBuffFromMember(string memberName, string buffName)
        {
            var member = GetMember(memberName);
            member?.RemoveBuff(buffName);
        }

        /// <summary>Entfernt einen Debuff von einem Mitglied.</summary>
        public void RemoveDebuffFromMember(string memberName, string debuffName)
        {
            var member = GetMember(memberName);
            member?.RemoveDebuff(debuffName);
        }

        /// <summary>Leert alle Buffs eines Mitglieds.</summary>
        public void ClearMemberBuffs(string memberName)
        {
            var member = GetMember(memberName);
            member?.ClearBuffs();
        }

        /// <summary>Leert alle Debuffs eines Mitglieds.</summary>
        public void ClearMemberDebuffs(string memberName)
        {
            var member = GetMember(memberName);
            member?.ClearDebuffs();
        }

        /// <summary>Aktualisiert HP/SP eines Mitglieds (z.B. via OCR oder Netzwerk).</summary>
        public void UpdateMemberStats(string name, int hpPercent, int spPercent)
        {
            var member = GetMember(name);
            if (member != null)
            {
                member.HpPercent = hpPercent;
                member.SpPercent = spPercent;
            }
        }
    }
}
