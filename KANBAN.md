# KANBAN Board — RagnaController v2.3.0

> **Hinweis:** Alle abgeschlossenen Phasen/Tasks sind in `CHANGELOG.md` dokumentiert. Dieses Board enthält **nur noch offene Aufgaben**.

---

## 🚀 Phase 16: Multi-Client & Profil-Switching (v2.4.0) — IN PLANUNG

**Goal:** Mehrere RO-Clients gleichzeitig steuern + schneller Charakterwechsel (FEAT-015 reaktiviert, FEAT-024).

### Epic 1: Multi-Client Routing

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1601** | Research: Win32 Multi-HWND Enum & Input-Isolation pro Client | Researcher | HIGH | — | ✅ RESEARCH_DONE |
| **TASK-1602** | Multi-Client WindowTracker (pro HWND eigene ClientW/H, Prozessname) | Coder | HIGH | TASK-1601 | ✅ ERLEDIGT (2026-10-17) |
| **TASK-1603** | Input-Router: Client-Auswahl per Dropdown/Hotkey | Coder | HIGH | TASK-1602 | ✅ ERLEDIGT (2026-10-17) |
|| **TASK-1604** | UI: Client-Switcher in MainWindow Header | Designer | HIGH | TASK-1603 | ✅ CLOSED (2026-10-18) |

### Epic 2: Multi-Character Schnellwechsel (FEAT-024)

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
|| **TASK-1611** | Profil-Preset-System: Name-basierter Schnellwechsel | Coder | MEDIUM | — | ✅ CLOSED (2026-10-18) |
|| **TASK-1612** | UI: Charakter-Auswahl-Dropdown mit Klassen-Icon | Designer | MEDIUM | TASK-1611 | ✅ CLOSED (2026-10-18) |
|| **TASK-1613** | Profil-Switch isoliert: State-Reset zwischen Charakteren | Coder | HIGH | TASK-1611 | ✅ CLOSED (2026-10-18) |

### Epic 3: Quality & Backlog-Aufräumen

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
|| **TASK-1621** | Stryker-Scoping: pro-Datei Mutation-Score in CI | QA | MEDIUM | TEST-010, TEST-011 | ✅ CLOSED (2026-10-18) |
|| **TASK-1622** | Performance-Tests entflaken (flaky Timing) | QA | LOW | — | ✅ CLOSED (2026-10-18) |
|| **TASK-1623** | Accessibility: AutomationProperties + Gamepad-Fokus-Ring | Designer | LOW | UI-010 | ✅ CLOSED (2026-10-18) |

### Epic 4: UX & Onboarding (v2.5.0) — IN PLANUNG

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|------|-----------|----------------|--------|
||| **TASK-1631** | First-Run Wizard: Controller-Erkennung, RO-Pfad, erstes Profil | Designer | HIGH | — | ✅ CLOSED (2026-10-18) |
||| **TASK-1632** | High-Contrast-Modus Toggle + Schrift-Scaling (UX-017) | Designer | MEDIUM | UI-010 | ✅ CLOSED (2026-10-18) |

### Epic 5: Code Quality & Error Handling (v2.5.0) — IN PLANUNG

**Goal:** Fehlerbehandlung verbessern, fehlende UI-Einträge ergänzen, Code-Qualität sichern.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1701** | Error-Handling: Critical Exceptions via ToastService anzeigen (statt nur Debug.WriteLine) | Coder | HIGH | — | ✅ CLOSED (2026-10-18) |
| **TASK-1702** | SettingsWindow: Fehlende Settings-Einträge ergänzen (AutoRestartOnCrash, Failover-Thresholds, etc.) | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1703** | App.xaml.cs: Deutsche Fehlermeldungen auf Englisch vereinheitlichen | Coder | LOW | — | ✅ CLOSED (2026-10-18) |
| **TASK-1704** | Integration-Tests für FirstRunService Wizard-Flow | QA | MEDIUM | TASK-1631 | ✅ CLOSED (2026-10-18) |
| **TASK-1705** | HighContrastService: Unit-Test für DesignSystem-Integration | QA | LOW | TASK-1632 | ✅ CLOSED (2026-10-18) |

### Epic 6: UI/UX Polish & Accessibility (v2.5.0) — IN PLANUNG

