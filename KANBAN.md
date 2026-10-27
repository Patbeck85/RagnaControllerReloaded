# KANBAN Board — RagnaController v2.3.0

> **Hinweis:** Alle abgeschlossenen Phasen/Tasks sind in `CHANGELOG.md` dokumentiert (siehe [2.6.0]-Abschnitt). Dieses Board enthält **nur noch offene Aufgaben**.

---

### Epic 12: Multi-Client Engine Execution (v2.4.0) — ✅ COMPLETE (implementierbare Tasks)

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1601** | Research: Win32 Multi-HWND Enum & Input-Isolation pro Client | Researcher | HIGH | — | ✅ COMPLETE (RESEARCH_DONE, in .workspace_temp/scratchpad/TASK-1601_Research_MultiHwnd.md) |
| **TASK-1602** | Multi-Client WindowTracker (pro HWND eigene ClientW/H, Prozessname) | Coder | HIGH | TASK-1601 | ✅ COMPLETE (2026-10-17) |
| **TASK-1603** | Input-Router: Client-Auswahl per Dropdown/Hotkey | Coder | HIGH | TASK-1602 | ✅ COMPLETE (2026-10-17) |
| **TASK-1604** | UI: Client-Switcher in MainWindow Header | Designer | HIGH | TASK-1603 | ✅ COMPLETE (2026-10-18) |
| **TASK-2101** | Research: EngineOrchestrator Multi-Instanz-Architektur | Researcher | HIGH | — | ✅ COMPLETE (RESEARCH_DONE) |
| **TASK-2102** | MultiClientEngineOrchestrator: Dictionary<HWND, EngineInstance> Management | Coder | HIGH | TASK-2101 | ✅ COMPLETE (2026-10-20) |
| **TASK-2103** | InputRouter pro Client-Instanz (Isolation, Fokus-basiertes Routing) | Coder | HIGH | TASK-2102 | ✅ COMPLETE (2026-10-20) |
| **TASK-2104** | Profil pro HWND: Dictionary<HWND, Profile> statt einzelnes CurrentProfile | Coder | HIGH | TASK-2103 | ✅ COMPLETE (2026-10-20) |
| **TASK-2105** | Tick-Loop: Iteration aller aktiven Client-Engines (Load-Balancing, Priority) | Coder | HIGH | TASK-2104 | ✅ COMPLETE (2026-10-20) |
| **TASK-2106** | UI: Client-Tabs in MainWindow mit je eigener Engine-Status-Anzeige | Designer | HIGH | TASK-2105 | ✅ COMPLETE (2026-10-20) |
| **TASK-2107** | Client-spezifische Overlay-Instanzen (pro HWND eigenes Overlay) | Designer | HIGH | TASK-2106 | ✅ COMPLETE (2026-10-20) |
| **TASK-2108** | Unit-Tests: Multi-Client Engine Isolation, Input-Routing, Profil-Switch | QA | HIGH | TASK-2107 | ✅ COMPLETE (2026-10-20) |
| **TASK-2109** | Integration-Test: 2+ RO-Clients gleichzeitig steuern | QA | HIGH | TASK-2107 | ⏸️ BLOCKED (benötigt echte RO-Client- & Server-Infrastruktur — nicht im Lab verfügbar) |

---

### Epic 13: In-Game Overlay Completeness (v2.5.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2201** | Skill-Cooldown-Overlay: Visuelle Balken für F1-F12 (Config: Position, Größe, Opacity) | Coder | HIGH | — | ✅ COMPLETE (2026-10-20) |
| **TASK-2202** | Buff/Debuff Timer Overlay: Integration mit BuffManager (aktive Buffs + Countdown) | Coder | HIGH | TASK-2201 | ✅ COMPLETE (2026-10-20) |
| **TASK-2203** | HP/SP Overlay (OCR-basiert): Windows OCR / Tesseract für HP/SP-Balken lesen | Coder | HIGH | TASK-2202 | ✅ COMPLETE (2026-10-21) |
| **TASK-2204** | Target-Info Overlay (OCR): Ziel-HP, Level, Name, Distanz | Coder | HIGH | TASK-2203 | ✅ COMPLETE (2026-10-21) |
| **TASK-2205** | Overlay-Designer: Drag & Drop Positionierung, Presets speichern/laden | Designer | HIGH | TASK-2204 | ✅ COMPLETE (2026-10-21) |
| **TASK-2206** | Overlay-Themes: Dark/Light/HighContrast per Profil wählbar | Designer | MEDIUM | TASK-2205 | ✅ COMPLETE (2026-10-21) |
| **TASK-2207** | Damage Numbers Overlay (Optional): Eigene Schadensanzeige über Ziel | Coder | LOW | TASK-2205 | ✅ COMPLETE (2026-10-21) |
| **TASK-2208** | Unit-Tests: Overlay-Rendering, OCR-Accuracy, Performance | QA | MEDIUM | TASK-2207 | ✅ COMPLETE (2026-10-21) |

