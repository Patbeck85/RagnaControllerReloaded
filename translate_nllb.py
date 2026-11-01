#!/usr/bin/env python3
"""
Translate missing languages using NLLB-200 directly (offline, no API limits).
NLLB-200 supports 200 languages including all 13 missing ones.
"""
import json, os, re, subprocess, sys, time
from pathlib import Path

PYTHON = "C:/Python314/python.exe"

LOCALES_DIR = Path("C:/Hermes/RagnaController/src/RagnaController/Locales")
CHECKPOINT_DIR = Path("C:/Hermes/RagnaController/.workspace_temp/nllb_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

# Missing languages supported by NLLB-200
NLLB_LANGS = {
    "gu": "guj_Gujr",    # Gujarati
    "hr": "hrv_Latn",    # Croatian
    "kn": "kan_Knda",    # Kannada
    "ml": "mal_Mlym",    # Malayalam
    "mr": "mar_Deva",    # Marathi
    "my": "mya_Mymr",    # Burmese
    "no": "nob_Latn",    # Norwegian Bokmål
    "or": "ory_Orya",    # Odia
    "pa": "pan_Guru",    # Punjabi
    "sr": "srp_Cyrl",    # Serbian
    "ta": "tam_Taml",    # Tamil
    "te": "tel_Telu",    # Telugu
    "zh-cn": "zho_Hans", # Chinese Simplified
}

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

def translate_batch_nllb(texts, target_nllb_code, max_retries=3):
    """Translate batch using NLLB-200 via transformers."""
    script = f'''
import json, sys, re, torch
from transformers import AutoTokenizer, AutoModelForSeq2SeqLM

texts = {json.dumps(texts, ensure_ascii=False)}
target = "{target_nllb_code}"

# Load model (cached after first run)
MODEL = "facebook/nllb-200-distilled-600M"
tokenizer = AutoTokenizer.from_pretrained(MODEL)
model = AutoModelForSeq2SeqLM.from_pretrained(
    MODEL, 
    torch_dtype=torch.float16 if torch.cuda.is_available() else torch.float32,
    device_map="auto" if torch.cuda.is_available() else None
)
device = "cuda" if torch.cuda.is_available() else "cpu"
if device == "cpu":
    model = model.to(device)

tokenizer.src_lang = "eng_Latn"
results = []
for text in texts:
    if not text or text.strip() == "":
        results.append(text)
        continue
    if re.search(r"\\{{[0-9a-zA-Z_]+\\}}", text) or re.search(r"%\\w", text):
        results.append(text)
        continue
    try:
        inputs = tokenizer(text, return_tensors="pt", padding=True, truncation=True, max_length=512).to(device)
        with torch.no_grad():
            tokens = model.generate(
                **inputs,
                forced_bos_token_id=tokenizer.convert_tokens_to_ids(target),
                max_length=512,
                num_beams=4,
                early_stopping=True
            )
        result = tokenizer.batch_decode(tokens, skip_special_tokens=True)[0]
        results.append(result)
    except Exception as e:
        results.append(text)

print(json.dumps(results, ensure_ascii=False))
'''
    for attempt in range(max_retries):
        try:
            result = subprocess.run(
                [PYTHON, "-c", script],
                capture_output=True, text=True, timeout=120, encoding="utf-8"
            )
            if result.returncode == 0:
                return json.loads(result.stdout.strip())
            else:
                print(f"  NLLB error (attempt {attempt+1}): {result.stderr[:300]}")
        except Exception as e:
            print(f"  NLLB exception (attempt {attempt+1}): {e}")
        time.sleep(5)
    return texts

def process_language(lang_code, nllb_code, en_data, batch_size=10):
    fpath = LOCALES_DIR / f"{lang_code}.json"
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    untranslated = [k for k, v in en_data.items() 
                    if k in data and data[k] == v and not PLACEHOLDER_PATTERN.search(str(v))]
    
    if not untranslated:
        print(f"  {lang_code}: Already fully translated")
        return 0
    
    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed", []))
    remaining = [k for k in untranslated if k not in completed]
    
    if not remaining:
        print(f"  {lang_code}: Done (checkpoint)")
        return cp.get("count", 0)
    
    print(f"  {lang_code} ({nllb_code}): {len(remaining)}/{len(untranslated)} keys")
    
    translated_count = cp.get("count", 0)
    total = len(remaining)
    
    for batch_start in range(0, total, batch_size):
        batch_keys = remaining[batch_start:batch_start + batch_size]
        batch_texts = [en_data[k] for k in batch_keys]
        
        results = translate_batch_nllb(batch_texts, nllb_code)
        
        for k, new_val in zip(batch_keys, results):
            if new_val != data[k]:
                data[k] = new_val
                translated_count += 1
            completed.add(k)
        
        with open(fpath, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
        
        progress = min(batch_start + batch_size, total)
        print(f"    {progress}/{total} (updated: {translated_count})")
    
    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {lang_code}: Done! {translated_count} keys updated")
    return translated_count

def main():
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument("--lang", help="Single language code")
    parser.add_argument("--all", action="store_true", help="All NLLB languages")
    parser.add_argument("--batch-size", type=int, default=10)
    parser.add_argument("--list", action="store_true")
    args = parser.parse_args()
    
    if args.list:
        print("NLLB-supported missing languages:")
        for k, v in NLLB_LANGS.items():
            print(f"  {k}: {v}")
        return
    
    en_data = load_en_data()
    
    if args.lang:
        if args.lang in NLLB_LANGS:
            process_language(args.lang, NLLB_LANGS[args.lang], en_data, args.batch_size)
        else:
            print(f"Language {args.lang} not in NLLB map")
            sys.exit(1)
    elif args.all:
        print(f"Translating {len(NLLB_LANGS)} languages with NLLB-200...")
        total = 0
        for lang_code, nllb_code in NLLB_LANGS.items():
            try:
                total += process_language(lang_code, nllb_code, en_data, args.batch_size)
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