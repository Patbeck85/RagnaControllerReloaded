# 🎮 Xbox Controller nicht gefunden - Lösung

## Problem
RagnaController verwendet **SDL** für die Controller-Erkennung, aber Standard-Xbox-Controller werden manchmal nicht erkannt.

## Lösungen

### 1. Controller neu verbinden
- Controller abstecken und wieder anstecken
- Windows Geräte-Manager öffnen → Gamecontroller überprüfen

### 2. DS4Windows verwenden (Empfohlen)
RagnaController benötigt einen **XInput-kompatiblen Controller**. DS4Windows emuliert DualShock/DualSense-Controller als Xbox-Controller.

**Schritte:**
1. [DS4Windows herunterladen](https://ds4windows.com/)
2. Installieren und ausführen
3. Controller verbinden - DS4Windows zeigt "XInput" im Status
4. RagnaController sollte den Controller jetzt erkennen

### 3. Alternative: Xbox Wireless Adapter
- Offizieller Xbox Wireless Adapter für PC verwenden
- Bessere Erkennung als USB-Bluetooth-Verbindung

### 4. Bluetooth-Verbindung prüfen
- In Windows: Einstellungen → Bluetooth & Geräte
- Controller sollte als "Gamepad" oder "Xbox Controller" erscheinen
- Nicht als "HID Device" (das funktioniert nicht)

### 5. Treiber aktualisieren
- Geräte-Manager öffnen
- Gamecontroller → Rechtsklick auf Controller → Treiber aktualisieren
- Standard-SQL Treiber verwenden

## Technische Details

RagnaController verwendet:
- **SDL2** für Joystick-Erkennung
- **SDL_INIT_GAMECONTROLLER** für Hot-plug-Ereignisse
- **SDL.GameControllerOpen()** zum Öffnen des Controllers

Der Controller muss als **Gamepad** erkannt werden, nicht nur als Joystick.

## Testen

Um zu überprüfen, ob der Controller erkannt wird:
1. RagnaController starten
2. Auf "Scan for controller" klicken (Stift-Icon in der Header-Leiste)
3. Status sollte sich von "No Controller" zu "Xbox Controller" ändern

## Hinweis für PlayStation-Nutzer

PlayStation-Controller (DualShock/DualSense) werden **nicht** nativ unterstützt, da RagnaController XInput verwendet. Verwenden Sie DS4Windows, um einen Xbox-kompatiblen Controller zu emulieren.
