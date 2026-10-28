# Core/Input — Input Processing & Routing

Handles all gamepad/keyboard/mouse input processing, routing, and strategy patterns.

## Files

| File | Responsibility |
|------|----------------|
| `BezierMouseService.cs` | Smooth mouse movement curves |
| `IInputHandler.cs` | Input handler interface |
| `IKeyboardEmulationStrategy.cs` | Keyboard emulation strategy interface |
| `IMouseEmulationStrategy.cs` | Mouse emulation strategy interface |
| `InputChain.cs` | Input processing chain |
| `InputCommandQueue.cs` | Thread-safe SendInput queue |
| `InputFailoverManager.cs` | Fallback between input methods |
| `InputLatencyTracker.cs` | Input latency monitoring |
| `InputReader.cs` | Normalizes XInput gamepad data |
| `InputRouter.cs` | Routes input to correct engine (priority chain) |
| `InterceptionKeyboardStrategy.cs` | Interception driver keyboard strategy |
| `JitterService.cs` | Natural input timing variance (15-46ms) |
| `KernelInputService.cs` | Kernel-level input bypass |
| `MultiClientInputRouter.cs` | Routes input to specific client HWNDs (multi-client support) |
| `SendInputKeyboardStrategy.cs` | Win32 SendInput keyboard strategy |
| `SendInputMouseStrategy.cs` | Win32 SendInput mouse strategy |

## Architecture

- **InputReader** normalizes raw XInput into `ParsedInput`
- **InputRouter** implements the priority chain: routes input to Kite → AutoTarget → Mage → Support engines
- **Strategy pattern**: keyboard/mouse emulation strategies are interchangeable (SendInput vs Interception)
