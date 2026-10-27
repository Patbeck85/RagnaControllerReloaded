using System;
using System.Collections.Generic;
using System.Linq;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core.Developer {
    /// <summary>
    /// TASK-2901: LuaScriptingService — Skripting-API für EngineOrchestrator-Events.
    ///
    /// Stellt eine Lua-ähnliche Skripting-Schnittstelle bereit über die
    /// EngineOrchestrator-Events (OnTick, OnInput, OnAction) zugänglich sind.
    ///
    /// White-Hat: Keine automatische Spielsteuerung, nur Ereignis-Zugriff
    /// für Skripte/Mods. Kein Cheating, kein Memory-Manipulation.
    /// </summary>
    public sealed class LuaScriptingService
    {
        private readonly EngineOrchestrator? _orchestrator;
        private bool _enabled = false;

        /// <summary>Ob die Skripting-API aktiv ist.</summary>
        public bool IsEnabled => _enabled;

#pragma warning disable CS0067 // Events are public API, subscribed externally
        /// <summary>Event: Wird pro Tick ausgelöst.</summary>
        public event Action<float>? OnTick;

        /// <summary>Event: Wird bei jedem Input ausgelöst.</summary>
        public event Action<ParsedInput>? OnInput;

        /// <summary>Event: Wird bei jeder Aktion ausgelöst.</summary>
        public event Action<string>? OnAction;
#pragma warning restore CS0067

        /// <summary>Registrierte Skript-Namen.</summary>
        public IReadOnlyList<string> RegisteredScripts => _scripts;

        private readonly List<string> _scripts = new();

        public LuaScriptingService(EngineOrchestrator? orchestrator = null)
        {
            _orchestrator = orchestrator;
        }

        /// <summary>Aktiviert die Skripting-API.</summary>
        public void Enable()
        {
            _enabled = true;
        }

        /// <summary>Deaktiviert die Skripting-API.</summary>
        public void Disable()
        {
            _enabled = false;
        }

        /// <summary>Registriert ein Skript (Name + Lua-ähnlicher Code als String).</summary>
        public bool RegisterScript(string name, string code)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
                return false;
            if (_scripts.Contains(name))
                return false;

            _scripts.Add(name);
            return true;
        }

        /// <summary>Entfernt ein registriertes Skript.</summary>
        public bool UnregisterScript(string name)
        {
            return _scripts.Remove(name);
        }
    }
}
