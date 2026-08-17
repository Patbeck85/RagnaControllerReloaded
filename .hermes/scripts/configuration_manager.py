#!/usr/bin/env python3
"""
Configuration Manager für RagnaController
Verwaltet und validiert JSON-Konfigurationen
"""

import os
import re
import json
import sys
from pathlib import Path
from typing import Dict, List, Optional, Any
from datetime import datetime

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
CONFIG_DIR = PROJECT_ROOT / "Config"
DEFAULT_CONFIG_FILE = CONFIG_DIR / "settings.json"
VALIDATION_RULES_FILE = CONFIG_DIR / "validation_rules.json"

class ConfigurationManager:
    """Verwaltet Konfigurationen für RagnaController"""
    
    def __init__(self):
        self.config: Dict[str, Any] = {}
        self.validation_rules: Dict[str, Any] = {}
        self.errors: List[Dict] = []
        self.warnings: List[Dict] = []
        
    def load_config_file(self, filepath: Path) -> bool:
        """Lädt eine Konfigurationsdatei"""
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                self.config = json.load(f)
            return True
        except json.JSONDecodeError as e:
            print(f"JSON parse error in {filepath}: {e}")
            return False
        except Exception as e:
            print(f"Error loading {filepath}: {e}")
            return False
    
    def save_config_file(self, filepath: Path) -> bool:
        """Speichert Konfigurationsdatei"""
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                json.dump(self.config, f, indent=2, ensure_ascii=False)
            return True
        except Exception as e:
            print(f"Error saving {filepath}: {e}")
            return False
    
    def validate_config(self) -> List[Dict]:
        """Validiert Konfiguration gegen Regeln"""
        self.errors = []
        self.warnings = []
        
        if not self.config:
            return self.errors
        
        # Validiere erforderliche Felder
        required_fields = ["game", "input", "graphics", "audio"]
        
        for field in required_fields:
            if field not in self.config:
                self.errors.append({
                    "type": "missing_field",
                    "field": field,
                    "message": f"Required field '{field}' is missing"
                })
        
        # Validiere Game-Konfiguration
        if "game" in self.config:
            game_config = self.config["game"]
            
            if "server_address" in game_config:
                server = game_config["server_address"]
                if not server.startswith("http"):
                    self.errors.append({
                        "type": "invalid_server",
                        "field": "game.server_address",
                        "value": server,
                        "message": "Server address must start with http://"
                    })
            
            if "port" in game_config:
                port = game_config["port"]
                if not isinstance(port, int) or port < 1 or port > 65535:
                    self.errors.append({
                        "type": "invalid_port",
                        "field": "game.port",
                        "value": port,
                        "message": "Port must be between 1 and 65535"
                    })
        
        # Validiere Input-Konfiguration
        if "input" in self.config:
            input_config = self.config["input"]
            
            if "key_mapping" in input_config:
                key_mapping = input_config["key_mapping"]
                
                for key, action in key_mapping.items():
                    if not isinstance(key, str) or len(key) == 0:
                        self.errors.append({
                            "type": "invalid_key",
                            "field": f"input.key_mapping.{key}",
                            "value": key,
                            "message": "Key mapping keys must be non-empty strings"
                        })
                    
                    if action not in ["move_forward", "move_backward", "turn_left", "turn_right", 
                                     "attack", "defend", "cast_spell", "use_item", "interact"]:
                        self.warnings.append({
                            "type": "unknown_action",
                            "field": f"input.key_mapping.{key}",
                            "value": action,
                            "message": f"Unknown action '{action}' in key mapping"
                        })
        
        # Validiere Graphics-Konfiguration
        if "graphics" in self.config:
            graphics_config = self.config["graphics"]
            
            if "resolution" in graphics_config:
                resolution = graphics_config["resolution"]
                
                if isinstance(resolution, list) and len(resolution) == 2:
                    width, height = resolution
                    
                    if not isinstance(width, int) or width < 1 or width > 8192:
                        self.errors.append({
                            "type": "invalid_resolution",
                            "field": "graphics.resolution.width",
                            "value": width,
                            "message": "Resolution width must be between 1 and 8192"
                        })
                    
                    if not isinstance(height, int) or height < 1 or height > 8192:
                        self.errors.append({
                            "type": "invalid_resolution",
                            "field": "graphics.resolution.height",
                            "value": height,
                            "message": "Resolution height must be between 1 and 8192"
                        })
            
            if "v_sync" in graphics_config:
                v_sync = graphics_config["v_sync"]
                if v_sync not in [True, False]:
                    self.errors.append({
                        "type": "invalid_vsync",
                        "field": "graphics.v_sync",
                        "value": v_sync,
                        "message": "V-Sync must be boolean (true/false)"
                    })
        
        # Validiere Audio-Konfiguration
        if "audio" in self.config:
            audio_config = self.config["audio"]
            
            if "master_volume" in audio_config:
                volume = audio_config["master_volume"]
                if not isinstance(volume, (int, float)) or volume < 0 or volume > 1:
                    self.errors.append({
                        "type": "invalid_volume",
                        "field": "audio.master_volume",
                        "value": volume,
                        "message": "Volume must be between 0 and 1"
                    })
            
            if "music_enabled" in audio_config:
                music = audio_config["music_enabled"]
                if music not in [True, False]:
                    self.errors.append({
                        "type": "invalid_music",
                        "field": "audio.music_enabled",
                        "value": music,
                        "message": "Music enabled must be boolean (true/false)"
                    })
        
        return self.errors + self.warnings
    
    def get_error_count(self) -> int:
        """Zählt Fehler"""
        return len(self.errors)
    
    def get_warning_count(self) -> int:
        """Zählt Warnungen"""
        return len(self.warnings)
    
    def generate_report(self) -> str:
        """Generiert einen formatierten Bericht"""
        lines = []
        
        lines.append("=" * 60)
        lines.append("CONFIGURATION MANAGER - RagnaController")
        lines.append("=" * 60)
        lines.append("")
        
        if self.config:
            lines.append("📋 CONFIGURATION SUMMARY:")
            lines.append("-" * 40)
            
            # Game
            if "game" in self.config:
                game = self.config["game"]
                lines.append(f"   🎮 Game:")
                if "server_address" in game:
                    lines.append(f"      Server: {game['server_address']}")
                if "port" in game:
                    lines.append(f"      Port: {game['port']}")
            
            # Input
            if "input" in self.config:
                input_config = self.config["input"]
                lines.append(f"   ⌨️  Input:")
                if "key_mapping" in input_config:
                    lines.append(f"      Keys configured: {len(input_config['key_mapping'])}")
            
            # Graphics
            if "graphics" in self.config:
                graphics = self.config["graphics"]
                lines.append(f"   🎨 Graphics:")
                if "resolution" in graphics:
                    res = graphics["resolution"]
                    if isinstance(res, list):
                        lines.append(f"      Resolution: {res[0]}x{res[1]}")
                if "v_sync" in graphics:
                    lines.append(f"      V-Sync: {'Enabled' if graphics['v_sync'] else 'Disabled'}")
            
            # Audio
            if "audio" in self.config:
                audio = self.config["audio"]
                lines.append(f"   🔊 Audio:")
                if "master_volume" in audio:
                    vol = audio["master_volume"]
                    lines.append(f"      Volume: {vol*100:.0f}%")
                if "music_enabled" in audio:
                    lines.append(f"      Music: {'Enabled' if audio['music_enabled'] else 'Disabled'}")
        else:
            lines.append("⚠️  No configuration loaded")
        
        lines.append("")
        
        # Fehler
        if self.errors:
            lines.append(f"🔴 ERRORS: {len(self.errors)}")
            lines.append("-" * 40)
            for error in self.errors[:10]:  # Zeige erste 10
                lines.append(f"   ❌ {error['message']}")
                if 'field' in error:
                    lines.append(f"      Field: {error['field']}")
                if 'value' in error:
                    lines.append(f"      Value: {error['value']}")
            if len(self.errors) > 10:
                lines.append(f"   ... and {len(self.errors) - 10} more errors")
        else:
            lines.append("✅ No errors")
        
        lines.append("")
        
        # Warnungen
        if self.warnings:
            lines.append(f"🟠 WARNINGS: {len(self.warnings)}")
            lines.append("-" * 40)
            for warning in self.warnings[:10]:  # Zeige erste 10
                lines.append(f"   ⚠️  {warning['message']}")
                if 'field' in warning:
                    lines.append(f"      Field: {warning['field']}")
                if 'value' in warning:
                    lines.append(f"      Value: {warning['value']}")
            if len(self.warnings) > 10:
                lines.append(f"   ... and {len(self.warnings) - 10} more warnings")
        else:
            lines.append("✅ No warnings")
        
        return "\n".join(lines)


