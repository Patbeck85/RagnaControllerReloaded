#!/usr/bin/env python3
"""
High-efficiency locale translator with checkpoint/resume capability.
Run with: python translate_locales_final.py [--lang CODE] [--all] [--batch-size N]
"""
import json, os, sys, time, argparse
from pathlib import Path

try:
    import requests
    REQUESTS_AVAILABLE = True
except ImportError:
    REQUESTS_AVAILABLE = False
    import urllib.request, urllib.parse

ROOT = Path("C:/Hermes/RagnaController")
LOCALES_DIR = ROOT / "src" / "RagnaController" / "Locales"
CHECKPOINT_DIR = ROOT / ".workspace_temp" / "translation_checkpoints"
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

# Load English reference
with open(LOCALES_DIR / "en.json", "r", encoding="utf-8") as f:
    EN_DATA = json.load(f)

# Keys with format placeholders to preserve
PLACEHOLDER_KEYS = set()
import re
for k, v in EN_DATA.items():
    v_str = str(v)
    if re.search(r"\{[0-9a-zA-Z_]+\}", v_str) or re.search(r"%\w", v_str):
        PLACEHOLDER_KEYS.add(k)

LANG_MAP = {
    "ar": "ar", "bg": "bg", "bn": "bn", "cs": "cs", "da": "da", "de": "de",
    "el": "el", "en": "en", "es": "es", "fa": "fa", "fi": "fi", "fr": "fr",
    "gu": "gu", "he": "he", "hi": "hi", "hr": "hr", "hu": "hu", "id": "id",
    "it": "it", "ja": "ja", "kn": "kn", "ko": "ko", "ml": "ml", "mr": "mr",
    "ms": "ms", "my": "my", "nl": "nl", "no": "no", "or": "or", "pa": "pa",
    "pl": "pl", "pt": "pt", "ro": "ro", "ru": "ru", "sk": "sk", "sl": "sl",
    "sr": "sr", "sv": "sv", "ta": "ta", "te": "te", "th": "th", "tl": "tl",
    "tr": "tr", "uk": "uk", "ur": "ur", "vi": "vi", "zh": "zh", "zh-cn": "zh-CN"
}

LANG_NAMES = {
    "ar": "Arabic", "bg": "Bulgarian", "bn": "Bengali", "cs": "Czech", "da": "Danish",
    "de": "German", "el": "Greek", "en": "English", "es": "Spanish", "fa": "Persian",
    "fi": "Finnish", "fr": "French", "gu": "Gujarati", "he": "Hebrew", "hi": "Hindi",
    "hr": "Croatian", "hu": "Hungarian", "id": "Indonesian", "it": "Italian", "ja": "Japanese",
    "kn": "Kannada", "ko": "Korean", "ml": "Malayalam", "mr": "Marathi", "ms": "Malay",
    "my": "Burmese", "nl": "Dutch", "no": "Norwegian", "or": "Odia", "pa": "Punjabi",
    "pl": "Polish", "pt": "Portuguese", "ro": "Romanian", "ru": "Russian", "sk": "Slovak",
    "sl": "Slovenian", "sr": "Serbian", "sv": "Swedish", "ta": "Tamil", "te": "Telugu",
    "th": "Thai", "tl": "Tagalog", "tr": "Turkish", "uk": "Ukrainian", "ur": "Urdu",
    "vi": "Vietnamese", "zh": "Chinese (Traditional)", "zh-cn": "Chinese (Simplified)"
}

class Translator:
    def __init__(self, use_requests=True):
        self.use_requests = use_requests and REQUESTS_AVAILABLE
        self.session = requests.Session() if self.use_requests else None
        if self.session:
            self.session.headers.update({"User-Agent": "Mozilla/5.0"})
    
    def translate(self, text, target_lang, source_lang="en", max_retries=3):
        if not text or text.strip() == "":
            return text
        if re.search(r"\{[0-9a-zA-Z_]+\}", text) or re.search(r"%\w", text):
            return text
        
        url = "https://translate.googleapis.com/translate_a/single"
        params = {"client": "gtx", "sl": source_lang, "tl": target_lang, "dt": "t", "q": text}
        
        for attempt in range(max_retries):
            try:
                if self.use_requests:
                    r = self.session.get(url, params=params, timeout=10)
                    r.raise_for_status()
                    res = r.json()
                else:
                    q = urllib.parse.urlencode(params)
                    full_url = f"{url}?{q}"
                    req = urllib.request.Request(full_url, headers={"User-Agent": "Mozilla/5.0"})
                    with urllib.request.urlopen(req, timeout=10) as response:
                        res = json.loads(response.read().decode('utf-8'))
                
                translated = "".join([item[0] for item in res[0] if item[0]])
                return translated
                
            except Exception as e:
                if "429" in str(e) or "Too Many" in str(e):
                    wait = min(2 ** attempt + 5, 60)
                    print(f"      Rate limited, waiting {wait}s...")
                    time.sleep(wait)
                else:
                    wait = 2 ** attempt
                    print(f"      Error (attempt {attempt+1}): {e}, retrying in {wait}s...")
                    time.sleep(wait)
        
        return text

