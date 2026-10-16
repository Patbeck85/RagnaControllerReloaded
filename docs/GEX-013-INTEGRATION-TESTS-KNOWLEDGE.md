# GEX-013 Knowledge Base — Integrationstests für Engine-Chains

> **ZWECK:** Persistierende Erkenntnis-Sammlung (Single Source of Truth). Jede Session liest DIESER FILE zuerst (`read_file`, NICHT `terminal cat`), dann arbeitet sie weiter — KEIN erneutes Scannen der Source-Dateien.
>
> **ANTI-LOOP PROTOCOL (wichtig!):**
> 1. Terminal-Outputs werden vom Kontext-Kompressor gekürzt ("1 lines output"). → Große Outputs NIEMALS direkt in den Context ziehen.
> 2. Stattdessen: `grep/sed`-Ergebnisse nach `.workspace_temp/scratchpad/gex013-*.txt` schreiben und mit `read_file` lesen (überlebt Kompression).
> 3. Neue Erkenntnisse SOFORT in §3 (API) oder §7 (Session Log) eintragen — nie nur im Context behalten.
> 4. Vor jeder neuen Lese-Runde: erst prüfen, ob die Info schon hier steht.
>
> **STATUS: ALLE VERIFIKATIONSPUNKTE (V1–V8) AUFGELÖST.** Keine offenen Punkte mehr. Nächster Schritt = §6 Schritt 3 (Testdatei schreiben).

---

## 1. Task-Definition (KANBAN.md → GEX-013)

- **Ziel:** Integrationstests für vollständige Engine-Ketten (deterministisch, headless).
- **DoD:** ≥ 50 Integrationstests | CI-Dauer < 5 min | 0 flaky.
- **Stand:** 24 Facts vorhanden:
  - `tests/RagnaController.Tests/EngineIntegrationTests.cs` → 11 Facts (323 Zeilen)
  - `tests/RagnaController.Tests/FullOverlayIntegrationTests.cs` → 13 Facts
  - → **Fehlende Menge: ≥ 26 neue Facts** (Plan §4 liefert 36 → Puffer).

## 2. Test-Konventionen (aus bestehender EngineIntegrationTests.cs + AutoTargetEngineTests.cs)

- Namespace `RagnaController.Tests`; using: `Xunit`, `System.Linq`, `RagnaController.Core`, `RagnaController.Models`.
- **Zwei bewährte Setup-Muster:**
  - **Muster A (reine Engine-Ketten, ParsedInput/Update):** Echte `InputCommandQueue` + `Queue.Start()` + Assertions über das `OnCommandEnqueued`-Event in eigene `Log`-Liste. → **DAS ist das Muster für §4** (AutoTarget/Mage/Support/Kite). Siehe `AutoTargetEngineTests.cs:TestContext`.
  - **Muster B (Orchestrator-Lifecycle):** `new InputCommandQueue()` OHNE `Start()`, nur Konstruktions-/Shutdown-Checks. Siehe `EngineIntegrationTests.cs`.
- **Determinismus:** Assertions laufen über das **synchron** feuernde `OnCommandEnqueued`-Event (bei `Enqueue()`, VOR dem Consumer-Thread). KEINE echten Timeouts/Waits → nur `deltaMs`-Injektion / Clock-Injektion (Flakiness = 0).
- `Queue.Start()` startet Consumer-Thread (echtes SendInput) — in headless CI OK, da Assertions nicht vom Consumer abhängen (nur vom Enqueue-Event). Am Testende IMMER `Queue.Stop(); Queue.Dispose()`.

### TestContext-Baustein (wiederverwendbar, aus AutoTargetEngineTests.cs:19-44)
```csharp
private sealed class TestContext : IDisposable
{
    public readonly InputCommandQueue Queue;
    public readonly List<InputCmd> Log = new();
    public TestContext()
    {
        Queue = new InputCommandQueue();
        Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
        Queue.Start();
    }
    public void Dispose() { Queue.Stop(); Queue.Dispose(); }
    // Helpers: KeyDowns(VirtualKey k) => Log.Count(c => c.Type==CmdType.KeyDown && c.Key==(ushort)k);
    //          RightClicks()         => Log.Count(c => c.Type==CmdType.RightDown);
}
```

## 3. API-Knowledge-Base (VERIFIZIERT, final — alle V-Punkte aufgelöst)

