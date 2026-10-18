# Core/Interfaces — Core Interfaces & Contracts

All public interfaces that define contracts between layers.

## Files

| File | Responsibility |
|------|----------------|
| `IControllerProvider.cs` | Controller provider interface |
| `ICommandQueue.cs` | Command queue interface |
| `ICombatState.cs` | Combat state interface |
| `IFileSystem.cs` | File system abstraction |
| `IFeedbackProvider.cs` | Feedback provider interface |
| `IMessenger.cs` | Messenger/IPC interface |
| `INativeMethods.cs` | Native methods interface |
| `IProcessNameResolver.cs` | Process name resolver interface |
| `IProfileManager.cs` | Profile manager interface |
| `ITickProvider.cs` | Tick provider interface |

## Notes

- Interfaces decouple implementation from usage
- Enables mocking in unit tests
- All interfaces live in `RagnaController.Core` namespace