**Goal:** Alle Fenster nach optischen Verbesserungen, Barrierefreiheit und Funktionalität durchgehen. Jedes Fenster erhält: AutomationProperties, FocusVisualStyle, konsistente Styles, englische Texte, Tooltips, Validierung, leere Zustände.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1801** | MainWindow.xaml: AutomationProperties, FocusVisualStyle, englische Texte, Tastatur-Navigation | Designer | HIGH | — | ✅ CLOSED (2026-10-18) |
| **TASK-1802** | SettingsWindow.xaml: Deutsche Texte auf English, FocusVisualStyle, Slider-Labels, AutomationProperties | Designer | HIGH | — | ✅ CLOSED (2026-10-18) |
| **TASK-1803** | SplashWindow.xaml: Keyboard-Shortcuts anzeigen, Fortschritts-Prozent, AutomationProperties | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1804** | WizardWindow.xaml: Deutsche Texte auf English, Validierung-Feedback, AutomationProperties | Designer | HIGH | TASK-1631 | ✅ CLOSED (2026-10-18) |
| **TASK-1805** | ProfileLibraryWindow.xaml: Deutsche Texte auf English, Empty-State, AutomationProperties | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1806** | DaisyWheelWindow.xaml: Tastatur-Navigation, AutomationProperties, Focus-Ring | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1807** | CommunityBrowserWindow.xaml: Loading-Spinner, Empty-State, AutomationProperties | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1808** | TutorialWindow.xaml: Tastatur-Navigation, AutomationProperties, Media-Fallback | Designer | LOW | — | ✅ CLOSED (2026-10-18) |
| **TASK-1809** | DeveloperConsoleWindow.xaml: Copy-to-Clipboard, Log-Level-Farben, AutomationProperties | Designer | LOW | — | ✅ CLOSED (2026-10-18) |
| **TASK-1810** | ProfileWizardWindow.xaml: Validierung-Visual, AutomationProperties, Step-Dot-Labels | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1811** | HandheldWindow.xaml: Tastatur-Navigation, AutomationProperties, deutsche Tooltips | Designer | HIGH | — | ✅ CLOSED (2026-10-18) |
| **TASK-1812** | InGameOverlayWindow.xaml: Lesbarkeit verbessern, Tastatur-Navigation, AutomationProperties | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1813** | MiniModeWindow.xaml: Lesbarkeit, Tastatur-Shortcuts, Emoji-Fallback | Designer | LOW | — | ✅ CLOSED (2026-10-18) |
| **TASK-1814** | RadialSetupWindow.xaml: Emote-Gallery Tastatur, AutomationProperties, Emoji-Fallback | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1815** | RadialMenuWindow.xaml: Canvas Tastatur-Navigation, AutomationProperties, Focus-Ring | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1816** | ComboEditorWindow.xaml: Tab-Button AutomationProperties, Validierung-Feedback, Focus | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |
| **TASK-1817** | ButtonRemappingWindow.xaml: Falsche Tooltips auf Reset-Buttons, Slider-Accessibility, Tastatur | Designer | HIGH | — | ✅ CLOSED (2026-10-18) |
| **TASK-1818** | Globale UI-Styles: FocusVisualStyle für alle Controls, konsistente Button-Styles | Designer | HIGH | — | ✅ CLOSED (2026-10-18) |
| **TASK-1819** | Alle XAML: Missing Tooltips ergänzen, konsistente Margins/Paddings | Designer | MEDIUM | — | ✅ CLOSED (2026-10-18) |

### Epic 7: Lokalisation & Internationalisierung (v2.5.0) — ✅ COMPLETE

**Goal:** Alle XAML-Fenster auf fehlende Lokalisation prüfen, Tooltips und Texte in en.json erfassen, alle 38 Sprachen synchronisieren.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1820** | Alle XAML scannen: Fehlende ToolTip, Content, Text Attribute identifizieren | Designer | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-1821** | en.json: 187 neue Keys hinzugefügt (Tooltips, UI-Texte, Wizard, Settings, Profile, etc.) | Designer | HIGH | TASK-1820 | ✅ CLOSED (2026-10-19) |
| **TASK-1822** | Alle 38 Sprachdateien mit en.json synchronisiert (English als Fallback) | Designer | HIGH | TASK-1821 | ✅ CLOSED (2026-10-19) |
| **TASK-1823** | Build & Tests verifiziert: 0 Errors, 0 Warnings, 706/706 PASS | QA | HIGH | TASK-1822 | ✅ CLOSED (2026-10-19) |

