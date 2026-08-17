# Contributing to RagnaController

Thank you for your interest in contributing to RagnaController! This document outlines the contribution guidelines, code style, and best practices to ensure high-quality, maintainable code.

---

## 🎯 Project Vision

RagnaController is a **premium, e-sports-ready middleware** for Ragnarok Online. We aim to provide:
- **Zero anti-cheat risk**: 100% white-hat, no memory injection, no hooking
- **High performance**: 125Hz tick loop with zero allocation in hot path
- **Community-friendly**: Easy localization, profile sharing, feature contributions

---

## 📋 Code of Conduct

### Be Respectful
- Use welcoming and inclusive language
- Accept constructive criticism gracefully
- Focus on what's best for the community

### Be Professional
- No spam, trolling, or harassment
- Report issues responsibly (include reproduction steps)
- Credit original authors when referencing external work

---

## 🛠️ Development Setup

### Prerequisites

```bash
# Install .NET 8 SDK
dotnet --version  # Should show 8.0.x

# Clone repository
git clone https://github.com/RagnaController/RagnaController.git
cd RagnaController

# Restore dependencies
dotnet restore
```

### Build & Test

```bash
# Clean build
dotnet clean
dotnet build --no-restore

# Run tests
dotnet test --no-build

# Debug build (for manual testing)
dotnet build -c Debug
```

---

## 📁 Project Structure Overview

```
src/RagnaController/
├── App.xaml / App.xaml.cs              # Entry point, DI setup
├── MainWindow.xaml / .cs               # Main UI
├── Core/                               # Business logic (engines, services)
│   ├── HybridEngine.cs                 # 125Hz tick loop orchestrator
│   ├── CombatRouter.cs                 # Input routing
│   ├── AutoTargetEngine.cs             # Melee engine
│   ├── KiteEngine.cs                   # Ranged hit-and-run FSM
│   └── ...                             # Other engines & services
├── Controller/                         # Hardware interaction
│   ├── ControllerService.cs            # Hexa.NET.SDL2 polling (Xbox/PlayStation)
│   ├── DualSenseLightbarService.cs     # PS5 LED colors via Raw HID
│   └── GyroService.cs                  # DualSense gyro reading
├── Profiles/                           # Profile management
│   ├── Profile.cs                      # Profile JSON schema
│   └── ProfileManager.cs               # Load/Save/Import/Export
├── Locales/                            # i18n JSON files
├── Assets/                            # Character sprites, icons, class images
│   ├── Classes/                        # RO class portraits
│   └── Emotes/                         # Emote images
├── AntiCheat/                         # Interception driver (optional)
├── DefaultProfiles/                    # Built-in character profiles
└── .hermes/                           # Hermes Agent configuration
```

---

## 🎨 Coding Standards

### C# 12 Features

Use modern C# 12 features where appropriate:

```csharp
// ✅ Primary constructors
public class Settings {
    public string Name { get; set; } = "";
    public int AttackIntervalMs { get; set; }
}

// ✅ Collection expressions
var buttons = new[] { 
    new Button { Id = 1, Name = "Attack" },
    new Button { Id = 2, Name = "Skill" }
};

// ✅ Pattern matching
switch (input.Type) {
    case InputType.A: HandleAButton(); break;
    case InputType.B: HandleBButton(); break;
    default: return;
}

// ✅ Null-conditional operators
string? name = settings?.Name;
```

### Zero Allocation in Hot Path

The `HybridEngine.OnTick()` runs 125 times per second. **NEVER allocate** in the tick loop:

```csharp
// ❌ WRONG - Allocates in hot path
public void OnTick() {
    var items = new List<Item>();  // BAD!
    items.Add(new Item());         // BAD!
}

// ✅ CORRECT - Use pre-allocated collections
private readonly List<Item> _itemPool = [];
public void OnTick() {
    if (_itemPool.Count < MaxItems) {
        _itemPool.Add(new Item());  // OK - pool management
    }
}
```

### Thread Safety

Engine runs on background thread; UI runs on WPF Dispatcher:

```csharp
// ❌ WRONG - Touching UI from engine thread
public void OnTick() {
    StatusText.Text = "Ready";  // BAD!
}

// ✅ CORRECT - Use Dispatcher.Invoke
public void OnTick() {
    if (StatusText != null) {
        Dispatcher.Invoke(() => {
            StatusText.Text = "Ready";  // OK - UI thread
        });
    }
}
```

### MVVM Pattern

Use `MainViewModel` for data binding in WPF:

```xaml
<!-- ✅ CORRECT - Data binding -->
<TextBlock Text="{Binding StatusText}" />

<!-- ❌ WRONG - Code-behind imperative updates -->
<TextBox Text="{Binding StatusText, Mode=TwoWay}" />
```

---

## 🧪 Testing Guidelines

### Unit Tests

Tests are located in `tests/RagnaController.Tests/`:

```csharp
// ✅ CORRECT - Deterministic testing
public class AutoTargetEngineTests {
    private readonly HybridEngine _engine = new(
        new BackgroundTickProvider(8),
        new Messenger(),
        new TestCommandQueue(),  // Mock queue
        new AdvancedLogger("test.log")
    );

    [Fact]
    public void When_TargetInRange_Should_Attack() {
        // Arrange
        _engine.SetTarget(new Target { Distance = 500 });
        
        // Act
        _engine.OnTick();
        
        // Assert
        var queue = (TestCommandQueue)_engine.CommandQueue;
        Assert.True(queue.Clicks.Any());
    }
}
```

