---
name: ragnacontroller-xaml-element-creation
description: SDL2-Migration: XAML-Elemente erstellen, wenn sie nicht existieren - Pattern für fehlende Elemente
trigger: "XAML-Elemente müssen erstellt werden, weil sie in der Code-behind Referenz existieren aber nicht im XAML definiert sind"
version: 1.0
---

# RagnaController XAML Element Creation Pattern

## Overview

Dieses Skill dokumentiert den systematischen Ansatz zum Erstellen fehlender XAML-Elemente für SDL2-Migration und WPF-Projekte. Wenn Elemente in der Code-behind Referenz existieren aber nicht im XAML definiert sind, müssen sie erstellt werden.

---

## Problem: Fehlende XAML-Elemente

### Symptom

```csharp
// In MainWindow.xaml.cs
private readonly TextButton TurboButton;  // Referenziert wird

public MainWindow()
{
    InitializeComponent();
    
    // ❌ CRASH: TurboButton ist null!
    TurboButton.Click += OnTurboClick;
}
```

**Fehler:** `NullReferenceException` - Das Element existiert nicht im XAML.

---

## Lösung: Systematisches Erstellen

### Schritt 1: Bestehende Elemente Identifizieren

Überprüfen Sie, welche Elemente bereits im XAML definiert sind:

```bash
# Suche nach x:Name Attributen in XAML-Dateien
grep -r "x:Name" /mnt/c/RagnaController/src/RagnaController/*.xaml | grep -v "//"
```

### Schritt 2: Code-behind Felder Analysieren

```bash
# Liste alle readonly Felder in Code-behind
grep -E "private\s+readonly\s+(TextBlock|Button|StackPanel|Canvas)" \
    /mnt/c/RagnaController/src/RagnaController/MainWindow.xaml.cs
```

### Schritt 3: Lücken Identifizieren

Vergleichen Sie die beiden Listen. Felder, die in Code-behind existieren aber nicht im XAML, müssen erstellt werden.

---

## Erstellen von XAML-Elementen

### Pattern 1: Einfache Controls

```xml
<!-- Button -->
<Button x:Name="TurboButton" 
        Content="Turbo" 
        Width="80" 
        Height="30"
        Margin="5,0"
        Click="TurboButton_Click"/>

<!-- TextBlock -->
<TextBlock x:Name="StatusText" 
           Text="Ready" 
           FontSize="12"
           Foreground="White"
           HorizontalAlignment="Left"/>

<!-- StackPanel -->
<StackPanel x:Name="PanelBase" 
            Orientation="Horizontal"
            Margin="5"/>
```

### Pattern 2: Container Controls

```xml
<!-- Canvas für Overlays -->
<Canvas x:Name="PreviewContainer" 
        Background="Transparent"
        ClipToBounds="True"/>

<!-- ScrollViewer für Logs -->
<ScrollViewer x:Name="LogScrollViewer" 
              HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Auto">
    <TextBlock x:Name="LogText" Text="" FontFamily="Consolas"/>
</ScrollViewer>
```

### Pattern 3: Layout Panels

```xml
<!-- Grid für komplexe Layouts -->
<Grid x:Name="PanelInfo" 
      Margin="5,0">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>
    
    <TextBlock Grid.Row="0" Text="Info:" FontWeight="Bold"/>
    <TextBlock Grid.Row="1" x:Name="InfoContent" Text="..." FontSize="12"/>
</Grid>
```

---

## SDL2-Migration Spezifisch

### Haptic Metronome Pattern

Für ASPD-Feedback mit 15ms Pulse:

```xml
<Button x:Name="TurboButton" 
        Content="TURBO" 
        Width="100" 
        Height="40"
        Background="#FF4444"
        Click="TurboButton_Click"/>

<Button x:Name="SprintButton" 
        Content="SPRINT" 
        Width="100" 
        Height="40"
        Background="#FF6644"
        Click="SprintButton_Click"/>
```

### Voice Announcer Pattern

```xml
<TextBlock x:Name="ToastText" 
           Text="" 
           FontSize="14"
           Foreground="Yellow"
           Background="Black"
           Padding="10,5"/>
```

---

## Build Verification

### Nach Element-Erstellung

```bash
# Clean build
dotnet clean && dotnet restore && dotnet build

# Erwartete Ausgabe:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

### WICHTIG: XAML Updates FIRST

Bevor Sie Code-behind aktualisieren:
1. ✅ XAML-Elemente erstellen
2. ✅ InitializeComponent() aufrufen
3. ✅ Code-behind Felder deklarieren
4. ✅ Build testen

**Nicht:**
1. ❌ Code-behind Felder deklarieren
2. ❌ Build (Fehler!)
3. ❌ XAML aktualisieren
4. ❌ Build wiederholen

---

## Best Practices

### 1. Konsistente Namensgebung

```xml
<!-- ✅ GUT - klar und deskriptiv -->
<Button x:Name="TurboButton" Content="TURBO"/>
<TextBlock x:Name="StatusText" Text="Ready"/>

