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

### Epic 9: First-Run Wizard erweitern (Controller-Button-Mapping, Profil-Vorlagen) — 📋 PLANUNG

**Goal:** Wizard um Controller-Button-Mapping und Profil-Vorlagen (NOVICE, SWORDMAN, MAGER, ARCHER, ACOLYTE, MERCHANT, THIEF) erweitern.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1911** | Profil-Vorlagen-Definitionen (Default-Buttons, Engines, Klassen) erstellen | Coder | HIGH | — | ✅ CLOSED (2026-10-19) |
| **TASK-1912** | UI: Profil-Vorlagen-Auswahl im Wizard (Schritt: Klasse + Vorlage) | Designer | HIGH | TASK-1911 | ✅ CLOSED (2026-10-19) |
| **TASK-1913** | UI: Controller-Button-Mapping im Wizard (A/B/X/Y, D-Pad, Trigger zuweisen) | Designer | HIGH | TASK-1912 | ✅ CLOSED (2026-10-19) |
| **TASK-1914** | Button-Mapping speichern und in Profile laden | Coder | HIGH | TASK-1913 | ✅ CLOSED (2026-10-19) |
| **TASK-1915** | Integration: Wizard-Fertigstellung erstellt Profil mit Vorlage + Mapping | Coder | HIGH | TASK-1914 | ✅ CLOSED (2026-10-19) |
| **TASK-1916** | Unit-Tests für Profil-Vorlagen und Button-Mapping | QA | MEDIUM | TASK-1915 | 📋 OPEN |

### Epic 10: Performance-Optimierung (Engine-Tick-Reduktion, Memory-Pooling) — 📋 PLANUNG

**Goal:** Engine-Tick optimieren, Memory-Pooling für häufige Objekte, CPU-Last senken.

|| Task ID | Titel | Verantwortlich | Priorität | Abhängigkeiten | Status ||
|---------|-------|----------------|-----------|----------------|--------|
| **TASK-1921** | Performance-Profiling: Engines mit höchsten Tick-Zeiten identifizieren | Coder | HIGH | — | 📋 OPEN |
| **TASK-1922** | Memory-Pooling für häufige Objekte (ControllerSnapshot, InputEvents) | Coder | HIGH | TASK-1921 | 📋 OPEN |
| **TASK-1923** | Engine-Tick-Reduktion: Idle-State erkennen, Tick-Frequenz senken | Coder | HIGH | TASK-1922 | 📋 OPEN |
| **TASK-1924** | Performance-Regression-Tests (CPU-Last, Memory) | QA | MEDIUM | TASK-1923 | 📋 OPEN |

---

**Start mit Epic 9 (First-Run Wizard erweitern)** als erste Priorität.

---

*Letzte Aktualisierung: 2026-10-17 | Alle Phasen 1-15 COMPLETE | Build: 0 Errors / 0 Warnings | Tests: 650/650 PASS*
