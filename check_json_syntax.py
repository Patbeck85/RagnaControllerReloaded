#!/usr/bin/env python3
import json
import os
import sys

def validate_json_file(filepath):
    """Prüft eine JSON-Datei auf Syntaxfehler"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            json.loads(content)
        return True, None
    except json.JSONDecodeError as e:
        return False, str(e)

def main():
    # Pfade zu JSON-Dateien
    json_files = [
        '/mnt/c/RagnaController/Locales/de.json',
        '/mnt/c/RagnaController/Locales/en.json',
        '/mnt/c/RagnaController/.hermes/agents.json',
        '/mnt/c/RagnaController/.hermes/bug_hunter.json',
        '/mnt/c/RagnaController/.hermes/build_agent.json',
        '/mnt/c/RagnaController/.hermes/doc_agent.json',
        '/mnt/c/RagnaController/.hermes/git_agent.json',
        '/mnt/c/RagnaController/.hermes/orchestrator.json',
        '/mnt/c/RagnaController/.hermes/performance_optimizer.json',
        '/mnt/c/RagnaController/.hermes/profile_creator.json',
        '/mnt/c/RagnaController/.hermes/test_writer.json',
        '/mnt/c/RagnaController/.hermes/ui_ux_agent.json',
    ]
    
    errors = []
    for filepath in json_files:
        if os.path.exists(filepath):
            valid, error = validate_json_file(filepath)
            if not valid:
                errors.append(f"{filepath}: {error}")
                print(f"FEHLER: {filepath}")
                print(f"  {error}")
    
    if errors:
        print(f"\n{len(errors)} Fehler gefunden!")
        return 1
    else:
        print("Alle JSON-Dateien sind gültig!")
        return 0

if __name__ == '__main__':
    sys.exit(main())