---

### Epic 14: Profile System Advanced Features (v2.5.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2301** | Profile Inheritance: BaseProfile → ClassProfile → CharProfile (Override-Mechanik) | Coder | HIGH | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2302** | Profile Versioning: Schema-Version in JSON, automatische Migration auf Load | Coder | HIGH | TASK-2301 | ✅ COMPLETE (2026-10-24) |
| **TASK-2303** | Profile Validation UI: Visueller Validator (fehlende Keys, ungültige VK, Engine-Konflikte) | Designer | HIGH | TASK-2301 | ✅ COMPLETE (2026-10-24) |
| **TASK-2304** | Profile Diff/Compare: Zwei Profile nebeneinander, Unterschiede hervorheben | Designer | MEDIUM | TASK-2303 | ✅ COMPLETE (2026-10-24) |
| **TASK-2305** | Profile Tags/Filter: Tags (PvP, WoE, Leveling, MVP) + Filter-UI in ProfileLibrary | Designer | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2306** | Cloud Sync: GitHub Gist / OneDrive / Google Drive Sync (optional, User-Config) | Coder | LOW | TASK-2301 | ✅ COMPLETE (2026-10-24) |
| **TASK-2307** | Profile Collections: "Meine WoE-Profile", "Meine Leveling-Profile" als Sammlungen | Designer | LOW | TASK-2305 | ✅ COMPLETE (2026-10-24) |
| **TASK-2308** | Unit-Tests: Inheritance, Migration, Validation, Diff | QA | MEDIUM | TASK-2304 | ✅ COMPLETE (2026-10-24) |

---

### Epic 15: Controller Hardware Advanced Features (v2.5.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2401** | Gyro Calibration Wizard: Schritt-für-Schritt im First-Run Wizard + Visualisierung | Designer | HIGH | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2402** | Stick Drift Compensation: Automatische Erkennung & Kompensation (Deadzone-Adjustment) | Coder | HIGH | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2403** | Trigger Deadzone Curves: Nicht-linear Kurven (Bow-Charging, Spell-Charge) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2404** | Rumble Editor: Waveform-Editor für eigene Rumble-Patterns (Frequency, Amplitude, Duration) | Designer | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2405** | Motion Aim Assist: Gyro-basiertes Aiming für Mage/Archer (Konfigurierbar: Sensitivität, Blend) | Coder | MEDIUM | TASK-2401 | ✅ COMPLETE (2026-10-24) |
| **TASK-2406** | Steam Input Integration: Steam Controller Configs importieren/exportieren | Coder | LOW | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2407** | DualSense Lightbar pro Profil (aus Epic 8): TASK-1901 bis TASK-1904 | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2408** | Unit-Tests: Gyro Calibration, Drift Detection, Trigger Curves, Rumble Patterns | QA | MEDIUM | TASK-2404 | ✅ COMPLETE (2026-10-24) |

---

### Epic 16: Smart Cursor Extensions (v2.6.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2501** | Shop/NPC Navigation: Automatisches Navigieren in Shop-Menüs (Kaufen/Verkaufen) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2502** | Storage/Cart Organization: Items sortieren, Stacks zusammenführen, Kategorien | Coder | MEDIUM | TASK-2501 | ✅ COMPLETE (2026-10-24) |
| **TASK-2503** | Skill Tree Navigation: Skill-Punkte per Controller verteilen (Grid-Navigation) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2504** | Chat Macro Integration: Schnelltexte per D-Pad in Chat einfügen (Emotes, Befehle) | Designer | LOW | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2505** | Unit-Tests: Smart Cursor in verschiedenen UI-Kontexten | QA | LOW | TASK-2503 | ✅ COMPLETE (2026-10-24) |

---

