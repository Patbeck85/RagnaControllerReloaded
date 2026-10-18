# Core/Input — Input Processing & Routing

Handles all gamepad/keyboard/mouse input processing, routing, and strategy patterns.

## Files

| File | Responsibility |
|------|----------------|
| `InputReader.cs` | Normalizes XInput gamepad data |
| `InputRouter.cs` | Routes input to correct engine (Priority-Chain) |
| `InputCommandQueue.cs` | Thread-safe SendInput queue |
| `InputChain.cs` | Input processing chain |
| `InputFailoverManager.cs` | Fallback between input methods |
| `InputLatencyTracker.cs` | Input latency monitoring |
| `JitterService.cs` | Natural input timing variance (15-46ms) |
| `KeyboardInputService.cs` | Keyboard input handling |
| `MouseInputService.cs` | Mouse input handling |
| `InterceptionKeyboardStrategy.cs` | Interception driver keyboard strategy |
| `SendInputKeyboardStrategy.cs` | Win32 SendInput keyboard strategy |
| `SendInputMouseStrategy.cs` | Win32 SendInput mouse strategy |
| `BezierMouseService.cs` | Smooth mouse movement curves |
| `KernelInputService.cs` | Kernel-level input bypass |
| `IInputHandler.cs` | Input handler interface |
| `IKeyboardEmulationStrategy.cs` | Keyboard emulation strategy interface |
| `IMouseEmulationStrategy.cs` | Mouse emulation strategy interface |

## Architecture

- **InputReader** normalizes raw XInput into `ParsedInput`
- **InputRouter** implements the priority chain: routes input to Kite → AutoTarget → Mage → Support engines
- **Strategy pattern**: Keyboard/mouse emulation strategies are interchangeable (SendInput vs Interception)
