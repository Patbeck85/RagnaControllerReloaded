# Troubleshooting Guide for RagnaController

## Common Issues and Solutions

### Issue: Input Commands Not Being Processed

**Symptoms:**
- Commands are queued but not executed
- Gameplay feels unresponsive

**Possible Causes:**
1. Input consumption flag not set
2. Queue is full or blocked
3. Tick provider not running

**Solutions:**
```csharp
// Ensure input consumption flag is set
_inputService.SetInputConsumptionFlag(true);

// Check queue status
var queue = _commandQueue;
Console.WriteLine($"Queue size: {queue.Count}");

// Verify tick provider is running
var tickProvider = _tickProvider;
Console.WriteLine($"Tick provider active: {tickProvider.IsActive}");
```

### Issue: Memory Leaks

**Symptoms:**
- Memory usage increases over time
- GC pressure is high
- Performance degradation

**Possible Causes:**
1. Objects not being returned to pool
2. Input consumption flag not set
3. String allocations in hot paths

**Solutions:**
```csharp
// Ensure objects are returned to pool
var command = _commandPool.Get();
try
{
    // Use command
}
finally
{
    _commandPool.Return(command);
}

// Use string pool for deterministic execution
private static readonly StringPool _stringPool = new StringPool();

// Use value types to avoid GC allocations
public struct ControllerSnapshot { ... }
```

### Issue: State Machine Crashes

**Symptoms:**
- Application crashes during gameplay
- State transitions fail
- Unexpected behavior

**Possible Causes:**
1. State machine not properly initialized
2. Invalid state transitions
3. Missing state handling

**Solutions:**
```csharp
// Ensure state machine is properly initialized
var engine = new AutoTargetEngine();
engine.Initialize(movementEngine);
engine.SetInitialState(KiteState.Idle);

// Handle all state transitions
public void OnTick()
{
    switch (_currentState)
    {
        case KiteState.Idle:
            HandleIdleState();
            break;
        case KiteState.Kiting:
            HandleKitingState();
            break;
        // ... handle other states
    }
}
```

### Issue: High Latency

**Symptoms:**
- Input feels delayed
- Gameplay feels sluggish
- Frame times are inconsistent

**Possible Causes:**
1. GC pressure is high
2. Cache misses in hot paths
3. Virtual calls in critical sections

**Solutions:**
```csharp
// Use value types for performance
public struct KiteState
{
    public Vector2 Position;
    public Vector2 Target;
    public float Cooldown;
}

// Pre-allocate buffers
private readonly byte[] _buffer = new byte[4096];

// Use object pooling
private static readonly ObjectPool<InputCommand> _commandPool = 
    new ObjectPool<InputCommand>(() => new InputCommand());
```

### Issue: Profile Loading Fails

**Symptoms:**
- Profile not found
- Invalid profile format
- Missing required fields

**Possible Causes:**
1. Profile file not found
2. Invalid JSON format
3. Missing required fields

**Solutions:**
```csharp
// Ensure profile file exists and is valid JSON
var profilePath = Path.Combine(_profilesDirectory, "Swordman.json");
if (!File.Exists(profilePath))
{
    throw new FileNotFoundException($"Profile not found: {profilePath}");
}

// Validate profile format
var profileJson = File.ReadAllText(profilePath);
try
{
    var profile = JsonSerializer.Deserialize<Profile>(profileJson);
    // Validate required fields
    if (string.IsNullOrEmpty(profile.Name))
    {
        throw new ArgumentException("Profile name is required");
    }
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
}
```

### Issue: Performance Degradation

**Symptoms:**
- Frame times increase over time
- Memory usage grows
- GC pressure is high

**Possible Causes:**
1. Allocations in hot paths
2. Cache misses
3. Virtual call overhead

**Solutions:**
```csharp
// Profile before optimizing
dotnet-trace collect --process <PID> --output memory.dmp

// Analyze results
dotnet-trace analyze memory.dmp --output memory-report.html

// Use performance optimization patterns
// See PERFORMANCE.md for detailed guidance
```

### Issue: Auto-Target Not Working

**Symptoms:**
- Target not updating
- Auto-target deadzone too large
- Aim sensitivity issues

**Possible Causes:**
1. Auto-target disabled
2. Deadzone configuration incorrect
3. Aim sensitivity too low

**Solutions:**
```csharp
// Ensure auto-target is enabled
profile.AutoTargetEnabled = true;

// Configure deadzone (0.18 is recommended)
profile.AutoTargetDeadzone = 0.18;

// Set aim sensitivity (20 is recommended)
profile.AimSensitivity = 20;
```

### Issue: Kite State Not Activating

**Symptoms:**
- Kite state machine not starting
- Retreat not triggering
- Pivot not executing

**Possible Causes:**
1. Kite disabled in profile
2. Cooldown not expired
3. Invalid target position