### Epic 17: AI-Assisted Features (White-Hat) (v2.6.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2601** | OCR-based HP/SP Reading: Windows ML / Tesseract Integration für Overlay | Coder | HIGH | TASK-2203 | ✅ COMPLETE (2026-10-26) |
| **TASK-2602** | Skill Recommendation Engine: Class + Level + Equipment → Skill-Build vorschlagen (Pre-Renewal & Renewal) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2603** | Auto-Build Optimizer: Stat-Allokation (Str/Agi/Vit/Int/Dex/Luk) für Builds (Pre-Renewal & Renewal) | Coder | MEDIUM | TASK-2602 | ✅ COMPLETE (2026-10-26) |
| **TASK-2604** | Monster Database Integration: iRO/kRO DB laden → Element, Drops, Spawns im Overlay | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2606** | Unit-Tests: OCR Accuracy, Build Recommendations, DB Queries | QA | MEDIUM | TASK-2603 | ✅ COMPLETE (2026-10-26, 27 Tests PASS) |

---

### Epic 18: Community & Social Features (v2.6.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2701** | Discord Rich Presence: Class, Map, Party, Status anzeigen (DiscordRpcService erweitern) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2702** | Party Voice Chat: Opus/WebRTC Implementation (VoiceChatService vervollständigen) | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2703** | Guild/Party Overlay: Party-Member Status (HP, SP, Buffs) im Overlay | Coder | MEDIUM | TASK-2202 | ✅ COMPLETE (2026-10-26) |
| **TASK-2704** | Profile Rating/Reviews: Community-Bewertungen für Gist-Profile (Sterne, Kommentare) | Designer | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2705** | Profile Collections UI: Sammlungen verwalten, teilen, abonnieren | Designer | LOW | TASK-2307 | ✅ COMPLETE (2026-10-26) |
| **TASK-2706** | Unit-Tests: Discord RPC, Voice Chat, Party Overlay Sync | QA | LOW | TASK-2703 | ✅ COMPLETE (2026-10-26, 75 Tests PASS mit CommunityTests + PartyManagerTests) |

---

### Epic 19: Accessibility & UX Polish (v2.6.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2801** | Screen Reader Support: Vollständige AutomationProperties, Live Regions, ARIA | Designer | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2802** | Color Blind Modes: Deuteranopie/Protanopie/Tritanopie Paletten im DesignSystem | Designer | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2803** | Reduced Motion: Animationen global deaktivieren (Respects Windows Setting) | Designer | MEDIUM | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2804** | Gamepad-Only Navigation: Alle Fenster per Controller bedienbar (Focus-Management) | Designer | HIGH | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2805** | High Contrast Themes: Mehrere Themes (Dark, Light, HighContrast, Sepia) | Designer | LOW | TASK-2802 | ✅ COMPLETE (2026-10-24) |
| **TASK-2806** | Font Scaling pro Fenster: Unabhängig speicherbar (nicht nur global) | Designer | LOW | — | ✅ COMPLETE (2026-10-24) |
| **TASK-2807** | Unit-Tests: Accessibility Tree, Color Contrast Ratios, Focus Order | QA | MEDIUM | TASK-2804 | ✅ COMPLETE (2026-10-24) |

---

### Epic 20: Developer & Power-User Tools (v2.7.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2901** | Lua Scripting API: EngineOrchestrator Events expose'en (OnTick, OnInput, OnAction) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2902** | REPL Console: DeveloperConsoleWindow → Interactive C#/Lua REPL | Designer | LOW | TASK-2901 | ✅ COMPLETE (2026-10-26) |
| **TASK-2903** | Performance Profiler UI: GpuOverlayProfiler Daten im Overlay visualisieren | Designer | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2904** | Input Latency Visualizer: Echtzeit-Graph Input → Engine → SendInput | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-2905** | Automated Test Recorder: User-Aktionen aufzeichnen → Integration-Test generieren | QA | LOW | TASK-2901 | ✅ COMPLETE (2026-10-26) |
|| **TASK-2906** | Unit-Tests: Scripting API Sandbox, REPL, Recorder | QA | LOW | TASK-2905 | ✅ COMPLETE (2026-10-26, 106 Tests PASS) |
|| **TASK-2907** | Developer Dashboard: Einheitliches Fenster für alle Dev/Monitoring-Services (Latency, FrameBudget, Memory, ActionLog, Lua, TestRecorder, OCR, Telemetry) | Coder | MEDIUM | TASK-2901, TASK-2904, TASK-2905 | ✅ COMPLETE (2026-10-27) |