### Test Rules

1. **NO Win32 calls** in tests (use `TestCommandQueue`)
2. **Deterministic**: Use mock services (`FakeInputService`)
3. **Fast**: Each test < 100ms
4. **Isolated**: No shared state between tests

---

## 🌍 Localization (i18n)

### Adding New Languages

1. Copy `Locales/en.json` to `Locales/yourlang.json`
2. Translate all values
3. Keep keys unchanged (e.g., `Btn_Base`, `Status_Ready`)
4. Add language option in `SettingsWindow.xaml`:

```xaml
<ComboBoxItem Content="Your Language" Tag="yourlang"/>
```

### JSON Structure

```json
{
  "Btn_Base": "BASE",
  "Btn_L1": "L1",
  "Status_Ready": "READY",
  "Info_ClassTips": "Class tips here..."
}
```

### Translation Guidelines

- **Keep keys in English** for consistency
- **Use natural phrasing** in target language
- **Test with all languages** before submitting
- **Document cultural nuances** in PR description

---

## 📝 Pull Request Process

### Before Submitting

1. **Fork** the repository
2. **Create feature branch**: `git checkout -b feature/amazing-feature`
3. **Make changes** following coding standards
4. **Run tests**: `dotnet test`
5. **Update documentation** as needed
6. **Create commit** with clear message

### Commit Message Format

```
feat(Core/HybridEngine): Add support for dual-sense gyro

- Implement raw HID reading for gyroscope
- Add low-pass filtering (cutoff: 10Hz)
- Update documentation

Refs #123
```

### PR Checklist

- [ ] All tests pass (`dotnet test`)
- [ ] No new warnings in build output
- [ ] Code follows project coding standards
- [ ] Documentation updated (README.md, comments)
- [ ] Changes are backward compatible
- [ ] Performance impact documented (if any)

---

## 🔍 Code Review Guidelines

### What We Look For

✅ **Good PRs**:
- Clear purpose in description
- Minimal diff (focused changes)
- Tests added/updated
- Documentation updated
- No breaking changes

❌ **Bad PRs**:
- Multiple unrelated features
- No tests for new functionality
- Breaking changes without migration path
- Performance regressions unexplained

### Review Process

1. **Automated checks**: CI runs build & tests
2. **Human review**: Maintainer reviews code
3. **Feedback loop**: Address comments iteratively
4. **Merge**: Squash commits, merge to main

---

## 🚀 Feature Requests

### How to Propose a Feature

1. **Check existing issues**: Maybe it's already discussed
2. **Write detailed proposal**:
   - Problem statement
   - Proposed solution
   - Alternative approaches considered
   - Performance implications
3. **Create issue**: Tag as `enhancement`
4. **Wait for discussion**: Community feedback

### Feature Acceptance Criteria

- ✅ Solves real user problem
- ✅ No anti-cheat risk
- ✅ Performance-neutral or improved
- ✅ Well-documented API
- ✅ Backward compatible

---

## 🐛 Bug Reports

### How to Report a Bug

1. **Search existing issues**: Use GitHub search
2. **Provide reproduction steps**:
   ```
   1. Connect Xbox controller
   2. Load Wizard profile
   3. Click "Play Game" button
   4. App crashes with error: [paste stack trace]
   ```
3. **Include environment info**:
   - OS version (Windows 10/11, build number)
   - Controller model
   - Ragnarok Online version
   - RagnaController version

### Bug Triage

- **P1 (Critical)**: App crashes, data loss
- **P2 (High)**: Core functionality broken
- **P3 (Medium)**: Missing features, UI issues
- **P4 (Low)**: Typos, minor improvements

---

## 📚 Documentation

### Where to Contribute

- **README.md**: Project overview, quick start
- **CONTRIBUTING.md**: This file
- **BUILD-INSTRUCTIONS.md**: Build & deployment guide
- **Inline code comments**: Explain complex logic
- **API docs**: XML documentation comments

### Writing Good Documentation

✅ **Do**:
- Use clear, concise language
- Include code examples
- Link to relevant resources
- Update when code changes

❌ **Don't**:
- Write vague statements
- Assume reader knowledge
- Leave broken links
- Forget to update examples

---

## 🎓 Learning Resources

### Project-Specific

- **AGENTS.md**: Architectural patterns & coding rules
- **Core/README.md**: Engine architecture deep dive
- **Locales/en.json**: Localization reference

### General Best Practices

- [.NET Performance Tuning](https://learn.microsoft.com/dotnet/core/performance/)
- [WPF MVVM Pattern](https://docs.microsoft.com/windows/uwp/app-model/mvvm)
- [System.Text.Json Source Generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json-source-generate-overview)

---

## 🤝 Community Guidelines

### Be Welcoming

- New contributors are encouraged
- Questions are welcome (ask in issues)
- Constructive feedback is appreciated

### Respect Boundaries

- No unsolicited code changes
- Ask before major refactoring
- Credit original authors

### Stay Focused

- On-topic discussions only
- No spam or self-promotion
- Report abuse to maintainers

---

## 📞 Contact & Support

- **GitHub Issues**: [Report bugs](https://github.com/RagnaController/RagnaController/issues)
- **Discord**: Join community server (link in releases)
- **Email**: maintainer@ragnaccontroller.dev

---

<div align="center">

**Thank you for contributing to RagnaController!**

Made with ❤️ for the Ragnarok Online community

</div>
