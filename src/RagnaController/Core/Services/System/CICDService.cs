using System;
using System.IO;

namespace RagnaController.Core.Services.System {
    /// <summary>
    /// TASK-3306: CICDService — CI/CD Pipeline Informationen und Konfiguration.
    ///
    /// Bietet Informationen über die CI/CD Pipeline für GitHub Actions:
    ///   • Matrix-Builds (Windows 10/11, x64/ARM64)
    ///   • Stryker.NET Mutation Testing
    ///   • Code-Signierung
    ///   • Release-Erstellung
    ///
    /// White-Hat: Nur Build/Release-Informationen, keine Spielautomatisierung.
    /// </summary>
    public sealed class CICDService
    {
        /// <summary>GitHub Actions Workflow-Datei für CI/CD.</summary>
        public const string WorkflowFileName = "ci-cd.yml";

        /// <summary>Unterstützte Windows-Versionen im Build-Matrix.</summary>
        public static string[] WindowsVersions => new[] { "windows-10", "windows-11" };

        /// <summary>Unterstützte Architekturen im Build-Matrix.</summary>
        public static string[] Architectures => new[] { "x64", "arm64" };

        /// <summary>Ob Stryker.NET Mutation Testing aktiviert ist.</summary>
        public static bool StrykerEnabled => true;

        /// <summary>Ob Code-Signierung aktiviert ist.</summary>
        public static bool CodeSigningEnabled => true;

        /// <summary>Gibt die GitHub Actions Workflow-YAML zurück.</summary>
        public static string GetWorkflowYaml()
        {
            return @"
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      fail-fast: false
      matrix:
        os: [windows-10, windows-11]
        architecture: [x64, arm64]
    steps:
    - uses: actions/checkout@v4
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '8.0.x'
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore -c Release -a ${{ matrix.architecture }}
    - name: Run tests
      run: dotnet test --no-restore -c Release --verbosity normal
    - name: Stryker.NET Mutation Testing
      if: ${{ matrix.os == 'windows-10' && matrix.architecture == 'x64' }}
      run: dotnet stryker
    - name: Publish
      if: github.ref == 'refs/heads/main'
      run: dotnet publish -c Release -a ${{ matrix.architecture }} -o ./publish/${{ matrix.os }}/${{ matrix.architecture }}
    - name: Code Sign
      if: github.ref == 'refs/heads/main' && ${{ secrets.CODE_SIGN_CERT != '' }}
      run: signtool sign /f ${{ secrets.CODE_SIGN_CERT }} /p ${{ secrets.CODE_SIGN_PASSWORD }} ./publish/**/*.exe
";
        }

        /// <summary>Erstellt die CI/CD Workflow-Datei im Repository.</summary>
        public static void CreateWorkflowFile(string repositoryRoot)
        {
            var workflowDir = Path.Combine(repositoryRoot, ".github", "workflows");
            Directory.CreateDirectory(workflowDir);
            var workflowPath = Path.Combine(workflowDir, WorkflowFileName);
            File.WriteAllText(workflowPath, GetWorkflowYaml());
        }

        /// <summary>Gibt die Release-Checkliste zurück.</summary>
        public static string[] GetReleaseChecklist()
        {
            return new[]
            {
                "[ ] Alle Tests passen (100%)",
                "[ ] Stryker.NET Mutation Testing >= 80%",
                "[ ] Build auf Windows 10 und 11 erfolgreich",
                "[ ] Build für x64 und ARM64 erfolgreich",
                "[ ] Code-Signierung durchgeführt",
                "[ ] MSIX-Paket erstellt",
                "[ ] Winget/Chocolatey/Scoop Manifest aktualisiert",
                "[ ] GitHub Release erstellt",
                "[ ] CHANGELOG.md aktualisiert",
                "[ ] Versionsnummer aktualisiert",
            };
        }
    }
}
