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

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status |
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1801** | MainWindow.xaml: AutomationProperties, FocusVisualStyle, englische Texte, Tastatur-Navigation | Designer | HIGH | — | 📋 OPEN |
| **TASK-1802** | SettingsWindow.xaml: Deutsche Texte auf English, FocusVisualStyle, Slider-Labels, AutomationProperties | Designer | HIGH | — | 📋 OPEN |
| **TASK-1803** | SplashWindow.xaml: Keyboard-Shortcuts anzeigen, Fortschritts-Prozent, AutomationProperties | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1804** | WizardWindow.xaml: Deutsche Texte auf English, Validierung-Feedback, AutomationProperties | Designer | HIGH | TASK-1631 | 📋 OPEN |
| **TASK-1805** | ProfileLibraryWindow.xaml: Deutsche Texte auf English, Empty-State, AutomationProperties | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1806** | DaisyWheelWindow.xaml: Tastatur-Navigation, AutomationProperties, Focus-Ring | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1807** | CommunityBrowserWindow.xaml: Loading-Spinner, Empty-State, AutomationProperties | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1808** | TutorialWindow.xaml: Tastatur-Navigation, AutomationProperties, Media-Fallback | Designer | LOW | — | 📋 OPEN |
| **TASK-1809** | DeveloperConsoleWindow.xaml: Copy-to-Clipboard, Log-Level-Farben, AutomationProperties | Designer | LOW | — | 📋 OPEN |
| **TASK-1810** | ProfileWizardWindow.xaml: Validierung-Visual, AutomationProperties, Step-Dot-Labels | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1811** | HandheldWindow.xaml: Tastatur-Navigation, AutomationProperties, deutsche Tooltips | Designer | HIGH | — | 📋 OPEN |
| **TASK-1812** | InGameOverlayWindow.xaml: Lesbarkeit verbessern, Tastatur-Navigation, AutomationProperties | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1813** | MiniModeWindow.xaml: Lesbarkeit, Tastatur-Shortcuts, Emoji-Fallback | Designer | LOW | — | 📋 OPEN |
| **TASK-1814** | RadialSetupWindow.xaml: Emote-Gallery Tastatur, AutomationProperties, Emoji-Fallback | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1815** | RadialMenuWindow.xaml: Canvas Tastatur-Navigation, AutomationProperties, Focus-Ring | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1816** | ComboEditorWindow.xaml: Tab-Button AutomationProperties, Validierung-Feedback, Focus | Designer | MEDIUM | — | 📋 OPEN |
| **TASK-1817** | ButtonRemappingWindow.xaml: Falsche Tooltips auf Reset-Buttons, Slider-Accessibility, Tastatur | Designer | HIGH | — | 📋 OPEN |
| **TASK-1818** | Globale UI-Styles: FocusVisualStyle für alle Controls, konsistente Button-Styles | Designer | HIGH | — | 📋 OPEN |
| **TASK-1819** | Alle XAML: Missing Tooltips ergänzen, konsistente Margins/Paddings | Designer | MEDIUM | — | 📋 OPEN |

---

### Workflow-Regeln (SOUL.md Compliance)

1. **Researcher zuerst:** TASK-1601 muss abgeschlossen sein, bevor der Coder mit TASK-1602 beginnt.
2. **Designer parallel:** UI-Design (TASK-1604, TASK-1612, TASK-1623) kann parallel zum Backend laufen.
3. **Definition of Done:** Jeder Task benötigt: Code implementiert, Build grün, Unit-Tests 100%, Doku aktualisiert, QA sign-off.
4. **Ein Task nach dem anderen:** Implementieren → Test grün → Dokumentieren → Committen → Nächster.

---

### 📋 Nächste sofortige Aktion

**Alle Epics abgeschlossen** — kein offener Task mehr in der KANBAN.md.

**Vorschläge für neue Epics (v2.5.0+):**
- Epic 6: DualSense Lightbar pro Profil konfigurierbar
- Epic 7: First-Run Wizard erweitern (Controller-Button-Mapping, Profil-Vorlagen)
- Epic 8: Performance-Optimierung (Engine-Tick-Reduktion, Memory-Pooling)

---

*Letzte Aktualisierung: 2026-10-17 | Alle Phasen 1-15 COMPLETE | Build: 0 Errors / 0 Warnings | Tests: 650/650 PASS*