### InputCommandQueue (`Core/InputCommandQueue.cs`) — KOMPLETT
- **`public List<InputCmd> Commands { get; }`** (bounded 4096) + **`event Action<InputCmd>? OnCommandEnqueued`** → deterministisches Assertions-Ziel.
- **`enum CmdType`:** `MouseRel, LeftDown, LeftUp, RightDown, RightUp, KeyDown, KeyUp, Wheel, AtomicLeftClick, AtomicRightClick, Wait, Action, MouseAbs`.
- **`sealed class InputCmd`:** Felder `CmdType Type`, `ushort Key`, `int X` (set), `int Y`, `Action? Callback`. Ctor: `InputCmd(CmdType)`, `(CmdType, ushort key)`, `(CmdType, int x, int y)`, `(CmdType, Action?)`.
- **Methoden → CmdType-Mapping (essenziell für Assertions!):**
  - `LeftDown()`→`LeftDown`; `LeftUp()`→`LeftUp`; `RightDown()`→`RightDown`; `RightUp()`→`RightUp`.
  - `LeftClick()`→**1× `LeftDown`** (+savedClicks++); `RightClick()`→**1× `RightDown`**.
  - `MouseMoveAbsolute(x,y)` / `MouseAbs(x,y)` → **`MouseAbs`** (X=x, Y=y).
  - `TapKey(VirtualKey k)` → **3 Cmds: `KeyDown`(k) + `Wait` + `KeyUp`(k)** (+savedKeystrokes++).
  - `TapKeyWithModifier(mod,key)` → KeyDown(mod)+Wait(10)+KeyDown(key)+Wait+KeyUp(key)+Wait(10)+KeyUp(mod).
  - `Wheel(delta)`/`ScrollWheel`→`Wheel`(X=delta); `MoveMouseRelative(dx,dy)`→`MouseRel`.
- Lifecycle: `Start()`, `Stop()` (Join 1000ms), `Dispose()`. **NICHT** `Drain()` (existiert nicht — alte KB war falsch).

### ParsedInput (`Models/ParsedInput.cs`) — `readonly record struct` (unterstützt `with`)
- Felder: `float LeftX,LeftY,RightX,RightY`; `bool L1,R1,L2,R2,L3,R3,R3Prev,BtnA,BtnB,BtnX,BtnY,DPadUp,DPadDown,DPadLeft,DPadRight,Start,Back`; `float TriggerLeft,TriggerRight` (ComputedProps `LT`,`RT`); `GamepadButtonFlags RawButtons,PrevRawButtons`; `bool IsConnected`; `string ControllerName,ControllerType,ControllerGuid`.
- Helper: `JustPressed(flag)`, `JustReleased(flag)`, `With(...)`.

### AutoTargetEngine (`Core/AutoTargetEngine.cs`)
- Ctor: `AutoTargetEngine(InputCommandQueue queue)`.
- **`Handle(ParsedInput input, int deltaMs)`** (Haupt-Einstieg; bestehende Tests nutzen genau das). Intern → `Update(...)`.
- Zustände (Property `State`): `CombatState.Seeking / Engaged / Attacking / Idle`.
- **R3-Rising-Edge** (`R3 && !R3Prev`): `_queue.LeftUp()` + `_queue.RightClick()`(=RightDown) + `OnTargetLocked()` → `Engaged`.
- **Seeking:** Tab-Tap wenn `!PartyTargetingEnabled && AutoRetargetEnabled`, Cooldown `TabCycleMs`.
- **Attacking:** `TapKey((VirtualKey)AttackKey_VK)` (3 Cmds), Cooldown `AttackIntervalMs`; `Nearest`→Tab-Cycling, `Sticky`→kein Cycling.
- Properties: `PartyTargetingEnabled`, `TargetingMode`(`Nearest|Sticky`), `AutoRetargetEnabled`, `AutoAttackEnabled`(default true), `AttackKey_VK`, `AimDeadzone`, `IsTargetLocked`, `TargetDataValid`, `MaxTargetDistance`(default 15f), `State`.
- Default-State: `AutoAttackEnabled=true, AutoRetargetEnabled=true, TargetingMode=Sticky, State=Idle`.

