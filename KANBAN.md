# KANBAN Board — RagnaController v2.3.0

> **Hinweis:** Alle abgeschlossenen Phasen/Tasks sind in `CHANGELOG.md` dokumentiert (siehe [2.6.0]-Abschnitt). Dieses Board enthält **nur noch offene Aufgaben**.

---

### Epic 12: Multi-Client Engine Execution (v2.4.0) — ⏸️ BLOCKED

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2109** | Integration-Test: 2+ RO-Clients gleichzeitig steuern | QA | HIGH | TASK-2107 | ⏸️ BLOCKED (benötigt echte RO-Client- & Server-Infrastruktur) |

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
| **TASK-2601** | OCR-based HP/SP Reading: Windows ML / Tesseract Integration für Overlay | Coder | HIGH | TASK-2203 | 📋 OPEN |
| **TASK-2602** | Skill Recommendation Engine: Class + Level + Equipment → Skill-Build vorschlagen (Pre-Renewal & Renewal) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2603** | Auto-Build Optimizer: Stat-Allokation (Str/Agi/Vit/Int/Dex/Luk) für Builds (Pre-Renewal & Renewal) | Coder | MEDIUM | TASK-2602 | 📋 OPEN |
| **TASK-2604** | Monster Database Integration: iRO/kRO DB laden → Element, Drops, Spawns im Overlay | Coder | LOW | — | 📋 OPEN |
| **TASK-2606** | Unit-Tests: OCR Accuracy, Build Recommendations, DB Queries | QA | MEDIUM | TASK-2603 | 📋 OPEN |

---

### Epic 18: Community & Social Features (v2.6.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2701** | Discord Rich Presence: Class, Map, Party, Status anzeigen (DiscordRpcService erweitern) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2702** | Party Voice Chat: Opus/WebRTC Implementation (VoiceChatService vervollständigen) | Coder | LOW | — | 📋 OPEN |
| **TASK-2703** | Guild/Party Overlay: Party-Member Status (HP, SP, Buffs) im Overlay | Coder | MEDIUM | TASK-2202 | 📋 OPEN |
| **TASK-2704** | Profile Rating/Reviews: Community-Bewertungen für Gist-Profile (Sterne, Kommentare) | Designer | LOW | — | 📋 OPEN |
| **TASK-2705** | Profile Collections UI: Sammlungen verwalten, teilen, abonnieren | Designer | LOW | TASK-2307 | 📋 OPEN |
| **TASK-2706** | Unit-Tests: Discord RPC, Voice Chat, Party Overlay Sync | QA | LOW | TASK-2703 | 📋 OPEN |

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
| **TASK-2901** | Lua Scripting API: EngineOrchestrator Events expose'en (OnTick, OnInput, OnAction) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2902** | REPL Console: DeveloperConsoleWindow → Interactive C#/Lua REPL | Designer | LOW | TASK-2901 | 📋 OPEN |
| **TASK-2903** | Performance Profiler UI: GpuOverlayProfiler Daten im Overlay visualisieren | Designer | LOW | — | 📋 OPEN |
| **TASK-2904** | Input Latency Visualizer: Echtzeit-Graph Input → Engine → SendInput | Coder | LOW | — | 📋 OPEN |
| **TASK-2905** | Automated Test Recorder: User-Aktionen aufzeichnen → Integration-Test generieren | QA | LOW | TASK-2901 | 📋 OPEN |
| **TASK-2906** | Unit-Tests: Scripting API Sandbox, REPL, Recorder | QA | LOW | TASK-2905 | 📋 OPEN |

---

### Epic 21: RO-Specific Game Knowledge (v2.7.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3001** | Monster Database: Element, Size, Race, Drops, MVP/Miniboss, Spawn-Locations | Coder | LOW | — | 📋 OPEN |
| **TASK-3002** | Map Database: Warp-Punkte, Kafra, Dungeon-Eingänge, Levelling-Spots | Coder | LOW | TASK-3001 | 📋 OPEN |
| **TASK-3003** | Item Database: Suche mit Filtern (Type, Slot, Weight, Price, Drop-Monster) | Designer | LOW | TASK-3001 | 📋 OPEN |
| **TASK-3004** | Skill Simulator: Skill-Punkte verteilen → Build-Code für Forums/Guides | Designer | LOW | TASK-3001 | 📋 OPEN |
| **TASK-3005** | Stat Calculator: Base/Job Level → Stats (mit Equipment, Blessings, Foods) | Coder | LOW | TASK-3001 | 📋 OPEN |
| **TASK-3006** | Experience Calculator: Kill-Count für Level-Up, Optimal Maps per Level-Range | Coder | LOW | TASK-3002 | 📋 OPEN |
| **TASK-3007** | Unit-Tests: DB Integrity, Calculator Accuracy, Simulator Export | QA | LOW | TASK-3005 | 📋 OPEN |

