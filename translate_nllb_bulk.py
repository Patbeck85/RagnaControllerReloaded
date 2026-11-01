#!/usr/bin/env python3
"""
NLLB-200 bulk translator - loads model once, processes all languages.
Run with: C:/Python314/python.exe translate_nllb_bulk.py
"""
import json, os, re, time
from pathlib import Path

LOCALES_DIR = Path("C:/Hermes/RagnaController/src/RagnaController/Locales")
CHECKPOINT_DIR = Path("C:/Hermes/RagnaController/.workspace_temp/nllb_checkpoints")
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

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

def main():
    import torch
    from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
    
    print("Loading NLLB-200 model...")
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
    print(f"Model loaded on {device}")
    
    en_data = load_en_data()
    
    total_all = 0
    for lang_code, nllb_code in NLLB_LANGS.items():
        fpath = LOCALES_DIR / f"{lang_code}.json"
        with open(fpath, "r", encoding="utf-8") as f:
            data = json.load(f)
        
        untranslated = [k for k, v in en_data.items() 
                        if k in data and data[k] == v and not PLACEHOLDER_PATTERN.search(str(v))]
        
        if not untranslated:
            print(f"  {lang_code}: Already fully translated")
            continue
        
        cp = load_checkpoint(lang_code)
        completed = set(cp.get("completed", []))
        remaining = [k for k in untranslated if k not in completed]
        
        if not remaining:
            print(f"  {lang_code}: Done (checkpoint: {cp.get('count', 0)} keys)")
            total_all += cp.get("count", 0)
            continue
        
        print(f"\n{lang_code} ({nllb_code}): {len(remaining)}/{len(untranslated)} keys to translate")
        
        translated_count = cp.get("count", 0)
        total = len(remaining)
        batch_size = 10
        
        tokenizer.src_lang = "eng_Latn"
        forced_bos = tokenizer.convert_tokens_to_ids(nllb_code)
        
        for batch_start in range(0, total, batch_size):
            batch_keys = remaining[batch_start:batch_start + batch_size]
            batch_texts = [en_data[k] for k in batch_keys]
            
            # Filter texts that need translation
            to_translate = []
            translate_indices = []
            for i, text in enumerate(batch_texts):
                if not text or text.strip() == "":
                    continue
                if PLACEHOLDER_PATTERN.search(text):
                    continue
                to_translate.append(text)
                translate_indices.append(i)
            
            if to_translate:
                inputs = tokenizer(to_translate, return_tensors="pt", padding=True, truncation=True, max_length=512).to(device)
                with torch.no_grad():
                    tokens = model.generate(
                        **inputs,
                        forced_bos_token_id=forced_bos,
                        max_length=512,
                        num_beams=4,
                        early_stopping=True
                    )
                results = tokenizer.batch_decode(tokens, skip_special_tokens=True)
            else:
                results = []
            
            # Map results back
            result_map = {idx: res for idx, res in zip(translate_indices, results)}
            final_results = [result_map.get(i, batch_texts[i]) for i in range(len(batch_texts))]
            
            for k, new_val in zip(batch_keys, final_results):
                if new_val != data[k]:
                    data[k] = new_val
                    translated_count += 1
                completed.add(k)
            
            # Save progress
            with open(fpath, "w", encoding="utf-8") as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            save_checkpoint(lang_code, {"completed": list(completed), "count": translated_count})
            
            progress = min(batch_start + batch_size, total)
            print(f"  {progress}/{total} (updated: {translated_count})")
        
        get_checkpoint_path(lang_code).unlink(missing_ok=True)
        total_all += translated_count
        print(f"  {lang_code}: DONE! {translated_count} keys updated")
    
    print(f"\n=== ALL COMPLETE: {total_all} keys translated ===")

if __name__ == "__main__":
    main()