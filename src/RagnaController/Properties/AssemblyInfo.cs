using System.Runtime.CompilerServices;

// TEST-0xx / FEAT-015: Internals for das Testprojekt sichtbar machen,
// damit reine Auswahl-/Routing-Funktionen (z.B. WindowSwitcher.SelectTargetHwnd)
// ohne Win32-Seitenwirkung unit-testbar are.
[assembly: InternalsVisibleTo("RagnaController.Tests")]
