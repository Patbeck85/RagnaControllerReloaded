---
name: localization-management
description: >
  Manage and synchronize localization across all supported language files.
  Validate that every language JSON file contains all required keys from the
  English (fallback) baseline, and add missing translations consistently.
category: software-development
tags:
  - localization
  - i18n
  - json
  - multilingual
  - validation
  - synchronization
---


# Localization Management

## Overview

Maintain synchronized localization across all supported language files. Validate that every language file contains all required keys from the English (fallback) baseline, and add missing translations consistently.

## When to Use

- Adding new localization keys to the English fallback
- Validating that all language files are synchronized
- Migrating or refactoring localization structure
- Onboarding new languages to the project
- Debugging missing or mismatched translations

## Core Principles

1. **English as Fallback**: `en.json` is the authoritative source for key set and default values
2. **All Languages Must Match**: Every non-English language file must contain ALL keys from English
3. **Number Placeholders Preserved**: Any `{0}`, `{1}`, etc. placeholders must be maintained across all languages
4. **JSON Validity**: All language files must parse as valid JSON
5. **Minimal Changes**: Only add missing keys — never remove or rename existing keys

## The Workflow

### Phase 1: Validate

```python
# Run the validation script
python src/RagnaController/validate_localization.py
```

Output shows:
- ✅ Valid JSON structure per file
- ✅ All keys from fallback present (or ❌ which are missing)
- ⚠️ Number placeholders with localized text

### Phase 2: Add Missing Keys

Use the script to add missing keys:

```python
# The missing_keys dict from the skill contains all new keys with English values
# Run against each language file
```

### Phase 3: Verify

Re-run validation to confirm all languages are synchronized.

## Key Workflow Details

### 1. Flatten JSON Structure

The validator flattens nested JSON to compare keys across levels:

```python
def flatten_dict(d, parent_key='', sep='.'):
    items = []
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k
        if isinstance(v, dict):
            items.extend(flatten_dict(v, new_key, sep=sep).items())
        else:
            items.append((new_key, v))
    return dict(items)
```

Keys are compared as `Category.Subcategory.Key` format.

### 2. Missing Key Detection

The script compares code usage against English JSON:

```python
code_keys = {
    "Controller.BatteryLevel_Empty",
    "Controller.ControllerName_NoController",
    # ... 20 total keys from code references
}

for key in sorted(code_keys):
    if key not in en_flat:
        missing.append(key)
```

### 3. Adding Keys to All Languages

Added 20 missing keys across 31 non-English language files:

**Controller:**
- `Controller.BatteryLevel_Empty` = "Battery: Empty"
- `Controller.ControllerName_NoController` = "No Controller Connected" 
- `Controller.Tab_NoMappings` = "No mappings configured"

**Macro:**
- `Macro.MacroTimeline_NoSteps` = "No steps defined"
- `Macro.MacroTimeline_HoverHint` = "Hover over a step for details"

**Tutorial:**
- `Tut.Btn_Finish` = "Finish"

**ComboEditor:**
- `ComboEditor.NoStepsDefined` = "No steps defined. Add steps to create a combo."

**CommunityBrowser:**
- `CommunityBrowser.Downloading` = "Downloading..."
- `CommunityBrowser.Installed` = "Installed"
- `CommunityBrowser.DownloadButton` = "Download"

**Handheld:**
- `Handheld.Play_NoGameFound_Message` = "No game found. Please start the game first."
- `Handheld.Play_NoGameFound_Title` = "Game Not Found"
- `Handheld.Play_Error_Message` = "Error launching game"

**ProfileLibrary:**
- `ProfileLibrary.Uploading` = "Uploading..."
- `ProfileLibrary.ShareButton` = "Share"

**RadialMenu:**
- `RadialMenu.SelectItem` = "Select an item"

**RadialSetup:**
- `RadialSetup.Downloading` = "Downloading..."
- `RadialSetup.ChatEmote` = "Chat Emote"
- `RadialSetup.Gallery` = "Gallery"

### 4. Number Placeholders

8 message templates contain `{0}` placeholders that are translated per language:

