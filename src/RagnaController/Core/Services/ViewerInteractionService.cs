using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-3203: ViewerInteractionService — Zuschauer-Interaktion über Chat.
    ///
    /// Ermöglicht Zuschauern über Chat-Befehle mit RagnaController zu interagieren:
    ///   • Profil-Switch (!profile <name>)
    ///   • Overlay-Toggle (!overlay on/off)
    ///   • Emote-Ausführung (!emote <name>)
    ///   • Sound-Wiedergabe (!sound <name>)
    ///   • Moderator-only Befehle
    ///
    /// White-Hat: Nur UI/Overlay-Steuerung, keine Spielautomatisierung.
    /// </summary>
    public sealed class ViewerInteractionService
    {
        private readonly Dictionary<string, ViewerCommand> _commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _moderators = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _commandCooldowns = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _lastCommandTimes = new(StringComparer.OrdinalIgnoreCase);

        public ViewerInteractionService()
        {
            RegisterDefaultCommands();
        }

        /// <summary>Event bei verarbeitetem Chat-Befehl.</summary>
        public event EventHandler<ViewerCommandEvent>? CommandExecuted;

        /// <summary>Event bei unbekanntem Befehl.</summary>
        public event EventHandler<string>? UnknownCommand;

        /// <summary>Fügt einen Moderator hinzu (nur dieser darf Mod-Befehle nutzen).</summary>
        public void AddModerator(string username)
        {
            _moderators.Add(username.ToLowerInvariant());
        }

        /// <summary>Entfernt einen Moderator.</summary>
        public void RemoveModerator(string username)
        {
            _moderators.Remove(username.ToLowerInvariant());
        }

        /// <summary>Registriert einen benutzerdefinierten Chat-Befehl.</summary>
        public void RegisterCommand(string command, string description, Action<string[]> action, bool moderatorOnly = false, int cooldownSeconds = 0)
        {
            _commands[command.ToLowerInvariant()] = new ViewerCommand
            {
                Command = command,
                Description = description,
                Action = action,
                ModeratorOnly = moderatorOnly,
                CooldownSeconds = cooldownSeconds,
            };
        }

        /// <summary>Entfernt einen Chat-Befehl.</summary>
        public bool UnregisterCommand(string command)
        {
            return _commands.Remove(command.ToLowerInvariant());
        }

        /// <summary>Verarbeitet einen empfangenen Chat-Befehl.</summary>
        public bool ProcessChatMessage(string username, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            // Befehl erkennen (!command args...)
            var match = Regex.Match(message, @"^!(\w+)\s*(.*)$", RegexOptions.IgnoreCase);
            if (!match.Success)
                return false;

            var cmdName = match.Groups[1].Value.ToLowerInvariant();
            var args = match.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (!_commands.TryGetValue(cmdName, out var cmd))
            {
                UnknownCommand?.Invoke(this, cmdName);
                return false;
            }

            // Moderator-Check
            if (cmd.ModeratorOnly && !_moderators.Contains(username.ToLowerInvariant()))
                return false;

            // Cooldown-Check
            if (cmd.CooldownSeconds > 0)
            {
                var now = DateTime.UtcNow;
                if (_lastCommandTimes.TryGetValue(cmd.Command, out var lastTime))
                {
                    var elapsed = (now - lastTime).TotalSeconds;
                    if (elapsed < cmd.CooldownSeconds)
                        return false;
                }
                _lastCommandTimes[cmd.Command] = now;
            }

            // Befehl ausführen
            cmd.Action?.Invoke(args);

            CommandExecuted?.Invoke(this, new ViewerCommandEvent(username, cmd.Command, args, DateTime.UtcNow));
            return true;
        }

        /// <summary>Gibt alle registrierten Befehle zurück.</summary>
        public IReadOnlyDictionary<string, ViewerCommand> Commands => _commands;

        /// <summary>Gibt alle Moderatoren zurück.</summary>
        public IReadOnlySet<string> Moderators => _moderators;

        private void RegisterDefaultCommands()
        {
            RegisterCommand("profile", "Wechselt das aktive Profil (!profile <name>)",
                args => { /* Profil-Switch wird vom Profil-Manager behandelt */ },
                moderatorOnly: true,
                cooldownSeconds: 5);

            RegisterCommand("overlay", "Schaltet das Overlay ein/aus (!overlay on|off)",
                args => { /* Overlay-Toggle wird vom Overlay-Manager behandelt */ },
                moderatorOnly: false,
                cooldownSeconds: 3);

            RegisterCommand("emote", "Führt ein Emote aus (!emote <name>)",
                args => { /* Emote-Ausgabe über Overlay */ },
                moderatorOnly: false,
                cooldownSeconds: 2);

            RegisterCommand("sound", "Spielt einen Sound ab (!sound <name>)",
                args => { /* Sound über Audio-Engine */ },
                moderatorOnly: false,
                cooldownSeconds: 3);

            RegisterCommand("command", "Listet alle verfügbaren Befehle auf",
                args => { /* Befehlsliste im Chat anzeigen */ },
                moderatorOnly: false,
                cooldownSeconds: 10);
        }
    }

    /// <summary>
    /// Repräsentiert einen registrierten Chat-Befehl.
    /// </summary>
    public class ViewerCommand
    {
        public string Command { get; set; } = "";
        public string Description { get; set; } = "";
        public Action<string[]>? Action { get; set; }
        public bool ModeratorOnly { get; set; }
        public int CooldownSeconds { get; set; }
    }

    /// <summary>
    /// Event-Daten für ausgeführte Chat-Befehle.
    /// </summary>
    public record ViewerCommandEvent(string Username, string Command, string[] Args, DateTime Timestamp);
}
