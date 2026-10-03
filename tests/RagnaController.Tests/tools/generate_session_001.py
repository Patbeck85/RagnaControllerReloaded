#!/usr/bin/env python3
"""TEST-016 Fixture-Generator (deterministisch, hardware-frei).

Erzeugt:
  fixtures/controller_session_001.json       - aufgezeichnete Session (720 Frames)
  fixtures/controller_session_001.golden.json - unabhaengiger Golden-Master

Das Session-Format ist EXAKT das Format eines Recordings aus dem
ControllerTestWindow (TEST-016): Buttons = GamepadButtonFlags-Bitmasks,
Analogwerte roh durchgereicht (KEIN Deadzone/Normalisierung - das ist
Aufgabe der Backend-Layer InputReader/XInputFallbackService).

Golden-Semantik repliziert die geteilte PipelineStep / Core/InputReader.Read():
  justPressed(f)   = raw.HasFlag(f) && !prev.HasFlag(f)
  justReleased(f) = !raw.HasFlag(f) && prev.HasFlag(f)
  prev := raw nach jedem verbundenen Frame; bei Disconnect: prev := None
  l2 = lt > 0.15, r2 = rt > 0.15 (analoge Trigger, NICHT in der Button-Mask)
Analogwerte: bit-exakter Durchlauf - dieselbe Dezimalzeichenfolge in beiden Dateien.

Alle Analogwerte liegen auf dem 1/16-Raster (exakt darstellbar im Binärfloat),
damit float-VerGLEICHE in C# (AssertF, ==) deterministisch sind. Die
Schwelle 0.15 wird nur von Werten >= 0.1875 ueberschritten (Margin >= 0.03125,
kein float/double-Rundungsrisiko).

Wiedergabe:  python tests/RagnaController.Tests/tools/generate_session_001.py
"""
import json
import os

# GamepadButtonFlags (src/RagnaController/Models/ParsedInput.cs)
BtnA, BtnB, BtnX, BtnY = 1 << 0, 1 << 1, 1 << 2, 1 << 3
L1, R1 = 1 << 4, 1 << 5
L3, R3 = 1 << 8, 1 << 9
DPadUp, DPadDown, DPadLeft, DPadRight = 1 << 10, 1 << 11, 1 << 12, 1 << 13
Start, Back = 1 << 14, 1 << 15

# Digitale Buttons, die JustPressed/JustReleased feuern koennen (L2/R2 sind analog).
NAMES = {
    BtnA: "BtnA", BtnB: "BtnB", BtnX: "BtnX", BtnY: "BtnY",
    L1: "L1", R1: "R1", L3: "L3", R3: "R3",
    DPadUp: "DPadUp", DPadDown: "DPadDown", DPadLeft: "DPadLeft", DPadRight: "DPadRight",
    Start: "Start", Back: "Back",
}

N = 720  # >= 500 Frames (DoD TEST-016)


def q(x: float) -> float:
    """Quantisierung auf das 1/16-Raster (binär exakt darstellbar)."""
    return round(x * 16.0) / 16.0


raw = [0] * N
lx = [0.0] * N
ly = [0.0] * N
rx = [0.0] * N
ry = [0.0] * N
lt = [0.0] * N
rt = [0.0] * N
conn = [True] * N


def hold(a: int, b: int, mask: int) -> None:
    """Inklusive Range a..b mit exakt dieser Maske (ueberschreibt)."""
    for i in range(a, b + 1):
        raw[i] = mask


# Phase 1 (0..59):   Idle.
# Phase 2 (60..119): A gedrueckt/gehalten, Release bei 120.
hold(60, 119, BtnA)

# Phase 3 (120..179): Stick-Sweep - Dreieckswelle lx in [-0.5, 0.5] auf 1/16-Raster.
for i in range(120, 180):
    t = (i - 120) / 59.0
    tri = abs(2.0 * t - 1.0)          # 1 -> 0 -> 1
    lx[i] = q(tri - 0.5)

# Phase 4 (180..239): L2-Rampe lt 0 -> 1 (Schwelle 0.15 ueberschritten ab lt=0.1875), dann halten.
for i in range(180, 240):
    lt[i] = q(min(1.0, (i - 180) / 15.0))

# Phase 5 (240..299): B+X-Combo: B ab 240, X ab 250, X-Release 280, B-Release 300.
hold(240, 249, BtnB)
hold(250, 279, BtnB | BtnX)
hold(280, 299, BtnB)

