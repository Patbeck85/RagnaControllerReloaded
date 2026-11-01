#!/usr/bin/env python3
"""
Batch translate all locale files with rate limiting and retries.
"""
import json, os, urllib.request, urllib.parse, re, time, sys, random

locales_dir = "C:/Hermes/RagnaController/src/RagnaController/Locales"

with open(os.path.join(locales_dir, "en.json"), "r", encoding="utf-8") as f:
    en_data = json.load(f)

placeholder_keys = set()
for k, v in en_data.items():
    v_str = str(v)
    if re.search(r"\{[0-9a-zA-Z_]+\}", v_str) or re.search(r"%\w", v_str):
        placeholder_keys.add(k)

lang_map = {
    "ar": "ar", "bg": "bg", "bn": "bn", "cs": "cs", "da": "da", "de": "de",
    "el": "el", "en": "en", "es": "es", "fa": "fa", "fi": "fi", "fr": "fr",
    "gu": "gu", "he": "he", "hi": "hi", "hr": "hr", "hu": "hu", "id": "id",
    "it": "it", "ja": "ja", "kn": "kn", "ko": "ko", "ml": "ml", "mr": "mr",
    "ms": "ms", "my": "my", "nl": "nl", "no": "no", "or": "or", "pa": "pa",
    "pl": "pl", "pt": "pt", "ro": "ro", "ru": "ru", "sk": "sk", "sl": "sl",
    "sr": "sr", "sv": "sv", "ta": "ta", "te": "te", "th": "th", "tl": "tl",
    "tr": "tr", "uk": "uk", "ur": "ur", "vi": "vi", "zh": "zh", "zh-cn": "zh-CN"
}

lang_names = {
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

def translate_text(text, target_lang, source_lang="en", max_retries=3):
    if not text or text.strip() == "":
        return text
    if re.search(r"\{[0-9a-zA-Z_]+\}", text) or re.search(r"%\w", text):
        return text
    
    url = f"https://translate.googleapis.com/translate_a/single?client=gtx&sl={source_lang}&tl={target_lang}&dt=t&q=" + urllib.parse.quote(text)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    
    for attempt in range(max_retries):
        try:
            with urllib.request.urlopen(req, timeout=10) as response:
                res = json.loads(response.read().decode('utf-8'))
                translated = "".join([item[0] for item in res[0] if item[0]])
                return translated
        except urllib.error.HTTPError as e:
            if e.code == 429:  # Rate limited
                wait = (2 ** attempt) + random.uniform(1, 3)
                print(f"    Rate limited, waiting {wait:.1f}s (attempt {attempt+1}/{max_retries})")
                time.sleep(wait)
            else:
                print(f"    HTTP {e.code}: {e.reason}")
                time.sleep(1)
        except Exception as e:
            print(f"    Error: {e}")
            time.sleep(1)
    
    return text  # Return original on failure

def process_language(lang_code, target_lang, batch_size=20):
    fpath = os.path.join(locales_dir, f"{lang_code}.json")
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    untranslated = [k for k, v in en_data.items() if k in data and data[k] == v and k not in placeholder_keys]
    
    if not untranslated:
        print(f"  {lang_names[lang_code]} ({lang_code}): Already fully translated!")
        return 0
    
    print(f"  {lang_names[lang_code]} ({lang_code}): Translating {len(untranslated)} keys...")
    
    translated_count = 0
    for batch_start in range(0, len(untranslated), batch_size):
        batch = untranslated[batch_start:batch_start + batch_size]
        for k in batch:
            old_val = data[k]
            new_val = translate_text(en_data[k], target_lang)
            if new_val != old_val:
                data[k] = new_val
                translated_count += 1
        
        # Save progress
        with open(fpath, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        progress = min(batch_start + batch_size, len(untranslated))
        print(f"    Progress: {progress}/{len(untranslated)} ({translated_count} updated)")
        time.sleep(1.0)  # Rate limiting between batches
    
    print(f"  {lang_names[lang_code]}: Completed - {translated_count} keys updated")
    return translated_count

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--lang", help="Process single language code")
    parser.add_argument("--all", action="store_true", help="Process all languages")
    args = parser.parse_args()
    
    target_langs = [k for k in lang_map.keys() if k not in ["en", "de"]]
    
    if args.lang:
        if args.lang in lang_map:
            process_language(args.lang, lang_map[args.lang])
        else:
            print(f"Unknown language: {args.lang}")
    elif args.all:
        print(f"Processing {len(target_langs)} languages...")
        total = 0
        for lang_code in target_langs:
            try:
                total += process_language(lang_code, lang_map[lang_code])
            except KeyboardInterrupt:
                print("\nInterrupted by user")
                break
            except Exception as e:
                print(f"  ERROR processing {lang_code}: {e}")
                continue
        print(f"\n=== DONE ===")
        print(f"Total keys translated: {total}")
    else:
        print("Usage: python translate_locales.py --lang <code>  OR  --all")