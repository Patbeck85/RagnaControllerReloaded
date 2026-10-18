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
| **TASK-1703** | App.xaml.cs: Deutsche Fehlermeldungen auf Englisch vereinheitlichen | Coder | LOW | — | 📋 OPEN |
| **TASK-1704** | Integration-Tests für FirstRunService Wizard-Flow | QA | MEDIUM | TASK-1631 | 📋 OPEN |
| **TASK-1705** | HighContrastService: Unit-Test für DesignSystem-Integration | QA | LOW | TASK-1632 | 📋 OPEN |

---

### Workflow-Regeln (SOUL.md Compliance)

1. **Researcher zuerst:** TASK-1601 muss abgeschlossen sein, bevor der Coder mit TASK-1602 beginnt.
2. **Designer parallel:** UI-Design (TASK-1604, TASK-1612, TASK-1623) kann parallel zum Backend laufen.
3. **Definition of Done:** Jeder Task benötigt: Code implementiert, Build grün, Unit-Tests 100%, Doku aktualisiert, QA sign-off.
4. **Ein Task nach dem anderen:** Implementieren → Test grün → Dokumentieren → Committen → Nächster.

---

### 📋 Nächste sofortige Aktion

**TASK-1604: UI: Client-Switcher in MainWindow Header**

Der `MultiClientInputRouter` (TASK-1603) ist implementiert und sendet Input gezielt an bestimmte Client-HWNDs per PostMessage. Jetzt muss das UI in MainWindow ergänzt werden: ein Dropdown/Hotkey zur Auswahl des Ziel-Clients, der `SetPreferredClientHwnd` auf dem `MultiClientInputRouter` aufruft.

---

*Letzte Aktualisierung: 2026-10-17 | Alle Phasen 1-15 COMPLETE | Build: 0 Errors / 0 Warnings | Tests: 650/650 PASS*