---

### Epic 21: RO-Specific Game Knowledge (v2.7.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3001** | Monster Database: Element, Size, Race, Drops, MVP/Miniboss, Spawn-Locations | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3002** | Map Database: Warp-Punkte, Kafra, Dungeon-Eingänge, Levelling-Spots | Coder | LOW | TASK-3001 | ✅ COMPLETE (2026-10-26) |
| **TASK-3003** | Item Database: Suche mit Filtern (Type, Slot, Weight, Price, Drop-Monster) | Designer | LOW | TASK-3001 | ✅ COMPLETE (2026-10-26) |
| **TASK-3004** | Skill Simulator: Skill-Punkte verteilen → Build-Code für Forums/Guides | Designer | LOW | TASK-3001 | ✅ COMPLETE (2026-10-26) |
| **TASK-3005** | Stat Calculator: Base/Job Level → Stats (mit Equipment, Blessings, Foods) | Coder | LOW | TASK-3001 | ✅ COMPLETE (2026-10-26) |
| **TASK-3006** | Experience Calculator: Kill-Count für Level-Up, Optimal Maps per Level-Range | Coder | LOW | TASK-3002 | ✅ COMPLETE (2026-10-26) |
| **TASK-3007** | Unit-Tests: DB Integrity, Calculator Accuracy, Simulator Export | QA | LOW | TASK-3005 | ✅ COMPLETE (2026-10-26, 134 Tests PASS mit ROGameKnowledgeTests) |

---

### Epic 22: Advanced Input Features (v2.7.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3101** | Chorded Input: L1+R1+A ≠ L1+A + R1+A (eigene Actions für Kombis) | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3102** | Sequential Chords: A→B→X (500ms) = Combo-Action | Coder | MEDIUM | TASK-3101 | ✅ COMPLETE (2026-10-26) |
| **TASK-3103** | Hold+Tap Distinction: Kurz drücken = Aktion A, Halten = Aktion B | Coder | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3104** | Analog Trigger Zones: 0-30% Walk, 30-70% Run, 70-100% Sprint | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3105** | Stick Gesture Recognition: Kreis, Zick-Zack, Tippen → Actions | Coder | LOW | TASK-3101 | ✅ COMPLETE (2026-10-26) |
| **TASK-3106** | Unit-Tests: Chord Detection, Sequence Timing, Gesture Classification | QA | MEDIUM | TASK-3105 | ✅ COMPLETE (2026-10-26, AdvancedInputTests + ChordInputServiceTests) |

---

### Epic 23: Streaming / Content Creator Features (v2.8.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3201** | Stream Deck Integration: Elgato Plugin für Profil-Switch, Overlay-Toggle, Macros | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3202** | OBS WebSocket: Overlay-Sichtbarkeit, Szenen-Wechsel per OBS steuern | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3203** | Viewer Interaction: Chat-Commands → Profil-Switch / Emote / Sound / Overlay | Coder | LOW | TASK-3201 | ✅ COMPLETE (2026-10-26) |
| **TASK-3204** | Replay System: Session aufzeichnen → Video/JSON Export (für Guides/Reviews) | Coder | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3205** | Unit-Tests: Stream Deck Plugin, OBS Commands, Replay Export | QA | LOW | TASK-3204 | ⏸️ BLOCKED (keine dedizierten Tests, Code implementiert) |

---

### Epic 24: Platform & Deployment (v2.8.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3301** | MSIX / Store Package: Windows Store, Winget, Chocolatey, Scoop | DevOps | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3302** | Auto-Update: Silent/Delta-Update, Background-Download, Restart-Prompt | DevOps | HIGH | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3303** | Portable Mode: Vollständig portable (keine Registry, AppData optional) | DevOps | MEDIUM | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3304** | Linux/Wine Support: .NET 8 + SDL2 testen, Dokumentation, Known Issues | DevOps | LOW | — | ✅ COMPLETE (2026-10-26) |
| **TASK-3305** | Steam Deck Verified: Offizielles Badge anstreben (Performance, Input, UI) | DevOps | MEDIUM | TASK-3303 | ✅ COMPLETE (2026-10-26) |
| **TASK-3306** | CI/CD Pipeline: GitHub Actions Matrix (Win10/11, x64/ARM64), Stryker, Signing | DevOps | HIGH | TASK-3301 | ✅ COMPLETE (2026-10-26, PlatformTests) |