### MageEngine (`Core/MageEngine.cs`)
- **PUBLIC Einstieg: `Handle(ParsedInput input, int deltaMs)`** (gibt bool zurück; intern → private `Update`). Ctor: `(InputCommandQueue queue, SmartCursorService smartCursorService)` — 2. Arg darf `null!` sein (SmartCursor wird null-geschützt).
- Properties: `MageEnabled`(default false!), `MageBoltKeyVK`(=F1), `MageBoltCastDelayMs`(=1200), `IsActive`(private set → via `ToggleMageMode()`), `Phase`(`MagePhase.Idle|BoltSpamming`).
- **Cast-Bedingung: `input.R2 || input.TriggerRight > 0.5f`** (Bolt-Spam, KEIN Target-Lock nötig) + `IsActive && MageEnabled`.
- Cooldown: `_castCooldown = JitterService.Apply(MageBoltCastDelayMs, 50)` → **max. Delay+50ms**. Deterministisch: deltaMs > Delay+50 garantieren Cast; für "kein 2. Cast" deltaMs < Delay−50.

### SupportEngine (`Core/SupportEngine.cs`)
- **`Handle(ParsedInput input, int deltaMs)`**; Property `IsActive`; `ToggleSupportMode()` (deaktiviert→`Phase=SupportPhase.Idle`); `Reset()`.
- **Konstanten: `TAB_COOLDOWN_MS = 300`, `HEAL_COOLDOWN_MS = 800`** (V2 ✓).

### KiteEngine (`Core/KiteEngine.cs`)
- Ctor: `(InputCommandQueue queue)` — Context `_ctx` wird intern gebaut (MouseClick→LeftClick, RightClick→RightClick, TapKey→TapKey).
- **Aktivierung: `ToggleKiteMode()`** → Idle→Locking (`PhaseChanged`). Properties (public get/set): `KiteEnabled`, `AttackKeyVK`, `AttackIntervalMs`, `AttacksPerCycle`, `RetreatDurationMs`, `RetreatCursorDist`, `PivotDurationMs`, `RelockDelayMs`, `AimDeadzone`, `AimSensitivity`.
- **`public bool Handle(ParsedInput input, int deltaMs)`** — nur wenn `KiteEnabled && IsActive`, sonst `false`. Config wird pro Tick in `_ctx` gesynced; State via `_state.Update(parsed, deltaMs, _ctx)`→next (immutable), bei Änderung `PhaseChanged`.
- **States in `Core/KiteStates.cs` (V3 ✓):** `KiteIdleState`(Label "IDLE"), `KiteLockingState`("LOCKING TARGET"), `KiteAttackingState`("ATTACKING"), `KiteRetreatingState`("RETREATING"), `KitePivotingState`("PIVOTING"), `KiteRelockingState`("RELOCKING"). Alle : `KiteStateBase : ICombatState`.

### MovementEngine (`Core/MovementEngine.cs`)
- Ctor: `(InputCommandQueue queue, WindowTracker tracker)`.
- **`SetCenter(int x, int y, int clientH, float dpiScale = 1.0f)`** → `_radiusScale = (clientH/1080f)*dpiScale`.
- **`Update(float x, float y)`:**
  - Ohne `ActionRpgMode` → `ForceStop()`.
  - `x²+y² ≤ Deadzone²` → Coast: `CoastFrames` Ticks zählen, dann `_queue.LeftUp()` + `MouseMoveAbsolute(_cx,_cy)` (Recentering).
  - Sonst: Radius=`LeashRadius*_radiusScale`, Kurve `norm^Curve`; **erstes Frame:** `MouseMoveAbsolute(tx,ty)`+`LeftDown()`; danach **nur bei Koordinaten-Änderung** senden.
  - Clamp auf `_cx ± min(radius, ClientW/2)`, `_cy ± min(radius, ClientH/2)`.
- Properties: `ActionRpgMode`, `Deadzone`, `Curve`, `CoastFrames`, `LeashRadius`.

### WindowTracker (`Core/WindowTracker.cs`) — für Movement-Tests
- Ctor: `(INativeMethods? native = null, IProcessNameResolver? processNames = null)`.
- **`ClientW`, `ClientH`, `CenterX`, `CenterY`, `DpiScale`** = private set → nur via `Refresh()` (liest `_native.GetForegroundWindow/GetClientRect/ClientToScreen/GetDpiForWindow`).
- **Für Movement-Tests:** `TestNativeMethods` (siehe §3.1) mit `GetClientRect`→RECT{0,0,W,H}, `ClientToScreen`→(cx,cy), `GetDpiForWindow`→96. Dann `tracker.Refresh()` → ClientW/H gefüllt.