<!-- ❌ SCHLECHT - unklar -->
<Button x:Name="Btn1" Content="..."/>
<TextBlock x:Name="Txt1" Text="..."/>
```

### 2. Standard-Größen und Abstände

```xml
<!-- ✅ GUT - konsistente Größen -->
<Button Width="80" Height="30" Margin="5,0"/>
<TextBlock FontSize="12" Padding="5,2"/>

<!-- ❌ SCHLECHT - inkonsistent -->
<Button Width="100" Height="40"/>
<TextBlock FontSize="14" Padding="10,5"/>
```

### 3. Accessibility Attributes

```xml
<!-- ✅ GUT - accessible -->
<Button x:Name="TurboButton" 
        Content="TURBO"
        ToolTip.ToolTipContent="Aktiviert Turbo-Modus"
        Tag="turbo-mode"/>

<TextBlock x:Name="StatusText" 
           Text="Ready"
           Tag="status-display"/>
```

---

## Common XAML Elements for SDL2

### Button Controls

```xml
<!-- Turbo Button -->
<Button x:Name="TurboButton" 
        Content="TURBO" 
        Width="100" 
        Height="40"
        Background="#FF3333"
        Foreground="White"
        FontSize="14"
        FontWeight="Bold"
        Click="TurboButton_Click"/>

<!-- Sprint Button -->
<Button x:Name="SprintButton" 
        Content="SPRINT" 
        Width="100" 
        Height="40"
        Background="#FF5533"
        Foreground="White"
        FontSize="14"
        FontWeight="Bold"
        Click="SprintButton_Click"/>

<!-- Dodge Button -->
<Button x:Name="DodgeButton" 
        Content="DODGE" 
        Width="100" 
        Height="40"
        Background="#FF6633"
        Foreground="White"
        FontSize="14"
        FontWeight="Bold"
        Click="DodgeButton_Click"/>
```

### Info Panels

```xml
<!-- Status Text -->
<TextBlock x:Name="StatusText" 
           Text="Ready" 
           FontSize="12"
           Foreground="White"
           Background="Transparent"
           HorizontalAlignment="Left"
           VerticalAlignment="Top"
           Margin="5,5"/>

<!-- Info Panel -->
<StackPanel x:Name="PanelInfo" 
            Orientation="Vertical"
            Margin="5,0">
    <TextBlock Text="Class:" FontWeight="Bold" Foreground="Yellow"/>
    <TextBlock x:Name="InfoClassType" Text="Mage" Foreground="White"/>
    
    <TextBlock Text="Skills:" FontWeight="Bold" Foreground="Yellow"/>
    <TextBlock x:Name="InfoSkillList" Text="..." Foreground="White"/>
</StackPanel>
```

### Log Viewer

```xml
<ScrollViewer x:Name="LogScrollViewer" 
              HorizontalScrollBarVisibility="Auto"
              VerticalScrollBarVisibility="Auto"
              Background="#1E1E1E">
    <TextBlock x:Name="LogText" 
               Text="" 
               FontFamily="Consolas"
               FontSize="10"
               Foreground="#CCCCCC"
               Padding="5,0"/>
</ScrollViewer>
```

---

## Migration Checklist

### Vor der Migration

- [ ] Alle XAML-Elemente identifizieren
- [ ] Code-behind Felder dokumentieren
- [ ] Lücken zwischen XAML und Code finden

### Während der Migration

- [ ] XAML-Elemente erstellen (BEVOR Code-behind aktualisieren)
- [ ] Konsistente Namensgebung verwenden
- [ ] Standard-Größen und Abstände setzen
- [ ] Accessibility Attributes hinzufügen

### Nach der Migration

- [ ] Build testen (`dotnet build`)
- [ ] NullReferenceException prüfen
- [ ] UI-Layout verifizieren
- [ ] ToolTips und Accessibility testen

---

## References

- [WPF XAML Overview](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/xaml/xaml-overview)
- [WPF Controls Catalog](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/wpf-controls-catalog)
- [RagnaController SDL2 Migration](./SDL2-Migration.md)

---

## Related Skills

- `ragnacontroller-xaml-field-warnings` - CS0649 warnings für XAML-Felder
- `ragnacontroller-build-debugging` - Build-Fehler beheben
- `wpf-xaml-development-patterns` - WPF/XAML Best Practices