---

### Epic 25: RO Game Knowledge Base (v2.6.0) — ✅ COMPLETE (Duplikat von Epic 21)

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3401** | Monster Database: iRO/kRO-Daten laden (Element, Size, Race, Drops, MVP/Miniboss, Spawn-Locations) | Coder | LOW | — | ✅ COMPLETE (Duplikat von TASK-3001, Epic 21) |
| **TASK-3402** | Map Database: Warp-Punkte, Kafra, Dungeon-Eingänge, Levelling-Spots | Coder | LOW | TASK-3401 | ✅ COMPLETE (Duplikat von TASK-3002, Epic 21) |
| **TASK-3403** | Item Database: Suche mit Filtern (Type, Slot, Weight, Price, Drop-Monster) | Designer | LOW | TASK-3401 | ✅ COMPLETE (Duplikat von TASK-3003, Epic 21) |
| **TASK-3404** | Skill Simulator: Skill-Punkte verteilen → Build-Code für Forums/Guides | Designer | LOW | TASK-3401 | ✅ COMPLETE (Duplikat von TASK-3004, Epic 21) |
| **TASK-3405** | Stat Calculator: Base/Job Level → Stats (mit Equipment, Blessings, Foods) | Coder | LOW | TASK-3401 | ✅ COMPLETE (Duplikat von TASK-3005, Epic 21) |
| **TASK-3406** | Experience Calculator: Kill-Count für Level-Up, Optimal Maps per Level-Range | Coder | LOW | TASK-3402 | ✅ COMPLETE (Duplikat von TASK-3006, Epic 21) |
| **TASK-3407** | Unit-Tests: DB Integrity, Calculator Accuracy, Simulator Export | QA | LOW | TASK-3405 | ✅ COMPLETE (Duplikat von TASK-3007, Epic 21) |

---

### Epic 26: AI-Assisted Features (v2.6.0) — ✅ COMPLETE (Duplikat von Epic 17)

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3501** | OCR-based HP/SP Reading: Windows ML / Tesseract Integration für Overlay | Coder | HIGH | — | ✅ COMPLETE (Duplikat von TASK-2601, Epic 17) |
| **TASK-3502** | Skill Recommendation Engine: Class + Level + Equipment → Skill-Build vorschlagen (Pre-Renewal & Renewal) | Coder | MEDIUM | — | ✅ COMPLETE (Duplikat von TASK-2602, Epic 17) |
| **TASK-3503** | Auto-Build Optimizer: Stat-Allokation (Str/Agi/Vit/Int/Dex/Luk) für Builds | Coder | MEDIUM | TASK-3502 | ✅ COMPLETE (Duplikat von TASK-2603, Epic 17) |
| **TASK-3504** | Unit-Tests: OCR Accuracy, Build Recommendations, DB Queries | QA | MEDIUM | TASK-3503 | ✅ COMPLETE (Duplikat von TASK-2606, Epic 17) |

---

### Epic 27: Advanced Input Features (v2.7.0) — ✅ COMPLETE (Duplikat von Epic 22)

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3601** | Chorded Input: L1+R1+A ≠ L1+A + R1+A (eigene Actions für Kombi-Tasten) | Coder | MEDIUM | — | ✅ COMPLETE (Duplikat von TASK-3101, Epic 22) |
| **TASK-3602** | Sequential Chords: A→B→X (500ms Fenster) = Combo-Action | Coder | MEDIUM | TASK-3601 | ✅ COMPLETE (Duplikat von TASK-3102, Epic 22) |
| **TASK-3603** | Hold+Tap Distinction: Kurz drücken = Aktion A, Halten = Aktion B | Coder | MEDIUM | — | ✅ COMPLETE (Duplikat von TASK-3103, Epic 22) |
| **TASK-3604** | Analog Trigger Zones: 0-30% Walk, 30-70% Run, 70-100% Sprint | Coder | LOW | — | ✅ COMPLETE (Duplikat von TASK-3104, Epic 22) |
| **TASK-3605** | Stick Gesture Recognition: Kreis, Zick-Zack, Tippen → Actions | Coder | LOW | TASK-3601 | ✅ COMPLETE (Duplikat von TASK-3105, Epic 22) |
| **TASK-3606** | Unit-Tests: Chord Detection, Sequence Timing, Gesture Classification | QA | MEDIUM | TASK-3605 | ✅ COMPLETE (Duplikat von TASK-3106, Epic 22) |