| Key | English | Arabic | German | Hindi |
|-----|---------|--------|--------|-------|
| Messages.Saved | {0} saved! | {0} تم حفظه! | {0} gespeichert! | {0} सेव हो गया! |
| Messages.Loaded | {0} loaded! | {0} تم تحميله! | {0} geladen! | {0} लोड हो गया! |
| Messages.Deleted | {0} deleted! | {0} تم حذفه! | {0} gelöscht! | {0} डिलीट हो गया! |
| Messages.Created | {0} created! | {0} تم إنشاؤه! | {0} erstellt! | {0} बनाया गया! |
| Messages.Updated | {0} updated! | {0} تم تحديثه! | {0} aktualisiert! | {0} अपडेट हो गया! |

## Supported Languages

32 language files validated:
- **European**: ar, bg, bn, cs, da, de, el, en, es, fa, fi, he, hi, hr, hu, it, ms, my, nl, no, pl, pt, ro, sk, sl, sr, sv, ta, tr, uk, ur, vi
- **Format**: All files use UTF-8 encoding with proper localization of number placeholders

## Files Created/Modified

### New Skill
- `skills/software-development/localization-management/SKILL.md`

### Modified Language Files
All 31 non-English JSON files in `Locales/` were updated with the 20 missing keys:
- ar.json, bg.json, bn.json, cs.json, da.json, de.json, el.json, es.json, fa.json, fi.json, he.json, hi.json, hr.json, hu.json, it.json, ms.json, my.json, nl.json, no.json, pl.json, pt.json, ro.json, sk.json, sl.json, sr.json, sv.json, ta.json, tr.json, uk.json, ur.json, vi.json

### Validation Script
- `src/RagnaController/validate_localization.py` - Updated with dynamic base directory resolution

## Commands & Tools

### Validation

```bash
cd /c/Hermes/RagnaController
python src/RagnaController/validate_localization.py
```

### Adding New Keys

When adding new keys to English, run the update script:

```python
# Missing keys dictionary format
missing_keys = {
    "New.Key.1": "Value in English",
    "New.Key.2": "Value in English",
    # ...
}

# Script adds to all language files automatically
```

## Pitfalls & Gotchas

1. **UTF-8 Encoding**: Ensure all JSON files remain UTF-8 compatible
2. **Number Placeholders**: Never remove `{0}`, `{1}` etc. from any language - they must match the English count
3. **Nested Structure**: When adding keys, add at the correct nesting level matching English structure
4. **JSON Syntax**: Verify all files remain valid JSON after modifications (the validator checks this)
5. **Extra Keys**: Languages MAY have additional keys beyond English - these are fine, but English keys must ALL be present
6. **Empty Values**: If a translation doesn't exist yet, use the English value as placeholder rather than leaving empty

## Verification Checklist

- [ ] Run validation script - all 32 files show ✅ All keys from fallback are present
- [ ] Number placeholders ({0}, {1}...) are preserved in all languages
- [ ] No JSON syntax errors introduced
- [ ] English file has 254 flattened keys (or updated count)
- [ ] All non-English files have ≥ English key count
- [ ] Code references all resolved (no undefined localization keys)

## Troubleshooting

### "X key missing after update"
- Check the key exists in en.json with correct nesting
- Verify the key wasn't already present but under different nesting
- Re-run the update script - it may have been skipped

### JSON parse error after modification
- Ensure no trailing commas
- Verify all strings are properly quoted
- Check for unescaped special characters

### Placeholder count mismatch
- The validator reports ⚠️ 8 keys with number placeholders
- Ensure each language has EXACTLY 8 placeholder keys (same count as English)
- The values differ per language, but the count must match

## Integration

### With Code

All localized strings are accessed via `LocalizationManager.GetLocalizedString("key")` or `LocalizationManager.Instance["key"]` in C# WPF applications.

### With CI/CD

Include validation in build pipeline:

```yaml
# Example GitHub Actions step
- name: Validate Localization
  run: python src/RagnaController/validate_localization.py
  # Fail build if any keys are missing
```

## Version History

- **1.0.0**: Initial release - Core validation and synchronization workflow
- **1.1.0**: Added dynamic base directory, improved script flexibility

## Related Skills

- `test-driven-development` - Write regression tests for localization keys
- `systematic-debugging` - Debug localization-related issues systematically
- `plan` - Plan localization migration or expansion projects
"""