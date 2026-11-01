#!/usr/bin/env python3
"""
Translate all RagnaController locale files using Argos Translate (offline, no API limits).
Uses C:/Python314/python.exe which has argostranslate installed with 33 language packages.
"""
import json, os, re, subprocess, sys, time
from pathlib import Path

# Use the Python with argostranslate installed
PYTHON = "C:/Python314/python.exe"

LOCALES_DIR = Path("C:/Hermes/RagnaController/src/RagnaController/Locales")
CHECKPOINT_DIR = Path("C:/Hermes/RagnaController/.workspace_temp/argostranslate_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

# 33 languages supported by argostranslate (installed)
SUPPORTED_LANGS = [
    "ar", "bg", "bn", "cs", "da", "el", "es", "fa", "fi", "fr",
    "he", "hi", "hu", "id", "it", "ja", "ko", "ms", "nl", "pl",
    "pt", "ro", "ru", "sk", "sl", "sv", "th", "tl", "tr", "uk",
    "ur", "vi", "zh"
]

# Languages NOT supported by argostranslate (will keep English placeholders)
UNSUPPORTED_LANGS = [
    "gu", "hr", "kn", "ml", "mr", "my", "no", "or", "pa", "sr", "ta", "te", "zh-cn"
]

# Placeholder keys that contain format specifiers
PLACEHOLDER_PATTERN = re.compile(r"\{[0-9a-zA-Z_]+\}|%\w")

def load_en_data():
    with open(LOCALES_DIR / "en.json", "r", encoding="utf-8") as f:
        return json.load(f)

def get_checkpoint_path(lang_code):
    return CHECKPOINT_DIR / f"{lang_code}.json"

def load_checkpoint(lang_code):
    cp = get_checkpoint_path(lang_code)
    if cp.exists():
        with open(cp, "r", encoding="utf-8") as f:
            return json.load(f)
    return {"completed": [], "count": 0}

def save_checkpoint(lang_code, data):
    cp = get_checkpoint_path(lang_code)
    with open(cp, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

def translate_batch(texts, target_lang, max_retries=3):
    """Translate a batch of texts using argostranslate via subprocess."""
    # Build Python script
    script = f"""
import json, sys
import argostranslate.translate

texts = {json.dumps(texts, ensure_ascii=False)}
target = "{target_lang}"

results = []
for text in texts:
    if not text or text.strip() == "":
        results.append(text)
        continue
    import re
    if re.search(r"\\{{[0-9a-zA-Z_]+\\}}", text) or re.search(r"%\\w", text):
        results.append(text)
        continue
    try:
        result = argostranslate.translate.translate(text, "en", target)
        results.append(result)
    except Exception as e:
        results.append(text)  # Keep original on error

print(json.dumps(results, ensure_ascii=False))
"""
    for attempt in range(max_retries):
        try:
            result = subprocess.run(
                [PYTHON, "-c", script],
                capture_output=True, text=True, timeout=60, encoding="utf-8"
            )
            if result.returncode == 0:
                return json.loads(result.stdout.strip())
            else:
                print(f"  Subprocess error (attempt {attempt+1}): {result.stderr[:200]}")
        except Exception as e:
            print(f"  Subprocess exception (attempt {attempt+1}): {e}")
        time.sleep(2)
    return texts  # Return originals on failure

def process_language(lang_code, en_data, batch_size=20):
    fpath = LOCALES_DIR / f"{lang_code}.json"
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    # Find keys that match English and aren't placeholders
    untranslated = []
    for k, v in en_data.items():
        if k in data and data[k] == v:
            if not PLACEHOLDER_PATTERN.search(str(v)):
                untranslated.append(k)
    
    if not untranslated:
        print(f"  {lang_code}: Already fully translated")
        return 0
    
    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed", []))
    remaining = [k for k in untranslated if k not in completed]
    
    if not remaining:
        print(f"  {lang_code}: Already completed (checkpoint)")
        return cp.get("count", 0)
    
    print(f"  {lang_code}: {len(remaining)}/{len(untranslated)} keys to translate")
    
    translated_count = cp.get("count", 0)
    total = len(remaining)
    
    for batch_start in range(0, total, batch_size):
        batch_keys = remaining[batch_start:batch_start + batch_size]
        batch_texts = [en_data[k] for k in batch_keys]
        
        # Translate batch
        results = translate_batch(batch_texts, lang_code)
        
        # Update data
        for k, new_val in zip(batch_keys, results):
            if new_val != data[k]:
                data[k] = new_val
                translated_count += 1
            completed.add(k)
        
        # Save progress
        with open(fpath, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
        
        progress = min(batch_start + batch_size, total)
        print(f"    {progress}/{total} (updated: {translated_count})")
    
    # Cleanup checkpoint
    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {lang_code}: Done! {translated_count} keys updated")
    return translated_count

def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--lang", help="Single language code")
    parser.add_argument("--all", action="store_true", help="All supported languages")
    parser.add_argument("--batch-size", type=int, default=20)
    parser.add_argument("--list", action="store_true")
    args = parser.parse_args()
    
    if args.list:
        print("Supported (33):", ", ".join(SUPPORTED_LANGS))
        print("Unsupported (13):", ", ".join(UNSUPPORTED_LANGS))
        print("Already complete: de, en")
        return
    
    en_data = load_en_data()
    
    if args.lang:
        if args.lang in SUPPORTED_LANGS:
            process_language(args.lang, en_data, args.batch_size)
        else:
            print(f"Language {args.lang} not supported by argostranslate")
            sys.exit(1)
    elif args.all:
        print(f"Translating {len(SUPPORTED_LANGS)} languages with Argos Translate...")
        total = 0
        for lang_code in SUPPORTED_LANGS:
            try:
                total += process_language(lang_code, en_data, args.batch_size)
            except KeyboardInterrupt:
                print("\nInterrupted - progress saved. Resume with same command.")
                break
            except Exception as e:
                print(f"  ERROR {lang_code}: {e}")
                continue
        print(f"\n=== COMPLETE: {total} keys translated ===")
    else:
        parser.print_help()

if __name__ == "__main__":
    main()