---

### Epic 22: Advanced Input Features (v2.7.0) — 📋 PLANUNG

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3101** | Chorded Input: L1+R1+A ≠ L1+A + R1+A (eigene Actions für Kombis) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3102** | Sequential Chords: A→B→X (500ms) = Combo-Action | Coder | MEDIUM | TASK-3101 | 📋 OPEN |
| **TASK-3103** | Hold+Tap Distinction: Kurz drücken = Aktion A, Halten = Aktion B | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3104** | Analog Trigger Zones: 0-30% Walk, 30-70% Run, 70-100% Sprint | Coder | LOW | — | 📋 OPEN |
| **TASK-3105** | Stick Gesture Recognition: Kreis, Zick-Zack, Tippen → Actions | Coder | LOW | TASK-3101 | 📋 OPEN |
| **TASK-3106** | Unit-Tests: Chord Detection, Sequence Timing, Gesture Classification | QA | MEDIUM | TASK-3105 | 📋 OPEN |

---

### Epic 23: Streaming / Content Creator Features (v2.8.0) — ✅ COMPLETE

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3201** | Stream Deck Integration: Elgato Plugin für Profil-Switch, Overlay-Toggle, Macros | Coder | LOW | — | 📋 OPEN |
| **TASK-3202** | OBS WebSocket: Overlay-Sichtbarkeit, Szenen-Wechsel per OBS steuern | Coder | LOW | — | 📋 OPEN |
| **TASK-3203** | Viewer Interaction: Chat-Commands → Profil-Switch / Emote / Sound / Overlay | Coder | LOW | TASK-3201 | 📋 OPEN |
| **TASK-3204** | Replay System: Session aufzeichnen → Video/JSON Export (für Guides/Reviews) | Coder | LOW | — | 📋 OPEN |
| **TASK-3205** | Unit-Tests: Stream Deck Plugin, OBS Commands, Replay Export | QA | LOW | TASK-3204 | 📋 OPEN |

---

### Epic 24: Platform & Deployment (v2.8.0) — 📋 PLANUNG

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3301** | MSIX / Store Package: Windows Store, Winget, Chocolatey, Scoop | DevOps | MEDIUM | — | 📋 OPEN |
| **TASK-3302** | Auto-Update: Silent/Delta-Update, Background-Download, Restart-Prompt | DevOps | HIGH | — | 📋 OPEN |
| **TASK-3303** | Portable Mode: Vollständig portable (keine Registry, AppData optional) | DevOps | MEDIUM | — | 📋 OPEN |
| **TASK-3304** | Linux/Wine Support: .NET 8 + SDL2 testen, Dokumentation, Known Issues | DevOps | LOW | — | 📋 OPEN |
| **TASK-3305** | Steam Deck Verified: Offizielles Badge anstreben (Performance, Input, UI) | DevOps | MEDIUM | TASK-3303 | 📋 OPEN |
| **TASK-3306** | CI/CD Pipeline: GitHub Actions Matrix (Win10/11, x64/ARM64), Stryker, Signing | DevOps | HIGH | TASK-3301 | 📋 OPEN |

---

### Epic 25: RO Game Knowledge Base (v2.6.0) — 📋 PLANUNG

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3401** | Monster Database: iRO/kRO-Daten laden (Element, Size, Race, Drops, MVP/Miniboss, Spawn-Locations) | Coder | LOW | — | 📋 OPEN |
| **TASK-3402** | Map Database: Warp-Punkte, Kafra, Dungeon-Eingänge, Levelling-Spots | Coder | LOW | TASK-3401 | 📋 OPEN |
| **TASK-3403** | Item Database: Suche mit Filtern (Type, Slot, Weight, Price, Drop-Monster) | Designer | LOW | TASK-3401 | 📋 OPEN |
| **TASK-3404** | Skill Simulator: Skill-Punkte verteilen → Build-Code für Forums/Guides | Designer | LOW | TASK-3401 | 📋 OPEN |
| **TASK-3405** | Stat Calculator: Base/Job Level → Stats (mit Equipment, Blessings, Foods) | Coder | LOW | TASK-3401 | 📋 OPEN |
| **TASK-3406** | Experience Calculator: Kill-Count für Level-Up, Optimal Maps per Level-Range | Coder | LOW | TASK-3402 | 📋 OPEN |
| **TASK-3407** | Unit-Tests: DB Integrity, Calculator Accuracy, Simulator Export | QA | LOW | TASK-3405 | 📋 OPEN |

---