# Phase 6 (300..359): DPad-Sequenz Up/Down/Left/Right.
hold(300, 314, DPadUp)
hold(315, 329, DPadDown)
hold(330, 344, DPadLeft)
hold(345, 359, DPadRight)

# Phase 7 (360..379): Start ab 360, Back ab 365, Release 380.
hold(360, 364, Start)
hold(365, 379, Start | Back)

# Phase 8 (380..404): L1+BtnY gehalten -> Disconnect 405..409 -> Reconnect 410.
# WICHTIG fuer den Ghost-Release-Test: Buttons beim Disconnect gehalten (raw != 0),
# damit OHNE Prev-Reset ein Ghost-Release auf dem Reconnect-Frame feuern wuerde.
hold(380, 404, L1 | BtnY)
for i in range(405, 410):
    conn[i] = False
hold(410, 419, L1 | BtnY)  # Reconnect: Hardware haelt weiter, Release bei 420.

# Phase 9 (420..539): R1 gehalten + rt-Rampe 0 -> 1 (Schwelle ab rt=0.1875), Release 540.
hold(420, 539, R1)
for i in range(420, 540):
    rt[i] = q(min(1.0, (i - 420) / 15.0))

# Phase 10 (540..584): L3/R3-Stick-Clicks, dann beide zusammen.
hold(540, 554, L3)
hold(555, 569, R3)
hold(570, 584, L3 | R3)

# Phase 11 (585..719): Idle-Tail.

# ---------------------------------------------------------------- Golden-Master
golden_frames = []
prev = 0
for i in range(N):
    if not conn[i]:
        prev = 0  # Disconnect-Reset: kein Ghost auf Reconnect
        golden_frames.append({
            "index": i, "connected": False,
            "justPressed": [], "justReleased": [],
            "l2": False, "r2": False,
            "leftX": 0.0, "leftY": 0.0, "rightX": 0.0, "rightY": 0.0,
            "triggerLeft": 0.0, "triggerRight": 0.0,
        })
        continue

    pressed = [NAMES[b] for b in NAMES if (raw[i] & b) and not (prev & b)]
    released = [NAMES[b] for b in NAMES if not (raw[i] & b) and (prev & b)]
    golden_frames.append({
        "index": i, "connected": True,
        "justPressed": pressed, "justReleased": released,
        "l2": lt[i] > 0.15, "r2": rt[i] > 0.15,
        "leftX": lx[i], "leftY": ly[i], "rightX": rx[i], "rightY": ry[i],
        "triggerLeft": lt[i], "triggerRight": rt[i],
    })
    prev = raw[i]

# ---------------------------------------------------------------- Ausgabe
here = os.path.dirname(os.path.abspath(__file__))
fix_dir = os.path.join(here, "..", "fixtures")

session = {
    "id": "session-001",
    "description": (
        "Aufgezeichnete Controller-Sitzung (720 Frames @ 50ms-Tick), deterministisch generiert in dem "
        "exakten Format eines Recordings aus dem ControllerTestWindow (TEST-016). Buttons sind "
        "GamepadButtonFlags-Bitmasks. Analogwerte reichen roh durch (KEIN Deadzone/Normalisierung - "
        "das ist Aufgabe der Backend-Layer InputReader/XInputFallbackService und liegt auerhalb des "
        "Replay-Skops). Frames 405..409 = Disconnect (L1+BtnY davor gehalten -> Ghost-Release-Waehrtest). "
        "Generator: tools/generate_session_001.py"
    ),
    "frames": [
        {"buttons": raw[i], "lx": lx[i], "ly": ly[i], "rx": rx[i], "ry": ry[i],
         "lt": lt[i], "rt": rt[i], "connected": conn[i]}
        for i in range(N)
    ],
}

golden = {
    "id": "session-001",
    "description": (
        "GOLDEN-Master: erwartete Pipeline-Ergebnisse pro Frame (unabhaengiges Orakel, erzeugt vom selben "
        "Generator wie die Session). justPressed/justReleased = digitale Transitionen via RawButtons/"
        "PrevRawButtons. l2/r2 = analoge Trigger-Flags (>0.15, NICHT in der Button-Mask). Analogwerte = "
        "bit-exakter Durchlauf der zugeflossenen Rohwerte."
    ),
    "frames": golden_frames,
}

for name, obj in (("controller_session_001.json", session),
                  ("controller_session_001.golden.json", golden)):
    path = os.path.join(fix_dir, name)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(obj, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(f"wrote {path} ({len(obj['frames'])} frames)")
