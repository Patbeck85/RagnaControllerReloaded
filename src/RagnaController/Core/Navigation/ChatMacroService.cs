using System;
using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core.Navigation {
    /// <summary>
    /// TASK-2504: ChatMacroService — Schnelltexte per D-Pad in Chat einfügen (Emotes, Befehle).
    ///
    /// Ermöglicht das schnelle Einfügen von vordefinierten Chat-Makros über Controller:
    ///   • Makro-Liste mit bis zu 8 Slots (D-Pad für Navigation)
    ///   • Emotes, Befehle, Vordefinierte Nachrichten
    ///   • Makro-Eingabe über Chat-String an InputCommandQueue
    ///   • Makro-Profil-Speicherung (pro Charakter/Klasse)
    /// </summary>
    public sealed class ChatMacroService
    {
        private readonly InputCommandQueue _queue;
        private readonly IFeedbackProvider _feedback;

        // ── Chat State ──────────────────────────────────────────────────────
        public bool IsChatOpen { get; private set; }
        public int SelectedMacroIndex { get; set; } = 0;

        // ── Makros ──────────────────────────────────────────────────────────
        public List<ChatMacro> Macros { get; } = new()
        {
            new ChatMacro { Id = 1, Name = "Hallo", Text = "/hello Hallo!", Category = "Emotes" },
            new ChatMacro { Id = 2, Name = "Danke", Text = "/emote Danke!", Category = "Emotes" },
            new ChatMacro { Id = 3, Name = "LFG", Text = "/shout LFG - Dungeon!", Category = "Befehle" },
            new ChatMacro { Id = 4, Name = "Buff pls", Text = "/whisper Buff bitte!", Category = "Befehle" },
            new ChatMacro { Id = 5, Name = "AFK", Text = "/afk Bin gleich zurück!", Category = "Status" },
            new ChatMacro { Id = 6, Name = "Trade", Text = "/shout Handel - PM mich!", Category = "Befehle" },
            new ChatMacro { Id = 7, Name = "Glückwunsch", Text = "/congrats Glückwunsch!", Category = "Emotes" },
            new ChatMacro { Id = 8, Name = "Custom", Text = "", Category = "Sonstiges" },
        };

        public ChatMacroService(InputCommandQueue queue, IFeedbackProvider feedback)
        {
            _queue = queue;
            _feedback = feedback;
        }

        /// <summary>Öffnet den Chat-Makro-Modus.</summary>
        public void OpenChat()
        {
            IsChatOpen = true;
            SelectedMacroIndex = 0;
            _feedback.Trigger(FeedbackType.PrecisionModeOn);
        }

        /// <summary>Schließt den Chat-Makro-Modus.</summary>
        public void CloseChat()
        {
            IsChatOpen = false;
            _feedback.Trigger(FeedbackType.PrecisionModeOff);
        }

        /// <summary>Fügt ein benutzerdefiniertes Makro hinzu.</summary>
        public void AddMacro(ChatMacro macro)
        {
            Macros.Add(macro);
        }

        /// <summary>Entfernt ein Makro nach ID.</summary>
        public bool RemoveMacro(int id)
        {
            var macro = Macros.FirstOrDefault(m => m.Id == id);
            if (macro == null) return false;
            Macros.Remove(macro);
            if (SelectedMacroIndex >= Macros.Count)
                SelectedMacroIndex = Math.Max(0, Macros.Count - 1);
            return true;
        }

        /// <summary>Gibt das aktuell ausgewählte Makro zurück.</summary>
        public ChatMacro? GetSelectedMacro()
        {
            if (Macros.Count == 0 || SelectedMacroIndex < 0 || SelectedMacroIndex >= Macros.Count)
                return null;
            return Macros[SelectedMacroIndex];
        }

        /// <summary>Sendet das ausgewählte Makro an den Chat.</summary>
        public void SendSelectedMacro()
        {
            var macro = GetSelectedMacro();
            if (macro == null || string.IsNullOrWhiteSpace(macro.Text))
                return;

            _queue.SendChatString(macro.Text);
            _feedback.Trigger(FeedbackType.TargetLocked);
        }

        /// <summary>Verarbeitet Controller-Eingaben für Chat-Makros.</summary>
        public bool ProcessInput(ParsedInput input)
        {
            if (!IsChatOpen)
                return false;

            // Chat verlassen (B / Back / Enter)
            if (input.BtnB || input.Back)
            {
                CloseChat();
                return true;
            }

            // Makro-Navigation (D-Pad Hoch/Runter)
            if (input.DPadUp)
            {
                SelectedMacroIndex = Math.Max(0, SelectedMacroIndex - 1);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }
            if (input.DPadDown)
            {
                SelectedMacroIndex = Math.Min(Macros.Count - 1, SelectedMacroIndex + 1);
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            // Makro senden (A-Button)
            if (input.BtnA)
            {
                SendSelectedMacro();
                return true;
            }

            // Makro-Kategorie wechseln (D-Pad Links/Rechts)
            if (input.DPadLeft || input.DPadRight)
            {
                var current = GetSelectedMacro();
                if (current != null)
                {
                    var categories = Macros.Select(m => m.Category).Distinct().ToList();
                    var currentCatIndex = categories.IndexOf(current.Category);
                    var nextCatIndex = input.DPadRight
                        ? (currentCatIndex + 1) % categories.Count
                        : (currentCatIndex - 1 + categories.Count) % categories.Count;

                    var nextCategory = categories[nextCatIndex];
                    var firstInCategory = Macros.FindIndex(m => m.Category == nextCategory);
                    if (firstInCategory >= 0)
                        SelectedMacroIndex = firstInCategory;
                }
                _feedback.Trigger(FeedbackType.Warning);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Repräsentiert ein Chat-Makro.
    /// </summary>
    public class ChatMacro
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Text { get; set; } = "";
        public string Category { get; set; } = "Sonstiges";
        public string? IconPath { get; set; }
    }
}