### PartyManager (`Core/PartyManager.cs`)
- Ctor: `(InputCommandQueue queue, Func<long>? clock = null)`.
- **`const int MaxMembers = 12`** (V4 ✓). Properties: `MemberCount`(=1), `HealThresholdPercent`(=70), `HealIntervalMs`(=5000), `HealKeyVK`(=(VirtualKey)90), `IsRunning`.
- Methoden: `Start()`, `Stop()`, `SetMemberHp(int index, int hpPercent)`, `Reset()`, `Update()`, `Dispose()`. Event: `Action<int>? MemberHealed`.

### BuffManager (`Core/BuffManager.cs`)
- **KEINE injizierbare Clock — nutzt `DateTime.UtcNow`** (V5 ✓). → Update-Tests mit kurzen Durations + 1× Testlauf, ODER nur Register/Explicit + Registry-Logik testen.
- `RegisterBuff(name, recastKey=None, autoRecast=false)`; `RegisterBuffExplicit(name, durationSec, warningSec=10, ...)`→deterministisch. `RegisterDebuff(name, durationSec)`→`DebuffApplied`. Events: Warning-once (`WarningFired`), AutoRecast. Entry: `DateTime StartTime`.

### ItemManagerEngine (`Core/ItemManagerEngine.cs`)
- **Ctor: `(InputCommandQueue queue, Func<long>? clock = null)`** (V6 ✓). Default `_clock = Environment.TickCount64`.
- **`Update(int hpPercent, int sp, int deltaMs)`:** pro Item `Enabled`; `hp > HpThresholdPercent`→nur Intervall; `sp < MinSpRequired`→skip; Cooldown aktiv→warte; sonst **FIRE**: `_queue.TapKey(item.Key)` + `ItemFired?.Invoke(item)`.
- `ItemConfig` (Models): `Enabled`, `HpThresholdPercent`, `MinSpRequired`, `CooldownMs`, `CheckIntervalMs`, `Key`.

### CooldownManager (`Core/CooldownManager.cs`)
- Ctor: `(IMessenger messenger, IFeedbackProvider feedback)` (V7 ✓). Methoden: `RegisterAction(ButtonAction)`, `Tick()`, `ResetAll()`.

### ButtonAction (`Models/ButtonAction.cs`) — vollständig bekannt
`Type=ActionType.Key`, `Key: VirtualKey`, `Label`, `IsGroundSpell`, `GroundSpellDurationSec=10`, `GroundSpellTickIntervalMs=1000`, `GroundSpellRadius=3f`, `GroundSpellFollowsTarget`, `GroundSpellIsHealing`, `IsSelfCast`, `RoFeatureName`, `WindowTarget="ragexe"`, `ScrollDelta=120`, `TrackBuff`, `BuffDurationSec=60`, `BuffWarningSec=10`.

### IFeedbackProvider (`Core/IFeedbackProvider.cs`) — vollständig bekannt
`IDisposable`; `StopAll()`, `SetLED(byte r,g,b)`, `Tick()`, `Trigger(FeedbackType)`, `TriggerSkillFired()`, `StopRumble()`.

### TestNativeMethods (`tests/RagnaController.Tests/TestNativeMethods.cs`) — deterministischer INativeMethods-Double
- Implementiert `INativeMethods` (public class, für WindowTracker/Headless-Tests). Felder wie `_clientRect`, `_foregroundHwnd` setzbar. Perfekt für MovementEngine/WindowTracker-Integrationstests ohne echtes Win32.

## 4. Testplan — ≥ 26 neue Facts → NEUE Datei `tests/RagnaController.Tests/EngineChainIntegrationTests.cs`
(Muster A: TestContext + OnCommandEnqueued-Log; ParsedInput via `with`-Expression / Default)

