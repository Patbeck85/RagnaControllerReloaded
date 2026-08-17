using System;
using System.Collections.Generic;
using RagnaController.Profiles;
using RagnaController.Models; // WICHTIG: Verweis auf den Models-Ordner
using static RagnaController.Core.NativeMethods; // For GetCursorPos and POINT

namespace RagnaController.Core
{
    public class CombatEngine
    {
        private readonly Dictionary<string, TurboState> _turboStates = new();
        private readonly Dictionary<string, MacroRecorder> _macroPlayers = new();
        private readonly Dictionary<string, Macro?> _macroCache = new();
        private Profile? _profile;
        private string _prefix = "";

        // NEW: Self-Cast dependencies
        private readonly WindowTracker _tracker;
        private readonly InputCommandQueue _queue;

        // FIX: Strict LIFO Input Buffer - prevents panic button-mashing from firing old inputs
        private ButtonAction? _bufferedAction;
        private string? _bufferedKey;
        private const int INPUT_BUFFER_WINDOW_MS = 100;

        // FIX: Release-to-Cast State Tracking for Ground Spells (AoE)
        private string _activeGroundLayer = "";
        private string _activeBaseGroundButton = "";

        public bool IsAimingGroundSpell => 
            !string.IsNullOrEmpty(_activeGroundLayer) || !string.IsNullOrEmpty(_activeBaseGroundButton);

        public event Action<ButtonAction>? ActionFired;

        // NEW: Haptic Metronome Event
        public event Action? TurboPulsed;

        /// <summary>Physische Ausführung der konfigurierten Aktion.</summary>
        private void ExecuteAction(ButtonAction action)
        {
            switch (action.Type)
            {
                case ActionType.Key:
                    _queue.TapKey(action.Key);
                    break;
                case ActionType.LeftClick:
                    _queue.LeftClick();
                    break;
                case ActionType.RightClick:
                    _queue.RightClick();
                    break;
                case ActionType.Scroll:
                    _queue.ScrollWheel(action.ScrollDelta != 0 ? action.ScrollDelta : 120);
                    break;
                case ActionType.SwitchWindow:
                    // FIX: Fire-and-forget für asynchrone Window-Switching (125Hz Tick-Loop bleibt blockfrei)
                    _ = Task.Run(async () => await WindowSwitcher.ToggleAsync(action.WindowTarget));
                    break;
                // ActionType.Combo und RoFeature werden von ComboEngine / RoUiMenuService behandelt
            }
        }

        // NEW: Execute Self-Cast sequence (snap to center, fire skill, snap back)
        private void ExecuteSelfCast(ButtonAction action)
        {
            if (action.Key == VirtualKey.None) return;

            // Save current cursor position (using Win32 API natively)
            if (NativeMethods.GetCursorPos(out NativeMethods.POINT savedPos))
            {
                // FIX: Safe access to tracker properties with null protection
                int centerX = _tracker?.CenterX ?? 0;
                int centerY = _tracker?.CenterY ?? 0;

                // Push the sequence into the queue
                // 1. Move to center of RO client
                _queue.MouseMoveAbsolute(centerX, centerY);
                _queue.Wait(10); // Let game register mouse movement

                // 2. Fire the hotkey
                _queue.KeyDown(action.Key);
                _queue.Wait(15);
                _queue.KeyUp(action.Key);
                _queue.Wait(15); // Wait for targeting circle to appear

                // 3. Left click to cast
                _queue.LeftClick();
                _queue.Wait(15);

                // 4. Snap back to saved position
                _queue.MouseMoveAbsolute(savedPos.X, savedPos.Y);
            }
        }

        public void LoadProfile(Profile p)
        {
            _profile = p;
            _turboStates.Clear();
            _macroCache.Clear();
        }

        // NEW: Constructor with dependencies for Self-Cast support
        public CombatEngine(WindowTracker tracker, InputCommandQueue queue)
        {
            _tracker = tracker;
            _queue = queue;
        }

        public void UpdateLayers(bool l1, bool r1, bool l2, bool r2)
        {
            // FIX: Use cached strings for layer prefixes
            string newPrefixRaw = l1 ? "L1+" : r1 ? "R1+" : l2 ? "L2+" : r2 ? "R2+" : "";
            string newPrefix = EngineOptimizationPool.Instance.GetString(newPrefixRaw);

            // FIX: Release-to-Cast for Modifier Layers (L1, R1, L2, R2)
            if (!string.IsNullOrEmpty(_activeGroundLayer) && newPrefix != _activeGroundLayer)
            {
                _queue.LeftClick();
                _activeGroundLayer = ""; // Spell fired, reset state
            }

            _prefix = newPrefix;
        }

        public void ClearMacroCache() => _macroCache.Clear();