def get_checkpoint_path(lang_code):
    return CHECKPOINT_DIR / f"{lang_code}_progress.json"

def load_checkpoint(lang_code):
    cp = get_checkpoint_path(lang_code)
    if cp.exists():
        with open(cp, "r", encoding="utf-8") as f:
            return json.load(f)
    return {"completed_keys": [], "translated_count": 0, "total_keys": 0}

def save_checkpoint(lang_code, data):
    cp = get_checkpoint_path(lang_code)
    with open(cp, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

def process_language(lang_code, target_lang, batch_size=15, translator=None):
    if translator is None:
        translator = Translator()
    
    fpath = LOCALES_DIR / f"{lang_code}.json"
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    untranslated = [k for k, v in EN_DATA.items() 
                    if k in data and data[k] == v and k not in PLACEHOLDER_KEYS]
    
    if not untranslated:
        print(f"  {LANG_NAMES[lang_code]} ({lang_code}): Already fully translated!")
        return 0
    
    # Load checkpoint
    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed_keys", []))
    remaining = [k for k in untranslated if k not in completed]
    
    print(f"  {LANG_NAMES[lang_code]} ({lang_code}): {len(remaining)}/{len(untranslated)} keys remaining")
    
    if not remaining:
        print(f"  Already completed (checkpoint shows done)")
        return cp.get("translated_count", 0)
    
    translated_count = cp.get("translated_count", 0)
    total_remaining = len(remaining)
    
    for batch_start in range(0, total_remaining, batch_size):
        batch = remaining[batch_start:batch_start + batch_size]
        
        for k in batch:
            new_val = translator.translate(EN_DATA[k], target_lang)
            if new_val != data[k]:
                data[k] = new_val
                translated_count += 1
            completed.add(k)
        
        # Save main file
        with open(fpath, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        # Save checkpoint
        save_checkpoint(lang_code, {
            "completed_keys": list(completed),
            "translated_count": translated_count,
            "total_keys": len(untranslated)
        })
        
        progress = min(batch_start + batch_size, total_remaining)
        print(f"    {progress}/{total_remaining} (updated: {translated_count})")
        
        # Rate limiting
        time.sleep(2.0)
    
    # Clean up checkpoint
    cp_file = get_checkpoint_path(lang_code)
    if cp_file.exists():
        cp_file.unlink()
    
    print(f"  {LANG_NAMES[lang_code]}: Done! {translated_count} total keys updated")
    return translated_count

def main():
    parser = argparse.ArgumentParser(description="Translate RagnaController locale files")
    parser.add_argument("--lang", help="Process single language code (e.g., nl, fr, es)")
    parser.add_argument("--all", action="store_true", help="Process all non-en/de languages")
    parser.add_argument("--batch-size", type=int, default=15, help="Keys per batch (default: 15)")
    parser.add_argument("--list", action="store_true", help="List available languages")
    args = parser.parse_args()
    
    if args.list:
        print("Available languages:")
        for k, v in LANG_NAMES.items():
            status = " (SKIP - source)" if k in ["en", "de"] else ""
            print(f"  {k}: {LANG_NAMES[k]}{status}")
        return
    
    target_langs = [k for k in LANG_MAP.keys() if k not in ["en", "de"]]
    
    if args.lang:
        if args.lang in LANG_MAP:
            process_language(args.lang, LANG_MAP[args.lang], args.batch_size)
        else:
            print(f"Unknown language: {args.lang}")
            sys.exit(1)
    elif args.all:
        print(f"Processing {len(target_langs)} languages...")
        translator = Translator()
        total = 0
        for lang_code in target_langs:
            try:
                total += process_language(lang_code, LANG_MAP[lang_code], args.batch_size, translator)
            except KeyboardInterrupt:
                print("\nInterrupted! Progress saved. Resume with same command.")
                break
            except Exception as e:
                print(f"  ERROR processing {lang_code}: {e}")
                continue
        print(f"\n=== COMPLETE ===")
        print(f"Total keys translated: {total}")
    else:
        parser.print_help()

if __name__ == "__main__":
    main()