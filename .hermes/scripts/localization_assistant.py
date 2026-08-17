#!/usr/bin/env python3
"""
Localization Assistant für RagnaController
Findet fehlende Übersetzungen und hilft bei der Lokalisierung
"""

import os
import re
import json
from pathlib import Path
from typing import Dict, List, Set

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
XAML_DIR = PROJECT_ROOT / "src" / "RagnaController" / "Views"
CS_CODE_DIR = PROJECT_ROOT / "src" / "RagnaController" / "ViewModels"
LOCALES_DIR = PROJECT_ROOT / "Locales"
DEFAULT_LOCALE_FILE = LOCALES_DIR / "en.json"

class LocalizationAssistant:
    """Hilft bei der Lokalisierung von RagnaController"""
    
    def __init__(self):
        self.locale_data: Dict[str, Dict] = {}
        self.missing_keys: List[Dict] = []
        self.hardcoded_strings: List[Dict] = []
        self.placeholder_issues: List[Dict] = []
        
    def load_locale_file(self, filepath: Path) -> Dict:
        """Lädt eine Locale-Datei"""
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                return json.load(f)
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
            return {}
    
    def scan_xaml_for_localization_markers(self) -> List[Dict]:
        """Scannt XAML-Dateien nach {core:Loc} Markern"""
        markers = []
        
        xaml_files = list(XAML_DIR.rglob("*.xaml"))
        
        for xaml_file in xaml_files:
            try:
                with open(xaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Finde alle {core:Loc Key} Marker
                pattern = r'\{core:Loc\s+(\w+)\}'
                matches = re.findall(pattern, content)
                
                for key in matches:
                    markers.append({
                        "file": str(xaml_file.relative_to(PROJECT_ROOT)),
                        "key": key
                    })
                    
            except Exception as e:
                print(f"Error scanning {xaml_file}: {e}")
        
        return markers
    
    def scan_for_hardcoded_strings(self) -> List[Dict]:
        """Findet Hardcoded Strings in XAML, die lokalisiert werden sollten"""
        hardcoded = []
        
        xaml_files = list(XAML_DIR.rglob("*.xaml"))
        
        for xaml_file in xaml_files:
            try:
                with open(xaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Finde Text zwischen > und </ (nicht {core:Loc})
                pattern = r'>([^<\{]+)</'
                matches = re.findall(pattern, content)
                
                for match in matches:
                    # Überspringe bekannte UI-Texte
                    if any(ui_text in match.lower() for ui_text in [
                        'button', 'textblock', 'stackpanel', 'grid',
                        'window', 'dialog', 'messagebox', 'label'
                    ]):
                        continue
                        
                    # Überspringe Platzhalter
                    if '{0}' in match or '{1}' in match or '{2}' in match:
                        continue
                        
                    hardcoded.append({
                        "file": str(xaml_file.relative_to(PROJECT_ROOT)),
                        "string": match.strip(),
                        "suggestion": self._suggest_key(match)
                    })
                    
            except Exception as e:
                print(f"Error scanning {xaml_file}: {e}")
        
        return hardcoded
    
    def _suggest_key(self, text: str) -> str:
        """Generiert einen Vorschlag für den Lokalisierungsschlüssel"""
        # Entferne Sonderzeichen und mache klein
        clean = re.sub(r'[^\w\s-]', '', text)
        clean = clean.strip()
        
        # Ersetze Leerzeichen mit Unterstrich
        key = clean.replace(' ', '_').replace('-', '_')
        
        return key.lower()
    
    def check_placeholders(self) -> List[Dict]:
        """Überprüft auf Placeholder-Probleme"""
        issues = []
        
        xaml_files = list(XAML_DIR.rglob("*.xaml"))
        
        for xaml_file in xaml_files:
            try:
                with open(xaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Finde {core:Loc Key} mit Platzhaltern
                pattern = r'\{core:Loc\s+(\w+)\}\s*\{([^}]+)\}'
                matches = re.findall(pattern, content)
                
                for key, placeholder in matches:
                    issues.append({
                        "file": str(xaml_file.relative_to(PROJECT_ROOT)),
                        "key": key,
                        "placeholder": placeholder.strip(),
                        "issue": "Placeholder needs parameter in code"
                    })
                    
            except Exception as e:
                print(f"Error checking {xaml_file}: {e}")
        
        return issues
    
    def get_missing_keys(self) -> List[Dict]:
        """Findet fehlende Locale-Keys"""
        if not self.locale_data:
            self.load_locale_file(DEFAULT_LOCALE_FILE)
        
        all_markers = self.scan_xaml_for_localization_markers()
        unique_keys = set(marker['key'] for marker in all_markers)
        existing_keys = set(self.locale_data.keys())
        
        missing = []
        for key in unique_keys - existing_keys:
            # Finde alle Dateien, die diesen Key verwenden
            files_using_key = []
            for marker in all_markers:
                if marker['key'] == key:
                    files_using_key.append(marker['file'])
            
            missing.append({
                "key": key,
                "files": list(set(files_using_key)),
                "suggestion": self._suggest_key(key)
            })
        
        return missing
    
    def generate_missing_locale_entries(self) -> List[str]:
        """Generiert fehlende Locale-Einträge"""
        missing = self.get_missing_keys()
        entries = []
        
        for item in missing:
            key = item['key']
            suggestion = item['suggestion']
            
            # Generiere einen sinnvollen Text basierend auf dem Key
            text = f"TODO: Translate '{key}' - Used in: {', '.join(item['files'])}"
            
            entries.append(f'    "{key}": "{text}"')
        
        return entries
    
    def generate_placeholder_fixes(self) -> List[str]:
        """Generiert Fixes für Placeholder-Probleme"""
        placeholders = self.check_placeholders()
        fixes = []
        
        for item in placeholders:
            key = item['key']
            placeholder = item['placeholder']
            
            # Generiere einen Fix-Vorschlag
            fix = f"// {item['file']}:{key} - Add parameter for '{placeholder}' in code-behind"
            fixes.append(fix)
        
        return fixes
    
    def generate_hardcoded_string_fixes(self) -> List[str]:
        """Generiert Fixes für Hardcoded Strings"""
        hardcoded = self.scan_for_hardcoded_strings()
        fixes = []
        
        for item in hardcoded:
            file = item['file']
            string = item['string']
            suggestion = item['suggestion']
            
            fix = f"// {file}: Replace '{string}' with {{core:Loc {suggestion}}}"
            fixes.append(fix)
        
        return fixes
    
    def generate_complete_missing_section(self) -> str:
        """Generiert einen kompletten fehlenden Section für Locale-Datei"""
        missing = self.get_missing_keys()
        
        if not missing:
            return "// No missing keys found"
        
        lines = [
            "",
            "// ============================================",
            "// MISSING LOCALIZATION KEYS",
            "// ============================================",
        ]
        
        for item in missing:
            key = item['key']
            suggestion = item['suggestion']
            files = ', '.join(item['files'])
            
            lines.append(f'    "{key}": "{{core:Loc {suggestion}}}",  // TODO: Add translation')
        
        return "\n".join(lines)


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("LOCALIZATION ASSISTANT - RagnaController")
    print("=" * 60)
    print()
    
    assistant = LocalizationAssistant()
    
    # Lade Locale-Datei
    print("📂 Loading locale files...")
    default_locale = DEFAULT_LOCALE_FILE
    if default_locale.exists():
        print(f"   Found: {default_locale}")
        assistant.load_locale_file(default_locale)
    else:
        print(f"   Warning: {default_locale} not found")
    
    print()
    
    # Scan nach {core:Loc} Markern
    print("🔍 Scanning XAML files for {core:Loc} markers...")
    markers = assistant.scan_xaml_for_localization_markers()
    print(f"   Found {len(markers)} localization markers")
    print()
    
    # Finde fehlende Keys
    print("🔎 Finding missing locale keys...")
    missing_keys = assistant.get_missing_keys()
    print(f"   Missing keys: {len(missing_keys)}")
    
    if missing_keys:
        print()
        print("   Missing keys:")
        for item in missing_keys[:10]:  # Zeige erste 10
            print(f"      - {item['key']} (suggested: {item['suggestion']})")
            print(f"        Used in: {', '.join(item['files'])}")
        
        if len(missing_keys) > 10:
            print(f"      ... and {len(missing_keys) - 10} more")
    else:
        print("   ✅ All locale keys are present!")
    
    print()
    
    # Finde Hardcoded Strings
    print("🔍 Finding hardcoded strings...")
    hardcoded = assistant.scan_for_hardcoded_strings()
    print(f"   Found {len(hardcoded)} potential hardcoded strings")
    
    if hardcoded:
        print()
        print("   Hardcoded strings:")
        for item in hardcoded[:10]:  # Zeige erste 10
            print(f"      - {item['file']}: '{item['string']}'")
            print(f"        Suggested key: {item['suggestion']}")
    else:
        print("   ✅ No hardcoded strings found!")
    
    print()
    
    # Placeholder-Check
    print("🔍 Checking placeholders...")
    placeholders = assistant.check_placeholders()
    print(f"   Found {len(placeholders)} placeholder issues")
    
    if placeholders:
        print()
        print("   Placeholder issues:")
        for item in placeholders[:5]:  # Zeige erste 5
            print(f"      - {item['file']}: {item['key']} ({item['placeholder']})")
    else:
        print("   ✅ No placeholder issues found!")
    
    print()
    
    # Generiere Vorschläge für fehlende Locale-Einträge
    if missing_keys:
        print("=" * 60)
        print("📝 MISSING LOCALIZATION ENTRIES (Copy to your locale file)")
        print("=" * 60)
        print()
        
        assistant.load_locale_file(DEFAULT_LOCALE_FILE)
        existing_keys = set(assistant.locale_data.keys())
        missing_key_objects = [k for k in missing_keys if k['key'] not in existing_keys]
        
        entries = []
        for item in missing_key_objects:
            key = item['key']
            suggestion = item['suggestion']
            
            # Generiere einen sinnvollen Text
            text = f"TODO: Translate '{key}' - Used in: {', '.join(item['files'])}"
            entries.append(f'    "{key}": "{text}"')
        
        print("".join(entries))
        print()
    
    # Zusammenfassung
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    print(f"   XAML files scanned: {len(list(XAML_DIR.rglob('*.xaml')))}")
    print(f"   Localization markers found: {len(markers)}")
    print(f"   Missing locale keys: {len(missing_keys)}")
    print(f"   Hardcoded strings: {len(hardcoded)}")
    print(f"   Placeholder issues: {len(placeholders)}")
    print()
    
    total_issues = len(missing_keys) + len(hardcoded) + len(placeholders)
    
    if total_issues == 0:
        print("✅ LOCALIZATION CHECK COMPLETE - No issues found!")
    else:
        print(f"⚠️  Found {total_issues} localization issues that should be addressed.")
    
    return 0 if total_issues == 0 else 1


if __name__ == "__main__":
    import sys
    sys.exit(main())
