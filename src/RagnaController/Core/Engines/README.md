# Core/Engines — Gameplay Engines (125Hz Tick Loop)

All gameplay engines run in the 125Hz tick loop, coordinated by `EngineOrchestrator`.

## Files

| File | Responsibility | Hot Path |
|------|----------------|----------|
| `AutoTargetEngine.cs` | Melee smart-aim & auto-attack | Yes |
| `CombatEngine.cs` | Combat state machine & targeting | Yes |
| `ComboEngine.cs` | Class-aware sequential skill chains | Yes |
| `CursorEngine.cs` | Right-stick free mouse movement | Yes |
| `GroundSpellEngine.cs` | AoE ground spell management with tick events | Yes |
| `HybridEngine.cs` | Legacy hybrid engine (deprecated) | No |
| `ItemManagerEngine.cs` | Auto item usage & inventory management | Yes |
| `KiteEngine.cs` | Ranged hit-and-run FSM | Yes |
| `MageEngine.cs` | Ground-spell aiming (Stick + Gyro) | Yes |
| `MobSweepEngine.cs` | Auto TAB-cycle + attack while moving | Yes |
| `MovementEngine.cs` | Left-stick click-to-move logic | Yes |
| `SkillOrchestrator.cs` | Class-specific rotation execution | Yes |
| `SmartCursorEngine.cs` | Smart cursor assistance engine | Yes |
| `SupportEngine.cs` | Party targeting & healing cycle | Yes |

## Rules

- **Zero allocation in hot path**: No LINQ, no class allocations in `OnTick()`/`Update()` methods
- **Thread safety**: All engines run on background thread; never touch WPF UI directly
- **Tick frequency**: 125Hz (8ms per tick)