**Solutions:**
```csharp
// Ensure kite is enabled in profile
profile.KiteEnabled = true;

// Check cooldown status
if (_kiteState.Cooldown > 0)
{
    Console.WriteLine($"Kite on cooldown: {_kiteState.Cooldown}ms");
}
else
{
    // Activate kite state
    _kiteState = KiteState.Kiting;
}
```

### Issue: Combo Execution Fails

**Symptoms:**
- Combos not executing
- Skills not chaining
- Timing issues

**Possible Causes:**
1. Combo disabled in profile
2. Skill cooldowns not tracked
3. Timing configuration incorrect

**Solutions:**
```csharp
// Ensure combo is enabled in profile
profile.ComboEnabled = true;

// Configure combo timing
profile.AttackIntervalMs = 55;
profile.AttacksPerCycle = 3;
```

### Issue: Smart Cursor Not Working

**Symptoms:**
- Cursor not snapping to target
- Smart cursor disabled
- Cursor movement issues

**Possible Causes:**
1. Smart cursor disabled in profile
2. Cursor distance configuration incorrect
3. Target lock issues

**Solutions:**
```csharp
// Ensure smart cursor is enabled in profile
profile.SmartCursorEnabled = true;

// Configure cursor distance (90 is recommended)
profile.RetreatCursorDist = 90;
```

### Issue: Jitter Not Working

**Symptoms:**
- Input feels choppy
- Jitter not smoothing input
- Timing issues

**Possible Causes:**
1. Jitter disabled in profile
2. Jitter interval configuration incorrect
3. Battery throttle interfering

**Solutions:**
```csharp
// Ensure jitter is enabled in profile
profile.JitterEnabled = true;

// Configure jitter intervals (15-30ms recommended)
profile.JitterMinMs = 15;
profile.JitterMaxMs = 30;

// Disable battery throttle if needed
profile.BatteryThrottle = false;
```

### Issue: Profile Not Loading

**Symptoms:**
- Default profile not found
- Profile loading errors
- Invalid profile format

**Possible Causes:**
1. Profile file not found
2. Invalid JSON format
3. Missing required fields

**Solutions:**
```csharp
// Ensure profile file exists
var profilePath = Path.Combine(_profilesDirectory, "Swordman.json");
if (!File.Exists(profilePath))
{
    // Create default profile
    CreateDefaultProfile("Swordman");
}

// Validate profile format
try
{
    var profileJson = File.ReadAllText(profilePath);
    var profile = JsonSerializer.Deserialize<Profile>(profileJson);
    Console.WriteLine($"Profile loaded: {profile.Name}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading profile: {ex.Message}");
}
```

### Issue: Engine Not Responding

**Symptoms:**
- Engine not processing input
- Tick provider not running
- State machine frozen

**Possible Causes:**
1. Engine not initialized
2. Tick provider stopped
3. State machine blocked

**Solutions:**
```csharp
// Ensure engine is initialized
var engine = new HybridEngine();
engine.Initialize(_inputService);
engine.SetInitialState(EngineState.Idle);

// Ensure tick provider is running
var tickProvider = _tickProvider;
if (!tickProvider.IsActive)
{
    tickProvider.Start();
}

// Check state machine status
Console.WriteLine($"Engine state: {_engineState}");
```

## Debugging Tips

### Enable Detailed Logging

```csharp
// Enable detailed logging for debugging
_logger.LogInformation("Input command processed: {Command}", command);
_logger.LogWarning("State transition: {From} -> {To}", fromState, toState);
_logger.LogError("Engine error: {Error}", error);
```

### Use Performance Counters

```bash
# Monitor allocations
perf counter /Process/\*PrivateBytes

# Monitor memory
perf counter /Performance/\*Memory\*/Pages/sec

# Monitor CPU
perf counter /Processor/_/Percentage_Interrupt_Time
```

### Profile Application

```bash
# Collect memory profile
dotnet-trace collect --process <PID> --output memory.dmp

# Analyze results
dotnet-trace analyze memory.dmp --output memory-report.html
```

## Getting Help

If you encounter issues not covered in this guide:

1. **Check the logs** - Review application logs for error messages
2. **Profile the application** - Use profiling tools to identify bottlenecks
3. **Review the code** - Check for common issues (memory leaks, state machine errors)
4. **Search existing issues** - Check GitHub issues for similar problems
5. **Create a new issue** - Provide detailed information about the issue

## Reporting Issues

When reporting an issue, please include:

- **Steps to reproduce** - Detailed steps to trigger the issue
- **Expected behavior** - What you expected to happen
- **Actual behavior** - What actually happened
- **System information** - OS, .NET version, application version
- **Logs** - Relevant log output or error messages
- **Screenshots** - Screenshots if applicable

---

*Last updated: 2026*