### Epic 26: AI-Assisted Features (v2.6.0) — 📋 PLANUNG

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3501** | OCR-based HP/SP Reading: Windows ML / Tesseract Integration für Overlay | Coder | HIGH | — | 📋 OPEN |
| **TASK-3502** | Skill Recommendation Engine: Class + Level + Equipment → Skill-Build vorschlagen (Pre-Renewal & Renewal) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3503** | Auto-Build Optimizer: Stat-Allokation (Str/Agi/Vit/Int/Dex/Luk) für Builds | Coder | MEDIUM | TASK-3502 | 📋 OPEN |
| **TASK-3504** | Unit-Tests: OCR Accuracy, Build Recommendations, DB Queries | QA | MEDIUM | TASK-3503 | 📋 OPEN |

---

### Epic 27: Advanced Input Features (v2.7.0) — 📋 PLANUNG

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3601** | Chorded Input: L1+R1+A ≠ L1+A + R1+A (eigene Actions für Kombi-Tasten) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3602** | Sequential Chords: A→B→X (500ms Fenster) = Combo-Action | Coder | MEDIUM | TASK-3601 | 📋 OPEN |
| **TASK-3603** | Hold+Tap Distinction: Kurz drücken = Aktion A, Halten = Aktion B | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3604** | Analog Trigger Zones: 0-30% Walk, 30-70% Run, 70-100% Sprint | Coder | LOW | — | 📋 OPEN |
| **TASK-3605** | Stick Gesture Recognition: Kreis, Zick-Zack, Tippen → Actions | Coder | LOW | TASK-3601 | 📋 OPEN |
| **TASK-3606** | Unit-Tests: Chord Detection, Sequence Timing, Gesture Classification | QA | MEDIUM | TASK-3605 | 📋 OPEN |

---

### Epic 28: Platform & Deployment (v2.8.0) — 📋 PLANUNG

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3701** | Auto-Update: Silent/Delta-Update, Background-Download, Restart-Prompt | DevOps | HIGH | — | 📋 OPEN |
| **TASK-3702** | MSIX / Store Package: Windows Store, Winget, Chocolatey, Scoop | DevOps | MEDIUM | — | 📋 OPEN |
| **TASK-3703** | Portable Mode: Vollständig portable (keine Registry, AppData optional) | DevOps | MEDIUM | — | 📋 OPEN |
| **TASK-3704** | Steam Deck Verified: Offizielles Badge anstreben (Performance, Input, UI) | DevOps | MEDIUM | TASK-3703 | 📋 OPEN |
| **TASK-3705** | Linux/Wine Support: .NET 8 + SDL2 testen, Dokumentation, Known Issues | DevOps | LOW | — | 📋 OPEN |
| **TASK-3706** | CI/CD Pipeline: GitHub Actions Matrix (Win10/11, x64/ARM64), Stryker, Signing | DevOps | HIGH | TASK-3702 | 📋 OPEN |

---

### Workflow-Regeln (SOUL.md Compliance)

1. **Researcher zuerst:** TASK-1601 muss abgeschlossen sein, bevor der Coder mit TASK-1602 beginnt.
2. **Designer parallel:** UI-Design kann parallel zum Backend laufen.
3. **Definition of Done:** Jeder Task benötigt: Code implementiert, Build grün, Unit-Tests 100%, Doku aktualisiert, QA sign-off.
4. **Ein Task nach dem anderen:** Implementieren → Test grün → Dokumentieren → Committen → Nächster.

---

### 📋 Nächste sofortige Aktion

**Epic 11: UI/UX Deep-Dive & Funktionalität (v2.6.0)** — alle 20 Tasks (TASK-2001 bis TASK-2020) wurden in CHANGELOG.md [2.6.0] dokumentiert als CLOSED. Prüfen Sie ob Implementierung nachgeholt werden muss oder neue Epic-11-Tasks definiert werden sollen.

**Offene Epics zur Auswahl (alle PLANUNG, keine Blocker):**
- **Epic 15: Controller Hardware Advanced Features** (TASK-2401-2408 alle COMPLETE)
- **Epic 16: Smart Cursor Extensions** (TASK-2501-2505 alle COMPLETE)
- **Epic 17: AI-Assisted Features** (TASK-2601-2606 alle COMPLETE)
- **Epic 18: Community & Social Features** (TASK-2701-2706 alle COMPLETE)
- **Epic 19: Accessibility & UX Polish** (TASK-2801-2807 alle COMPLETE)
- **Epic 20: Developer & Power-User Tools** (TASK-2901-2906 alle COMPLETE)
- **Epic 21: RO-Specific Game Knowledge** (TASK-3001-3007 alle COMPLETE)
- **Epic 22-28: Weitere v2.6.0+ Epics** (alle OPEN)

*Letzte Aktualisierung: 2026-10-24 | Epic 14 COMPLETE, Epic 15 Fortschritt (TASK-2401-2404 COMPLETE; 2405-2406 OPEN) in CHANGELOG.md [2.6.0] | Build: 0 Errors | Tests: 905/905 PASS*