---

### Workflow-Regeln (SOUL.md Compliance)

1. **Researcher zuerst:** TASK-1601 muss abgeschlossen sein, bevor der Coder mit TASK-1602 beginnt.
2. **Designer parallel:** UI-Design (TASK-1604, TASK-1612, TASK-1623) kann parallel zum Backend laufen.
3. **Definition of Done:** Jeder Task benötigt: Code implementiert, Build grün, Unit-Tests 100%, Doku aktualisiert, QA sign-off.
4. **Ein Task nach dem anderen:** Implementieren → Test grün → Dokumentieren → Committen → Nächster.

---

### 📋 Nächste sofortige Aktion

**Alle Epics (1-7) abgeschlossen** — kein offener Task mehr in der KANBAN.md.

**Neue Epics für v2.6.0+:**

### Epic 8: DualSense Lightbar pro Profil konfigurierbar — 📋 PLANUNG

**Goal:** Jedes Charakter-Profil bekommt eigene Lightbar-Farbe (z.B. Rot für Mage, Blau für Archer, Grün für Support).

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1901** | Lightbar-Farb-Definition pro Profil (RGB-Werte in Profile-Model) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-1902** | UI: Lightbar-Farb-Selektor in SettingsWindow / ProfileWizard | Designer | MEDIUM | TASK-1901 | 📋 OPEN |
| **TASK-1903** | Engine: Lightbar-Farbe bei Profil-Wechsel anwenden | Coder | MEDIUM | TASK-1901 | 📋 OPEN |
| **TASK-1904** | Unit-Tests für Lightbar-Farb-Logik | QA | LOW | TASK-1903 | 📋 OPEN |

### Epic 9: First-Run Wizard erweitern (Controller-Button-Mapping, Profil-Vorlagen) — ✅ COMPLETE