        public void ProcessButton(string btn, bool pressed, int ms)
        {
            if (_profile == null) return;
            
            // FIX: Use cached string for full key combination (e.g., "L1+A")
            string fullKey = EngineOptimizationPool.Instance.GetString(_prefix + btn);

            // Mapping suchen (mit Fallback auf Base-Layer)
            if (!_profile.ButtonMappings.TryGetValue(fullKey, out var action))
            {
                if (_prefix == "" || !_profile.ButtonMappings.TryGetValue(btn, out action)) return;
            }

            // Combo-Aktionen werden von der ComboEngine separat verarbeitet
            if (action.Type == ActionType.Combo) return;

            if (!_turboStates.TryGetValue(fullKey, out var state)) { state = new TurboState(); _turboStates[fullKey] = state; }

            if (pressed)
            {
                if (action.IsMacro && !state.WasPressed)
                {
                    ExecuteMacro(fullKey, action);
                }
                // NEW: Intercept Self-Cast (bypass aiming, snap to center)
                else if (action.IsSelfCast && !state.WasPressed)
                {
                    ActionFired?.Invoke(action);
                    ExecuteSelfCast(action);
                }
                // FIX: Enter Ground Spell Aiming State
                else if (action.IsGroundSpell)
                {
                    if (!state.WasPressed)
                    {
                        ActionFired?.Invoke(action);
                        ExecuteAction(action); // Sends the F-Key to show targeting circle in RO

                        if (!string.IsNullOrEmpty(_prefix))
                            _activeGroundLayer = _prefix; // Modifier holds the spell
                        else
                            _activeBaseGroundButton = btn; // Face button holds the spell
                    }
                }
                else if (action.TurboEnabled)
                {
                    state.HoldMs += ms;
                    if (state.HoldMs >= state.NextInterval || !state.WasPressed)
                    {
                        ActionFired?.Invoke(action);
                        ExecuteAction(action);

                        // NEW: Fire the haptic metronome event
                        TurboPulsed?.Invoke();

                        state.HoldMs = 0;
                        // Menschlicher Jitter für den Turbo-Abstand
                        state.NextInterval = 100; // FIX: JitterService ist ein Typ, nicht eine Instanz - verwenden wir konstanten Wert
                    }
                }
                else if (!state.WasPressed)
                {
                    // FIX: Strict LIFO Overwrite. The newest button press ALWAYS wins 
                    // and resets the validity window.
                    _bufferedAction = action;
                    _bufferedKey = fullKey;
                }
            }
            else
            {
                state.HoldMs = 0;

                // FIX: Release-to-Cast for Base Layer (no modifiers)
                if (action.IsGroundSpell && _activeBaseGroundButton == btn)
                {
                    _queue.LeftClick();
                    _activeBaseGroundButton = "";
                }
            }

state.WasPressed = pressed;
        }

        private void ExecuteMacro(string key, ButtonAction action)
        {
            if (!_macroPlayers.TryGetValue(key, out var player))
                _macroPlayers[key] = player = new MacroRecorder(_queue);

            // FIX: Safe null check before accessing MacroFilePath
            if (string.IsNullOrEmpty(action.MacroFilePath))
                return;

            if (!_macroCache.TryGetValue(action.MacroFilePath, out var macro))
            {
                macro = MacroRecorder.LoadMacro(action.MacroFilePath);
                _macroCache[action.MacroFilePath] = macro;
            }

            if (macro != null) player.Play(macro, macro.LoopCount);
        }

        public void UpdateMacroPlayback(int ms)
        {
            foreach (var player in _macroPlayers.Values)
                if (player.IsPlaying) player.UpdatePlayback(ms);
        }

        public bool IsComboActionHeld(GamepadButtonFlags buttons)
        {
            if (_profile == null) return false;

            // FIX: Nur Combos prüfen, deren Layer mit dem aktuellen Prefix übereinstimmt
            foreach (var mapping in _profile.ButtonMappings)
            {
                if (mapping.Value.Type != ActionType.Combo) continue;

                string key = mapping.Key;

                // Layer-Prüfung: Der Mapping-Key muss mit dem aktuellen Layer übereinstimmen
                if (!_prefix.Equals(key.Substring(0, _prefix.Length))) continue;

                // Button-Teil extrahieren (nach dem '+')
                string btnPart = key.Substring(_prefix.Length + 1);

                if (Enum.TryParse<GamepadButtonFlags>(btnPart, out var flag) && buttons.HasFlag(flag))
                    return true;
            }
            return false;
        }

        private class TurboState 
        { 
            public bool WasPressed; 
            public int HoldMs; 
            public int NextInterval = 100; 
        }

        public void StopAllActiveRoutines()
        {
            _activeGroundLayer = "";
            _activeBaseGroundButton = "";
            // ... existing macro/turbo stopping ...
            foreach (var player in _macroPlayers.Values)
                if (player.IsPlaying) player.StopPlayback();
            _turboStates.Clear();
        }
    }
}