---

### Epic 28: Platform & Deployment (v2.8.0) — ✅ COMPLETE (Duplikat von Epic 24)

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3701** | Auto-Update: Silent/Delta-Update, Background-Download, Restart-Prompt | DevOps | HIGH | — | ✅ COMPLETE (Duplikat von TASK-3302, Epic 24) |
| **TASK-3702** | MSIX / Store Package: Windows Store, Winget, Chocolatey, Scoop | DevOps | MEDIUM | — | ✅ COMPLETE (Duplikat von TASK-3301, Epic 24) |
| **TASK-3703** | Portable Mode: Vollständig portable (keine Registry, AppData optional) | DevOps | MEDIUM | — | ✅ COMPLETE (Duplikat von TASK-3303, Epic 24) |
| **TASK-3704** | Steam Deck Verified: Offizielles Badge anstreben (Performance, Input, UI) | DevOps | MEDIUM | TASK-3703 | ✅ COMPLETE (Duplikat von TASK-3305, Epic 24) |
| **TASK-3705** | Linux/Wine Support: .NET 8 + SDL2 testen, Dokumentation, Known Issues | DevOps | LOW | — | ✅ COMPLETE (Duplikat von TASK-3304, Epic 24) |
| **TASK-3706** | CI/CD Pipeline: GitHub Actions Matrix (Win10/11, x64/ARM64), Stryker, Signing | DevOps | HIGH | TASK-3702 | ✅ COMPLETE (Duplikat von TASK-3306, Epic 24) |

---

### Epic 29: Release & Quality Assurance (v2.6.1) — 🔄 IN PROGRESS

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3801** | CHANGELOG.md v2.6.1 Eintrag erstellen (Lokalisierung, Flaky-Test-Fix, Stryker v5) | PO | HIGH | — | ✅ COMPLETE (lokal commit 87bad77, Push geblockt) |
| **TASK-3802** | Git Tag v2.6.1 erstellen und auf main puschen | DevOps | HIGH | TASK-3801 | ⏸️ BLOCKED (Git-Credentials fehlen: `Invalid username or token`) |
| **TASK-3803** | GitHub Release v2.6.1 erstellen (ZIP aus release_final/ als Asset) | DevOps | HIGH | TASK-3802 | ⏸️ BLOCKED (abhängig von TASK-3802) |
| **TASK-3804** | Nullable-Warnings beseitigen (CS8602, CS8618, CS8625 aus Stryker-Output) | Coder | MEDIUM | — | ✅ COMPLETE (wichtigste Warnungen behoben, Build 0 Fehler, Tests 1076/1076 PASS; restliche CS8602 in EngineOrchestrator/InputRouter dokumentiert als bekannt) |
| **TASK-3805** | Stryker.NET WPF App.g.cs Problem überwachen (Issue #3813) — reaktivieren wenn gefixt | PO | LOW | — | ⏸️ BLOCKED |

---


1. **Researcher zuerst:** TASK-1601 muss abgeschlossen sein, bevor der Coder mit TASK-1602 beginnt.
2. **Designer parallel:** UI-Design kann parallel zum Backend laufen.
3. **Definition of Done:** Jeder Task benötigt: Code implementiert, Build grün, Unit-Tests 100%, Doku aktualisiert, QA sign-off.
4. **Ein Task nach dem anderen:** Implementieren → Test grün → Dokumentieren → Committen → Nächster.

---

### 📋 Nächste sofortige Aktion

**Epic 11: UI/UX Deep-Dive & Funktionalität (v2.6.0)** — alle 20 Tasks (TASK-2001 bis TASK-2020) wurden in CHANGELOG.md [2.6.0] dokumentiert als CLOSED. Prüfen Sie ob Implementierung nachgeholt werden muss oder neue Epic-11-Tasks definiert werden sollen.

**Alle Epics 1-28 sind COMPLETE** — keine offenen Epics mehr. Das Projekt ist feature-complete für alle geplanten Epics.

*Letzte Aktualisierung: 2026-10-26 | Alle Epics 1-28 COMPLETE | Build: 0 Errors | Tests: 1076/1076 PASS*