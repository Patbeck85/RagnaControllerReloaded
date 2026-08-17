#!/bin/bash
# ===============================================================
# RagnaController Test Runner (WSL Optimized)
# Ziel: Bietet eine deterministische, Linux-native Umgebung zum Bauen und Testen.
# WARNUNG: Dieses Skript setzt voraus, dass der .NET SDK auf dem WSL-Host installiert ist.
# ===============================================================

echo "--- 🛠️ 1. Setup & Restore Dependencies ---"
# Wir gehen davon aus, dass das Hauptprojekt im RagnaController/src liegt und wir alle Projekte auflösen müssen.
dotnet restore /mnt/c/RagnaController/src/RagnaController.csproj

if [ $? -ne 0 ]; then
    echo "❌ FEHLER: Dependency Restore fehlgeschlagen. Bitte prüfen Sie die .NET SDK Installation."
    exit 1
fi
echo "✅ Dependencies erfolgreich wiederhergestellt."


echo ""
echo "--- 🔨 2. Build Core Projects ---"
# Wir bauen das Hauptprojekt, um sicherzustellen, dass alle Abhängigkeiten korrekt sind.
dotnet build /mnt/c/RagnaController/src/RagnaController.csproj

if [ $? -ne 0 ]; then
    echo "❌ FEHLER: Das Kompilieren des Core-Projekts ist fehlgeschlagen."
    exit 1
fi
echo "✅ Build des Kernmoduls erfolgreich."


echo ""
echo "--- ✅ 3. Run Tests (Unit & Integration) ---"

# Testen des spezifischen Unit/Integration Test Projekts
dotnet test /mnt/c/RagnaController/src/RagnaController.Tests/RagnaController.Tests.csproj --logger "trx;LogFileName=test_results.trx"

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================================"
    echo "🚀 ALLE TESTS BESTANDEN! (Code Green)"
    echo "Der State Machine Review-Protokoll ist validiert."
    echo "========================================================"
else
    echo ""
    echo "========================================================"
    echo "❌ TEST FEHLGESCHLAGENE!"
    echo "Bitte analysieren Sie die Fehlermeldungen im Output, um Fehler in State-Übergängen oder Unit Logic zu finden."
    echo "========================================================"
fi