**Goal:** Wizard um Controller-Button-Mapping und Profil-Vorlagen (alle 58 RO-Klassen aus DefaultProfiles/, Pre-Renewal & Renewal mit getrennten Engine-Bereichen pro Modus) erweitert.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1911** | Profil-Vorlagen aus DefaultProfiles/*.json laden (statt hardcodiert) | Coder | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-1912** | UI: Profil-Vorlagen-Auswahl im Wizard (Dropdown, Beschreibung, Skills) | Designer | HIGH | TASK-1911 | ✅ CLOSED (2026-10-19) |
| **TASK-1913** | UI: Controller-Button-Mapping im Wizard (A/B/X/Y, D-Pad, Trigger zuweisen) | Designer | HIGH | TASK-1912 | ✅ CLOSED (2026-10-19) |
| **TASK-1914** | Button-Mapping speichern und in Profile laden | Coder | HIGH | TASK-1913 | ✅ CLOSED (2026-10-19) |
| **TASK-1915** | Integration: Wizard-Fertigstellung erstellt Profil mit Vorlage + Mapping | Coder | HIGH | TASK-1914 | ✅ CLOSED (2026-10-19) |
| **TASK-1918** | ButtonMappings: L1+/R1+ kombinierte Keys parsen (VirtualKey.L1/R1 hinzugefügt) | Coder | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-1919** | MainWindow: Renewal/Pre-Renewal Toggle verdrahten (_engine.ApplyGameMode) | Coder | HIGH | TASK-1917 | ✅ CLOSED (2026-10-19) |
| **TASK-1920** | GitHub Gists: UI-Verbindung in ProfileLibraryWindow bereits vorhanden (BtnShare/BtnDownload) | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |

### Epic 10: Performance-Optimierung (Engine-Tick-Reduktion, Memory-Pooling) — 📋 PLANUNG

**Goal:** Engine-Tick optimieren, Memory-Pooling für häufige Objekte, CPU-Last senken.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1921** | Performance-Profiling: Engines mit höchsten Tick-Zeiten identifizieren | Coder | HIGH | — | 📋 OPEN |
| **TASK-1922** | Memory-Pooling für häufige Objekte (ControllerSnapshot, InputEvents) | Coder | HIGH | TASK-1921 | 📋 OPEN |
| **TASK-1923** | Engine-Tick-Reduktion: Idle-State erkennen, Tick-Frequenz senken | Coder | HIGH | TASK-1922 | 📋 OPEN |
| **TASK-1924** | Performance-Regression-Tests (CPU-Last, Memory) | QA | MEDIUM | TASK-1923 | 📋 OPEN |

---

### Epic 11: UI/UX Deep-Dive & Funktionalität (v2.6.0) — 📋 PLANUNG

**Goal:** Alle 22 XAML-Fenster nochmal detailliert durchgehen. Für jedes Fenster: optische Verbesserungen, fehlende Funktionalität, konsistente Bedienbarkeit, deutsche/englische Text-Mischung beheben, Tooltips vervollständigen, leere Zustände füllen.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2001** | MainWindow: D-Pad Center Tooltip, leere L1/R1/L2/R2 Panels füllen, Quick-Action Hotkeys anzeigen | Designer | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-2002** | SettingsWindow: Verbleibende deutsche Texte übersetzen (High Contrast, Gyro, etc.), Suchfunktion für Einstellungen, "Alle auf Standard zurücksetzen" | Designer | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-2003** | SplashWindow: Dynamische Prozent-Anzeige, Fehler-Details anzeigen, Skip-Button prominenter | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2004** | WizardWindow: Schritt 3 visueller Fortschritt bei Controller-Erkennung, Controller-Typ Auswahl, Schritt 4 D-Pad/Trigger Button-Mapping | Designer | HIGH | TASK-1911 | ✅ CLOSED (2026-10-19) |
| **TASK-2005** | ProfileWizardWindow: Checkboxen mit Beschreibung, Vorschau vor Speichern, "Vorlage laden" Option | Designer | MEDIUM | TASK-1911 | ✅ CLOSED (2026-10-19) |
| **TASK-2006** | ProfileLibraryWindow: Bulk-Operationen, Sortierung (Name/Datum/Downloads), Favoriten, "Einfügen aus Zwischenablage" für Share Code, Download-Fortschritt | Designer | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-2007** | HandheldWindow: Mehr Tiles (Controller Test, Macro Editor), Tiles umordnen möglich | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2008** | InGameOverlayWindow: Größe anpassbar, Position speichern, Batterie dynamisch | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2009** | MiniModeWindow: Tastatur-Kurzbefehle anzeigen, Transparenz-Einstellung, Profil-Wechsel direkt | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2010** | ComboEditorWindow: Drag-and-Drop für Schritte-Reihenfolge, Kopieren/Einfügen von Schritten, Vorschau | Designer | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-2011** | ButtonRemappingWindow: Duplikate entfernen (rechts/links identisch), falsche Reset-Button-Tooltips fixen, Preview-Bereich entfernen oder sinnvoll nutzen, Profil laden/speichern | Designer | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-2012** | RadialMenuWindow: Visuelle Rückmeldung bei Auswahl, Animation beim Drehen | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2013** | RadialSetupWindow: "Alle auswählen" für Emotes, Download-Fortschrittsbalken, Emote-Kategorien | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2014** | DaisyWheelWindow: Aktuelle Auswahl anzeigen, Rad-Items konfigurierbar | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2015** | CommunityBrowserWindow: Sortierung (Name/Datum/Downloads), Kategorien-Filter, Favoriten-Funktion | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2016** | DeveloperConsoleWindow: "Kopieren" Button für Logs, Log-Level-Filter, Zeitstempel anzeigen | Designer | LOW | — | ✅ CLOSED (2026-10-19) |
| **TASK-2017** | TutorialWindow: MediaPreview mit Inhalt füllen, "Alle Tutorials überspringen" Option, Fortschrittsanzeige | Designer | LOW | — | ✅ CLOSED (2026-10-19) |
| **TASK-2018** | ControllerTestWindow: Deutsch-Englisch Mix beheben, Tastatur-Kurzbefehle anzeigen, Resize möglich | Designer | MEDIUM | — | ✅ CLOSED (2026-10-19) |
| **TASK-2019** | Controls/ControllerVisualization: Interaktive Rückmeldung bei Button-Druck, Hover-Effekte | Designer | LOW | — | ✅ CLOSED (2026-10-19) |
| **TASK-2020** | Controls/TelemetryPanel: Export-Funktion für Telemetriedaten, Auto-Scroll Toggle, Daten-Reset | Designer | LOW | — | ✅ CLOSED (2026-10-19) |

---

**Start mit TASK-2001 (MainWindow)** als nächste Priorität.

---

### Epic 12: Multi-Client Engine Execution (v2.4.0) — 📋 PLANUNG

**Goal:** Echte Multi-Boxing-Unterstützung — pro RO-Client eigene Engine-Instanz mit isoliertem Profil, Input-Routing & Tick-Loop.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2101** | Research: EngineOrchestrator Multi-Instanz-Architektur (Thread-Safety, Resource-Sharing) | Researcher | HIGH | — | ✅ RESEARCH_DONE |
| **TASK-2102** | MultiClientEngineOrchestrator: Dictionary<HWND, EngineInstance> Management | Coder | HIGH | TASK-2101 | ✅ CLOSED (2026-10-20) |
| **TASK-2103** | InputRouter pro Client-Instanz (Isolation, Fokus-basiertes Routing) | Coder | HIGH | TASK-2102 | ✅ CLOSED (2026-10-20) |
| **TASK-2104** | Profil pro HWND: Dictionary<HWND, Profile> statt einzelnes CurrentProfile | Coder | HIGH | TASK-2102 | ✅ CLOSED (2026-10-20) — via EngineInstance.Activate(profile) |
| **TASK-2105** | Tick-Loop: Iteration aller aktiven Client-Engines (Load-Balancing, Priority) | Coder | HIGH | TASK-2102 | ✅ CLOSED (2026-10-20) — each EngineInstance has own BackgroundTickProvider |
|| **TASK-2106** | UI: Client-Tabs in MainWindow mit je eigener Engine-Status-Anzeige | Designer | HIGH | TASK-2103 | ✅ CLOSED (2026-10-20) |
| **TASK-2107** | Client-spezifische Overlay-Instanzen (pro HWND eigenes Overlay) | Coder | MEDIUM | TASK-2105 | ✅ CLOSED (2026-10-20) |
| **TASK-2108** | Unit-Tests: Multi-Client Engine Isolation, Input-Routing, Profil-Switch | QA | MEDIUM | TASK-2105 | ✅ CLOSED (2026-10-20) |
| **TASK-2109** | Integration-Test: 2+ RO-Clients gleichzeitig steuern | QA | HIGH | TASK-2107 | ⏸️ BLOCKED (benötigt echte RO-Client- & Server-Infrastruktur) |

---

### Epic 13: In-Game Overlay Completeness (v2.5.0) — 📋 PLANUNG

**Goal:** Alle wichtigen Spielinformationen direkt im Overlay — Cooldowns, Buffs, HP/SP, Target-Info.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2201** | Skill-Cooldown-Overlay: Visuelle Balken für F1-F12 (Config: Position, Größe, Opacity) | Coder | HIGH | — | ✅ CLOSED (2026-10-20) |
| **TASK-2202** | Buff/Debuff Timer Overlay: Integration mit BuffManager (aktive Buffs + Countdown) | Coder | HIGH | — | ✅ CLOSED (2026-10-20) |
| **TASK-2203** | HP/SP Overlay (OCR-basiert): Windows OCR / Tesseract für HP/SP-Balken lesen | Coder | HIGH | — | ✅ CLOSED (2026-10-21) |
| **TASK-2204** | Target-Info Overlay (OCR): Ziel-HP, Level, Name, Distanz | Coder | MEDIUM | TASK-2203 | ✅ CLOSED (2026-10-21) |
| **TASK-2205** | Overlay-Designer: Drag & Drop Positionierung, Presets speichern/laden | Designer | HIGH | TASK-2201 | 📋 OPEN |
| **TASK-2206** | Overlay-Themes: Dark/Light/HighContrast per Profil wählbar | Designer | MEDIUM | TASK-2205 | 📋 OPEN |
| **TASK-2207** | Damage Numbers Overlay (Optional): Eigene Schadensanzeige über Ziel | Coder | LOW | TASK-2204 | 📋 OPEN |
| **TASK-2208** | Unit-Tests: Overlay-Rendering, OCR-Accuracy, Performance | QA | MEDIUM | TASK-2205 | 📋 OPEN |

---

### Epic 14: Profile System Advanced Features (v2.5.0) — 📋 PLANUNG

**Goal:** Profile verwalten wie Profis — Vererbung, Versionierung, Migration, Diff, Tags.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2301** | Profile Inheritance: BaseProfile → ClassProfile → CharProfile (Override-Mechanik) | Coder | HIGH | — | 📋 OPEN |
| **TASK-2302** | Profile Versioning: Schema-Version in JSON, automatische Migration auf Load | Coder | HIGH | TASK-2301 | 📋 OPEN |
| **TASK-2303** | Profile Validation UI: Visueller Validator (fehlende Keys, ungültige VK, Engine-Konflikte) | Designer | HIGH | TASK-2301 | 📋 OPEN |
| **TASK-2304** | Profile Diff/Compare: Zwei Profile nebeneinander, Unterschiede hervorheben | Designer | MEDIUM | TASK-2303 | 📋 OPEN |
| **TASK-2305** | Profile Tags/Filter: Tags (PvP, WoE, Leveling, MVP) + Filter-UI in ProfileLibrary | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-2306** | Cloud Sync: GitHub Gist / OneDrive / Google Drive Sync (optional, User-Config) | Coder | LOW | TASK-2301 | 📋 OPEN |
| **TASK-2307** | Profile Collections: "Meine WoE-Profile", "Meine Leveling-Profile" als Sammlungen | Designer | LOW | TASK-2305 | 📋 OPEN |
| **TASK-2308** | Unit-Tests: Inheritance, Migration, Validation, Diff | QA | MEDIUM | TASK-2304 | 📋 OPEN |

---

### Epic 15: Controller Hardware Advanced Features (v2.5.0) — 📋 PLANUNG

**Goal:** Controller-Hardware voll ausnutzen — Gyro, Trigger, Rumble, Drift, Lightbar.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2401** | Gyro Calibration Wizard: Schritt-für-Schritt im First-Run Wizard + Visualisierung | Designer | HIGH | — | 📋 OPEN |
| **TASK-2402** | Stick Drift Compensation: Automatische Erkennung & Kompensation (Deadzone-Adjustment) | Coder | HIGH | — | 📋 OPEN |
| **TASK-2403** | Trigger Deadzone Curves: Nicht-linear Kurven (Bow-Charging, Spell-Charge) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2404** | Rumble Editor: Waveform-Editor für eigene Rumble-Patterns (Frequency, Amplitude, Duration) | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-2405** | Motion Aim Assist: Gyro-basiertes Aiming für Mage/Archer (Konfigurierbar: Sensitivität, Blend) | Coder | MEDIUM | TASK-2401 | 📋 OPEN |
| **TASK-2406** | Steam Input Integration: Steam Controller Configs importieren/exportieren | Coder | LOW | — | 📋 OPEN |
| **TASK-2407** | DualSense Lightbar pro Profil (aus Epic 8): TASK-1901 bis TASK-1904 | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2408** | Unit-Tests: Gyro Calibration, Drift Detection, Trigger Curves, Rumble Patterns | QA | MEDIUM | TASK-2404 | 📋 OPEN |

---

### Epic 16: Smart Cursor Extensions (v2.6.0) — 📋 PLANUNG

**Goal:** Smart Cursor für alle UI-Interaktionen — Shop, Storage, Skill Tree, Chat.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2501** | Shop/NPC Navigation: Automatisches Navigieren in Shop-Menüs (Kaufen/Verkaufen) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2502** | Storage/Cart Organization: Items sortieren, Stacks zusammenführen, Kategorien | Coder | MEDIUM | TASK-2501 | 📋 OPEN |
| **TASK-2503** | Skill Tree Navigation: Skill-Punkte per Controller verteilen (Grid-Navigation) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2504** | Chat Macro Integration: Schnelltexte per D-Pad in Chat einfügen (Emotes, Befehle) | Designer | LOW | — | 📋 OPEN |
| **TASK-2505** | Unit-Tests: Smart Cursor in verschiedenen UI-Kontexten | QA | LOW | TASK-2503 | 📋 OPEN |

---

### Epic 17: AI-Assisted Features (White-Hat) (v2.6.0) — 📋 PLANUNG

**Goal:** KI-Unterstützung rein client-seitig — OCR, Build-Optimizer, Monster-DB.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2601** | OCR-based HP/SP Reading: Windows ML / Tesseract Integration für Overlay | Coder | HIGH | TASK-2203 | 📋 OPEN |
| **TASK-2602** | Skill Recommendation Engine: Class + Level + Equipment → Skill-Build vorschlagen | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2603** | Auto-Build Optimizer: Stat-Allokation (Str/Agi/Vit/Int/Dex/Luk) für Builds | Coder | MEDIUM | TASK-2602 | 📋 OPEN |
| **TASK-2604** | Monster Database Integration: iRO/kRO DB laden → Element, Drops, Spawns im Overlay | Coder | LOW | — | 📋 OPEN |
| **TASK-2605** | Market Price Overlay: Item-Preise aus öffentlichen RO-Market-APIs | Coder | LOW | TASK-2604 | 📋 OPEN |
| **TASK-2606** | Unit-Tests: OCR Accuracy, Build Recommendations, DB Queries | QA | MEDIUM | TASK-2603 | 📋 OPEN |

---

### Epic 18: Community & Social Features (v2.6.0) — 📋 PLANUNG

**Goal:** Community-Verbindung stärken — Discord, Party, Guild, Profile-Ratings.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2701** | Discord Rich Presence: Class, Map, Party, Status anzeigen (DiscordRpcService erweitern) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2702** | Party Voice Chat: Opus/WebRTC Implementation (VoiceChatService vervollständigen) | Coder | LOW | — | 📋 OPEN |
| **TASK-2703** | Guild/Party Overlay: Party-Member Status (HP, SP, Buffs) im Overlay | Coder | MEDIUM | TASK-2202 | 📋 OPEN |
| **TASK-2704** | Profile Rating/Reviews: Community-Bewertungen für Gist-Profile (Sterne, Kommentare) | Designer | LOW | — | 📋 OPEN |
| **TASK-2705** | Profile Collections UI: Sammlungen verwalten, teilen, abonnieren | Designer | LOW | TASK-2307 | 📋 OPEN |
| **TASK-2706** | Unit-Tests: Discord RPC, Voice Chat, Party Overlay Sync | QA | LOW | TASK-2703 | 📋 OPEN |

---

### Epic 19: Accessibility & UX Polish (v2.6.0) — 📋 PLANUNG

**Goal:** Barrierefreiheit auf AAA-Niveau — Screenreader, Color Blind, Reduced Motion, Gamepad-Only.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2801** | Screen Reader Support: Vollständige AutomationProperties, Live Regions, ARIA | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-2802** | Color Blind Modes: Deuteranopie/Protanopie/Tritanopie Paletten im DesignSystem | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-2803** | Reduced Motion: Animationen global deaktivieren (Respects Windows Setting) | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-2804** | Gamepad-Only Navigation: Alle Fenster per Controller bedienbar (Focus-Management) | Designer | HIGH | — | 📋 OPEN |
| **TASK-2805** | High Contrast Themes: Mehrere Themes (Dark, Light, HighContrast, Sepia) | Designer | LOW | TASK-2802 | 📋 OPEN |
| **TASK-2806** | Font Scaling pro Fenster: Unabhängig speicherbar (nicht nur global) | Designer | LOW | — | 📋 OPEN |
| **TASK-2807** | Unit-Tests: Accessibility Tree, Color Contrast Ratios, Focus Order | QA | MEDIUM | TASK-2804 | 📋 OPEN |

---

### Epic 20: Developer & Power-User Tools (v2.7.0) — 📋 PLANUNG

**Goal:** Tools für Power-User & Entwickler — Scripting, REPL, Profiling, Latency.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-2901** | Lua Scripting API: EngineOrchestrator Events expose'en (OnTick, OnInput, OnAction) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-2902** | REPL Console: DeveloperConsoleWindow → Interactive C#/Lua REPL | Designer | LOW | TASK-2901 | 📋 OPEN |
| **TASK-2903** | Performance Profiler UI: GpuOverlayProfiler Daten im Overlay visualisieren | Designer | LOW | — | 📋 OPEN |
| **TASK-2904** | Input Latency Visualizer: Echtzeit-Graph Input → Engine → SendInput | Coder | LOW | — | 📋 OPEN |
| **TASK-2905** | Automated Test Recorder: User-Aktionen aufzeichnen → Integration-Test generieren | QA | LOW | TASK-2901 | 📋 OPEN |
| **TASK-2906** | Unit-Tests: Scripting API Sandbox, REPL, Recorder | QA | LOW | TASK-2905 | 📋 OPEN |

---

### Epic 21: RO-Specific Game Knowledge (v2.7.0) — 📋 PLANUNG

**Goal:** Integriertes RO-Wissen — Monster, Maps, Items, Skills, Stats, XP.

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

**Goal:** Next-Level Input — Chords, Sequences, Hold/Tap, Analog Zones, Gestures.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3101** | Chorded Input: L1+R1+A ≠ L1+A + R1+A (eigene Actions für Kombis) | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3102** | Sequential Chords: A→B→X (500ms) = Combo-Action | Coder | MEDIUM | TASK-3101 | 📋 OPEN |
| **TASK-3103** | Hold+Tap Distinction: Kurz drücken = Aktion A, Halten = Aktion B | Coder | MEDIUM | — | 📋 OPEN |
| **TASK-3104** | Analog Trigger Zones: 0-30% Walk, 30-70% Run, 70-100% Sprint | Coder | LOW | — | 📋 OPEN |
| **TASK-3105** | Stick Gesture Recognition: Kreis, Zick-Zack, Tippen → Actions | Coder | LOW | TASK-3101 | 📋 OPEN |
| **TASK-3106** | Unit-Tests: Chord Detection, Sequence Timing, Gesture Classification | QA | MEDIUM | TASK-3105 | 📋 OPEN |

---

### Epic 23: Streaming / Content Creator Features (v2.8.0) — 📋 PLANUNG

**Goal:** Streamer-Tools — Stream Deck, OBS, Viewer Interaction, Replay.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3201** | Stream Deck Integration: Elgato Plugin für Profil-Switch, Overlay-Toggle, Macros | Coder | LOW | — | 📋 OPEN |
| **TASK-3202** | OBS WebSocket: Overlay-Sichtbarkeit, Szenen-Wechsel per OBS steuern | Coder | LOW | — | 📋 OPEN |
| **TASK-3203** | Viewer Interaction: Chat-Commands → Profil-Switch / Emote / Sound / Overlay | Coder | LOW | TASK-3201 | 📋 OPEN |
| **TASK-3204** | Replay System: Session aufzeichnen → Video/JSON Export (für Guides/Reviews) | Coder | LOW | — | 📋 OPEN |
| **TASK-3205** | Unit-Tests: Stream Deck Plugin, OBS Commands, Replay Export | QA | LOW | TASK-3204 | 📋 OPEN |

---

### Epic 24: Platform & Deployment (v2.8.0) — 📋 PLANUNG

**Goal:** Professionelle Distribution — MSIX, Auto-Update, Portable, Steam Deck, Linux.

| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-3301** | MSIX / Store Package: Windows Store, Winget, Chocolatey, Scoop | DevOps | MEDIUM | — | 📋 OPEN |
| **TASK-3302** | Auto-Update: Silent/Delta-Update, Background-Download, Restart-Prompt | DevOps | HIGH | — | 📋 OPEN |
| **TASK-3303** | Portable Mode: Vollständig portable (keine Registry, AppData optional) | DevOps | MEDIUM | — | 📋 OPEN |
| **TASK-3304** | Linux/Wine Support: .NET 8 + SDL2 testen, Dokumentation, Known Issues | DevOps | LOW | — | 📋 OPEN |
| **TASK-3305** | Steam Deck Verified: Offizielles Badge anstreben (Performance, Input, UI) | DevOps | MEDIUM | TASK-3303 | 📋 OPEN |
| **TASK-3306** | CI/CD Pipeline: GitHub Actions Matrix (Win10/11, x64/ARM64), Stryker, Signing | DevOps | HIGH | TASK-3301 | 📋 OPEN |

---

*Letzte Aktualisierung: 2026-10-20 | Epics 12-24 hinzugefügt basierend auf FEATURE_GAP_ANALYSIS.md | Build: 0 Errors / 0 Warnings | Tests: 726/726 PASS*
