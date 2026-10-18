# 📄 File Documentation Guide — RagnaController

This document defines the standard for documenting every `.cs` and `.xaml` file in the project.

---

## 1. Every `.cs` File Needs a File-Level Header

Each public class/struct/interface must have XML documentation at the top:

```csharp
/// <summary>
/// FEAT-XXX: One-line description of what this class does.
/// 
/// Detailed explanation of responsibility, usage patterns, and constraints.
/// Thread safety: Safe / Unsafe / UI-thread only.
/// 
/// Example:
/// <code>
/// var service = new ClassDetector();
/// var className = service.DetectClass(profile);
/// </code>
/// </summary>
/// <remarks>
/// Thread safety: This class is safe to call from background threads.
/// UI access must go through Dispatcher.Invoke.
/// </remarks>
/// <seealso cref="FEAT-009"/>
public sealed class ClassDetector { ... }
```

### Required Elements

| Element | When |
|---------|------|
| `<summary>` | Always — one-line purpose + details |
| `<remarks>` | When thread safety, lifecycle, or constraints apply |
| `<seealso cref="FEAT-XXX"/>` | When a feature ticket exists |
| `<seealso cref="TECH-XXX"/>` | When a technical task exists |

---

## 2. Public Member Documentation

Every public method, property, event, and field:

```csharp
/// <summary>
/// Detects the character class based on button mappings.
/// </summary>
/// <param name="profile">The profile to analyze.</param>
/// <returns>The detected class name, or "Melee" if unrecognized.</returns>
/// <exception cref="ArgumentNullException">If profile is null.</exception>
public string DetectClass(Profile profile)
```

### Required XML Tags

| Tag | Purpose |
|-----|---------|
| `<summary>` | What the member does |
| `<param name="...">` | Each parameter |
| `<returns>` | Return value (non-void methods) |
| `<exception cref="...">` | Thrown exceptions |
| `<seealso cref="..."/>` | Related members |

---

## 3. Internal/Private Members

Internal and private members should have `<summary>` only when non-obvious:

```csharp
// ✅ Obvious - no doc needed
private readonly int _maxRetries = 3;

// ✅ Non-obvious - needs doc
/// <summary>
/// Cached skill name string to prevent allocations in hot path.
/// </summary>
private readonly string _cachedSkillName;
```

---

## 4. FEAT/TECH Ticket References

Every file should reference its originating ticket:

```csharp
/// <summary>
/// FEAT-009: Weighted heuristic auto-class detection.
/// </summary>
public sealed class ClassDetector { ... }
```

Ticket prefixes:
- `FEAT-XXX` — New feature
- `TECH-XXX` — Technical task / refactor
- `BUG-XXX` — Bug fix
- `PERF-XXX` — Performance improvement
- `POLISH-XXX` — UI/UX polish

---

## 5. XAML File Documentation

XAML files need a comment at the top:

```xml
<!-- 
    FEAT-010: ProfileWizardWindow - Guided profile creation wizard.
    Steps: 1. Class selection → 2. Skill mapping → 3. Auto-detect → 4. Save
-->
<Window x:Class="RagnaController.Views.Dialogs.ProfileWizardWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        ...>
```

---

## 6. File-Level README Convention

Each major folder can have a `README.md` explaining its files:

```
Core/
├── Engines/
│   ├── README.md      # Explains each engine's role in the tick loop
│   ├── AutoTargetEngine.cs
│   └── ...
├── README.md          # Core/ folder overview
└── ...
```

Example `Core/Engines/README.md`:

```markdown
# Core/Engines

All gameplay engines run in the 125Hz tick loop coordinated by `EngineOrchestrator`.

| Engine | Responsibility | Hot Path |
|--------|---------------|----------|
| AutoTargetEngine | Melee smart-aim | Yes |
| MageEngine | Ground-spell aiming | Yes |
| ... | ... | ... |

**Rule:** Zero allocation in `OnTick()` methods.
```

---

## 7. Documentation Checklist

Before committing any file:

- [ ] `<summary>` on every public class
- [ ] `<summary>` + `<param>` + `<returns>` on every public method
- [ ] `<exception>` for thrown exceptions
- [ ] FEAT/TECH ticket reference in `<summary>` or `<seealso>`
- [ ] Thread safety noted in `<remarks>` if applicable
- [ ] XAML top comment with purpose
- [ ] Folder README updated (if new folder created)

---

## 8. Example: Complete File Header

```csharp
/// <summary>
/// FEAT-008: Active buff/debuff tracking with durations, expiration warnings, and auto-recast.
/// 
/// Tracks all active buffs and debuffs on the player character. Polls at 125Hz
/// during combat, throttles to 20Hz during idle. Expiration warnings fire
/// 500ms before a buff expires.
/// 
/// Thread safety: Safe to call from background engine thread.
/// UI updates must go through Dispatcher.Invoke.
/// </summary>
/// <remarks>
/// Zero-allocation hot path: Uses pre-allocated dictionaries, no LINQ in Update().
/// </remarks>
/// <seealso cref="FEAT-008"/>
/// <seealso cref="BuffManager"/>
public sealed class BuffTracker { ... }
```
