# Core/Input — Input Processing & Routing

Handles all gamepad/keyboard/mouse input processing, routing, and strategy patterns.

## Files

| File | Responsibility |
|------|----------------|
|| `BezierMouseService.cs` | Sanfte Maus-Bewegungskurven |
|| `IInputHandler.cs` | Input-Handler-Interface |
|| `IKeyboardEmulationStrategy.cs` | Keyboard-Emulations-Strategie-Interface |
|| `IMouseEmulationStrategy.cs` | Maus-Emulations-Strategie-Interface |
|| `InputChain.cs` | Input-Verarbeitungskette |
|| `InputCommandQueue.cs` | Thread-sichere SendInput-Warteschlange |
|| `InputFailoverManager.cs` | Fallback zwischen Input-Methoden |
|| `InputLatencyTracker.cs` | Input-Latenz-Überwachung |
|| `InputReader.cs` | Normalisiert XInput-Gamepad-Daten |
|| `InputRouter.cs` | Leitet Input an korrekte Engine weiter (Prioritäts-Kette) |
|| `InterceptionKeyboardStrategy.cs` | Interception-Driver Keyboard-Strategie |
|| `JitterService.cs` | Natürliches Input-Timing-Variance (15-46ms) |
|| `KernelInputService.cs` | Kernel-Level Input-Bypass |
|| `MultiClientInputRouter.cs` | Leitet Input an spezifische Client-HWNDs weiter (Multi-Client-Support) |
|| `SendInputKeyboardStrategy.cs` | Win32 SendInput Keyboard-Strategie |
|| `SendInputMouseStrategy.cs` | Win32 SendInput Maus-Strategie |

## Architecture

- **InputReader** normalizes raw XInput into `ParsedInput`
- **InputRouter** implements the priority chain: routes input to Kite → AutoTarget → Mage → Support engines
- **Strategy pattern**: Keyboard/mouse emulation strategies are interchangeable (SendInput vs Interception)