1. **AutoTarget-Kette (7):** Seeking→R3-Rising-Edge→Engaged (RightDown+LeftUp in Queue); Engaged→Attacking wenn AutoAttackEnabled; Angriffs-Interval (kein Tap vor AttackIntervalMs, Tap danach); Nearest: Tab-Cycling im Attacking; Sticky: KEIN Cycling; PartyTargetingEnabled unterdrückt Cycling; Dead-Target (`TargetName=""`, `TargetDataValid=true`)→Retarget.
2. **Mage-Kette (4):** Cast bei gelocktem Ziel; Cooldown respektiert (CastCount stabil); kein Cast ohne Lock; MageEnabled=false→keine Befehle.
3. **Support-Kette (4):** Tab-Cycle mit TAB_COOLDOWN_MS=300; Heal-Key mit HEAL_COOLDOWN_MS=800; Toggle off→Idle+keine Befehle; Reset→Cooldowns 0.
4. **Kite-Kette (5):** voller Zyklen Attack→Retreat→Pivot→Relock via deltaMs; PhaseChanged feuert pro Transition; KiteEnabled=false→Handle()=false, Queue leer; RetreatDuration respektiert.
5. **Movement-Kette (6):** Walk-Start (LeftDown genau 1×); Koordinatenwechsel→neues MouseAbs; gleiche Koordinaten→KEIN Duplikat; Deadzone-Coast→LeftUp+Recentering; DPI 2.0 halbiert Radius; Clamp an Client-Hälfte.
6. **ItemManager-Kette (3):** Item-Use am Cooldown-Rand (injizierter Clock); deaktiviertes Item nie; zwei Items unabhängige Cooldowns.
7. **BuffManager (3):** RegisterBuffExplicit→Entry mit korrekter Duration; Warning feuert einmal; Debuff→Event.
8. **Orchestrator-Vollkette (4):** Profil-Anwendung→Engines aktiv auf Tick; Commands mehrerer Engines in Queue korrekt geordnet; Pause stoppt Command-Generierung; Shutdown disposed sauber.

**Summe: 36 neue Facts → 24 + 36 = 60 ≥ 50 ✓** (Puffer gegen Fehlschläge).

## 5. Offene Verifikationspunkte — **ALLE AUFGELÖST ✓**
| ID | Ergebnis |
|----|----------|
| V1 | CmdType: MouseRel,LeftDown,LeftUp,RightDown,RightUp,KeyDown,KeyUp,Wheel,AtomicLeftClick,AtomicRightClick,Wait,Action,MouseAbs. InputCmd: Type,Key,X,Y,Callback. ✓ |
| V2 | TAB_COOLDOWN_MS=300, HEAL_COOLDOWN_MS=800. ✓ |
| V3 | KiteStates: IDLE, LOCKING TARGET, ATTACKING, RETREATING, PIVOTING, RELOCKING. ✓ |
| V4 | PartyManager: MaxMembers=12, Ctor(queue, clock?), SetMemberHp/Update/Start/Stop/Reset. ✓ |
| V5 | BuffManager: KEINE Clock (DateTime.UtcNow) → nur Register/Explicit + 1× Update-Test. ✓ |
| V6 | ItemManagerEngine Ctor: `(InputCommandQueue queue, Func<long>? clock = null)`. ✓ |
| V7 | CooldownManager: Ctor(IMessenger, IFeedbackProvider), RegisterAction/Tick/ResetAll. ✓ |
| V8 | ParsedInput-Felder komplett (siehe §3). ✓ |

## 6. Ausführungs-Protokoll (Anti-Loop, strikt)
1. `read_file docs/GEX-013-INTEGRATION-TESTS-KNOWLEDGE.md` (diese Datei).
2. ~~V-Punkte verifizieren~~ — **ERLEDIGT** (§5 alle ✓). Kein erneutes Source-Scannen nötig.
3. Testdatei `EngineChainIntegrationTests.cs` **komplett** per `write_file` schreiben (einmalig, nicht stückweise) — Muster A aus §2, APIs aus §3.
4. `dotnet build tests/RagnaController.Tests` → nur Compile-Fehler fixen (kleine gezielte Greps).
5. `dotnet test --configuration Release --no-build --filter "FullyQualifiedName~EngineChainIntegrationTests"` → iterieren bis grün.
6. Fact-Zählung: `grep -c "\[Fact\]"` in EngineIntegrationTests.cs + FullOverlayIntegrationTests.cs + EngineChainIntegrationTests.cs → ≥ 50 bestätigen.
7. Volle Suite einmal laufen lassen → CI-Dauer < 5 min dokumentieren.
8. Commit + `KANBAN.md`: GEX-013 → CLOSED (mit Fact-Zahl, CI-Zeit).

## 7. Session-Log
- **2026-10-15 (Session A):** API-Recherche AutoTarget/Mage/Support/Kite/Movement/Buff/Item/Party/ButtonAction/IFeedback. KB erstellt mit offenen V-Punkten.
- **2026-10-15 (Session B — FINAL):** Alle 8 V-Punkte (V1–V8) in EINEM Rutsch verifiziert und in §3 eingetragen. Komplette InputCommandQueue-API + CmdType-Mapping + TestContext-Muster A gesichert. ParsedInput-Felder komplett. **KB ist jetzt Single Source of Truth ohne offene Punkte.** NÄCHSTER SCHRITT: §6 Schritt 3 — `EngineChainIntegrationTests.cs` schreiben (36 Facts, Muster A).