def load_default_config() -> Dict[str, Any]:
    """Lädt Standard-Konfiguration"""
    default_config = {
        "game": {
            "server_address": "http://localhost:8080",
            "port": 8080,
            "max_players": 16
        },
        "input": {
            "key_mapping": {
                "w": "move_forward",
                "s": "move_backward",
                "a": "turn_left",
                "d": "turn_right",
                "space": "attack",
                "e": "interact",
                "r": "use_item"
            }
        },
        "graphics": {
            "resolution": [1920, 1080],
            "v_sync": True,
            "fullscreen": False,
            "anti_aliasing": 4
        },
        "audio": {
            "master_volume": 0.8,
            "music_enabled": True,
            "sfx_enabled": True,
            "voice_chat_enabled": False
        }
    }
    
    return default_config


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("CONFIGURATION MANAGER - RagnaController")
    print("=" * 60)
    print()
    
    manager = ConfigurationManager()
    
    # Lade Standard-Konfiguration
    print("📂 Loading default configuration...")
    manager.config = load_default_config()
    print("   ✅ Default configuration loaded")
    print()
    
    # Validiere Konfiguration
    print("✅ Validating configuration...")
    validation_results = manager.validate_config()
    
    if validation_results:
        errors = [r for r in validation_results if r['type'] == 'error']
        warnings = [r for r in validation_results if r['type'] == 'warning']
        
        print(f"   Found {len(errors)} errors and {len(warnings)} warnings")
    else:
        print("   ✅ Configuration is valid")
    
    print()
    
    # Generiere Bericht
    report = manager.generate_report()
    print("=" * 60)
    print(report)
    print("=" * 60)
    print()
    
    # Zusammenfassung
    error_count = manager.get_error_count()
    warning_count = manager.get_warning_count()
    
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    print(f"   Configuration fields: 4 (game, input, graphics, audio)")
    print(f"   Key mappings: {len(manager.config.get('input', {}).get('key_mapping', {}))}")
    print(f"   Errors: {error_count}")
    print(f"   Warnings: {warning_count}")
    
    if error_count == 0 and warning_count == 0:
        print()
        print("✅ CONFIGURATION VALID - No issues found!")
    else:
        print()
        print("⚠️  Please review the configuration issues above.")
    
    return 0 if error_count == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
