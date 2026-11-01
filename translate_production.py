#!/usr/bin/env python3
"""
RagnaController Locale Translation - Production Version

This script supports multiple translation backends:
1. Google Cloud Translation API (recommended for production - high limits)
2. LibreTranslate (self-hosted or public instances)
3. Local offline models (via transformers/ctranslate2)

Usage:
  python translate_production.py --backend google-cloud --lang nl
  python translate_production.py --backend libretranslate --url https://libretranslate.de --all
  python translate_production.py --backend local --model facebook/nllb-200-distilled-600M --all
"""
import json, os, sys, time, argparse, re
from pathlib import Path

ROOT = Path("C:/Hermes/RagnaController")
LOCALES_DIR = ROOT / "src" / "RagnaController" / "Locales"
CHECKPOINT_DIR = ROOT / ".workspace_temp" / "translation_checkpoints"
CHECKPOINT_DIR.mkdir(parents=True, exist_ok=True)

with open(LOCALES_DIR / "en.json", "r", encoding="utf-8") as f:
    EN_DATA = json.load(f)

PLACEHOLDER_KEYS = set()
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

# ============================================================
# BACKEND 1: Google Cloud Translation API (requires API key)
# ============================================================
class GoogleCloudTranslator:
    def __init__(self, api_key, project_id=None):
        self.api_key = api_key
        self.project_id = project_id
        self.base_url = "https://translation.googleapis.com/language/translate/v2"
    
    def translate(self, text, target_lang, source_lang="en"):
        if not text or text.strip() == "":
            return text
        if re.search(r"\{[0-9a-zA-Z_]+\}", text) or re.search(r"%\w", text):
            return text
        
        import requests
        params = {"key": self.api_key, "q": text, "target": target_lang, "source": source_lang, "format": "text"}
        r = requests.post(self.base_url, params=params, timeout=30)
        r.raise_for_status()
        return r.json()["data"]["translations"][0]["translatedText"]

# ============================================================
# BACKEND 2: LibreTranslate (self-hosted or public)
# ============================================================
class LibreTranslateTranslator:
    def __init__(self, base_url, api_key=None):
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
    
    def translate(self, text, target_lang, source_lang="en"):
        if not text or text.strip() == "":
            return text
        if re.search(r"\{[0-9a-zA-Z_]+\}", text) or re.search(r"%\w", text):
            return text
        
        import requests
        data = {"q": text, "source": source_lang, "target": target_lang, "format": "text"}
        if self.api_key:
            data["api_key"] = self.api_key
        r = requests.post(f"{self.base_url}/translate", json=data, timeout=30)
        r.raise_for_status()
        return r.json()["translatedText"]

# ============================================================
# BACKEND 3: Local NLLB Model (offline, high quality)
# ============================================================
class LocalNLLBTranslator:
    """Uses facebook/nllb-200-distilled-600M for offline translation"""
    
    NLLB_LANG_MAP = {
        "ar": "ara_Arab", "bg": "bul_Cyrl", "bn": "ben_Beng", "cs": "ces_Latn",
        "da": "dan_Latn", "de": "deu_Latn", "el": "ell_Grek", "en": "eng_Latn",
        "es": "spa_Latn", "fa": "pes_Arab", "fi": "fin_Latn", "fr": "fra_Latn",
        "gu": "guj_Gujr", "he": "heb_Hebr", "hi": "hin_Deva", "hr": "hrv_Latn",
        "hu": "hun_Latn", "id": "ind_Latn", "it": "ita_Latn", "ja": "jpn_Jpan",
        "kn": "kan_Knda", "ko": "kor_Hang", "ml": "mal_Mlym", "mr": "mar_Deva",
        "ms": "zsm_Latn", "my": "mya_Mymr", "nl": "nld_Latn", "no": "nob_Latn",
        "or": "ory_Orya", "pa": "pan_Guru", "pl": "pol_Latn", "pt": "por_Latn",
        "ro": "ron_Latn", "ru": "rus_Cyrl", "sk": "slk_Latn", "sl": "slv_Latn",
        "sr": "srp_Cyrl", "sv": "swe_Latn", "ta": "tam_Taml", "te": "tel_Telu",
        "th": "tha_Thai", "tl": "tgl_Latn", "tr": "tur_Latn", "uk": "ukr_Cyrl",
        "ur": "urd_Arab", "vi": "vie_Latn", "zh": "zho_Hant", "zh-cn": "zho_Hans"
    }
    
    def __init__(self, model_name="facebook/nllb-200-distilled-600M", device="auto"):
        try:
            import torch
            from transformers import AutoTokenizer, AutoModelForSeq2SeqLM
            
            if device == "auto":
                device = "cuda" if torch.cuda.is_available() else "cpu"
            
            print(f"Loading {model_name} on {device}...")
            self.tokenizer = AutoTokenizer.from_pretrained(model_name)
            self.model = AutoModelForSeq2SeqLM.from_pretrained(
                model_name, 
                torch_dtype=torch.float16 if device == "cuda" else torch.float32,
                device_map=device if device == "cuda" else None
            )
            if device == "cpu":
                self.model = self.model.to(device)
            self.device = device
            self.src_lang = "eng_Latn"
            print("Model loaded successfully!")
        except ImportError:
            raise RuntimeError("transformers and torch required: pip install transformers torch accelerate")
    
    def translate(self, text, target_lang, source_lang="en"):
        if not text or text.strip() == "":
            return text
        if re.search(r"\{[0-9a-zA-Z_]+\}", text) or re.search(r"%\w", text):
            return text
        
        import torch
        tgt_lang = self.NLLB_LANG_MAP.get(target_lang, target_lang)
        
        self.tokenizer.src_lang = self.src_lang
        inputs = self.tokenizer(text, return_tensors="pt", padding=True, truncation=True, max_length=512).to(self.device)
        
        with torch.no_grad():
            translated_tokens = self.model.generate(
                **inputs,
                forced_bos_token_id=self.tokenizer.convert_tokens_to_ids(tgt_lang),
                max_length=512,
                num_beams=4,
                early_stopping=True
            )
        
        return self.tokenizer.batch_decode(translated_tokens, skip_special_tokens=True)[0]

# ============================================================
# CHECKPOINT MANAGEMENT
# ============================================================
def get_checkpoint_path(lang_code):
    return CHECKPOINT_DIR / f"{lang_code}_progress.json"

def load_checkpoint(lang_code):
    cp = get_checkpoint_path(lang_code)
    if cp.exists():
        with open(cp, "r", encoding="utf-8") as f:
            return json.load(f)
    return {"completed_keys": [], "translated_count": 0}

def save_checkpoint(lang_code, data):
    cp = get_checkpoint_path(lang_code)
    with open(cp, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)

# ============================================================
# MAIN PROCESSING
# ============================================================
def process_language(lang_code, target_lang, translator, batch_size=20):
    fpath = LOCALES_DIR / f"{lang_code}.json"
    with open(fpath, "r", encoding="utf-8") as f:
        data = json.load(f)
    
    untranslated = [k for k, v in EN_DATA.items() 
                    if k in data and data[k] == v and k not in PLACEHOLDER_KEYS]
    
    if not untranslated:
        print(f"  {LANG_NAMES[lang_code]} ({lang_code}): Already fully translated!")
        return 0
    
    cp = load_checkpoint(lang_code)
    completed = set(cp.get("completed_keys", []))
    remaining = [k for k in untranslated if k not in completed]
    
    print(f"  {LANG_NAMES[lang_code]} ({lang_code}): {len(remaining)}/{len(untranslated)} remaining")
    
    if not remaining:
        print(f"  Already completed (from checkpoint)")
        return cp.get("translated_count", 0)
    
    translated_count = cp.get("translated_count", 0)
    total = len(remaining)
    
    for batch_start in range(0, total, batch_size):
        batch = remaining[batch_start:batch_start + batch_size]
        
        for k in batch:
            try:
                new_val = translator.translate(EN_DATA[k], target_lang)
                if new_val != data[k]:
                    data[k] = new_val
                    translated_count += 1
                completed.add(k)
            except Exception as e:
                print(f"    Error translating {k}: {e}")
                completed.add(k)  # Skip on error
        
        # Save progress
        with open(fpath, "w", encoding="utf-8") as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        save_checkpoint(lang_code, {
            "completed_keys": list(completed),
            "translated_count": translated_count
        })
        
        progress = min(batch_start + batch_size, total)
        print(f"    {progress}/{total} (updated: {translated_count})")
    
    # Cleanup checkpoint
    get_checkpoint_path(lang_code).unlink(missing_ok=True)
    print(f"  {LANG_NAMES[lang_code]}: Done! {translated_count} keys updated")
    return translated_count

def get_translator(backend, **kwargs):
    if backend == "google-cloud":
        api_key = kwargs.get("api_key") or os.getenv("GOOGLE_TRANSLATE_API_KEY")
        if not api_key:
            raise ValueError("Google Cloud API key required (--api-key or GOOGLE_TRANSLATE_API_KEY env)")
        return GoogleCloudTranslator(api_key)
    elif backend == "libretranslate":
        url = kwargs.get("url") or "https://libretranslate.de"
        api_key = kwargs.get("api_key")
        return LibreTranslateTranslator(url, api_key)
    elif backend == "local":
        model = kwargs.get("model", "facebook/nllb-200-distilled-600M")
        device = kwargs.get("device", "auto")
        return LocalNLLBTranslator(model, device)
    else:
        raise ValueError(f"Unknown backend: {backend}")

def main():
    parser = argparse.ArgumentParser(description="Production-grade locale translation")
    parser.add_argument("--backend", choices=["google-cloud", "libretranslate", "local"], 
                        default="libretranslate", help="Translation backend")
    parser.add_argument("--api-key", help="API key for Google Cloud or LibreTranslate")
    parser.add_argument("--url", help="LibreTranslate base URL (default: https://libretranslate.de)")
    parser.add_argument("--model", help="Local model name (default: facebook/nllb-200-distilled-600M)")
    parser.add_argument("--device", choices=["auto", "cpu", "cuda"], default="auto")
    parser.add_argument("--lang", help="Single language code")
    parser.add_argument("--all", action="store_true", help="All languages")
    parser.add_argument("--batch-size", type=int, default=20)
    parser.add_argument("--list", action="store_true")
    args = parser.parse_args()
    
    if args.list:
        for k, v in LANG_NAMES.items():
            print(f"  {k}: {v}{' (source)' if k in ['en','de'] else ''}")
        return
    
    target_langs = [k for k in LANG_MAP.keys() if k not in ["en", "de"]]
    
    if args.lang:
        if args.lang not in LANG_MAP:
            print(f"Unknown: {args.lang}")
            sys.exit(1)
        target_langs = [args.lang]
    elif not args.all:
        parser.print_help()
        return
    
    print(f"Using backend: {args.backend}")
    translator = get_translator(args.backend, api_key=args.api_key, url=args.url, 
                                model=args.model, device=args.device)
    
    total = 0
    for lang_code in target_langs:
        try:
            total += process_language(lang_code, LANG_MAP[lang_code], translator, args.batch_size)
        except KeyboardInterrupt:
            print("\nInterrupted - progress saved. Resume with same command.")
            break
        except Exception as e:
            print(f"  ERROR {lang_code}: {e}")
            continue
    
    print(f"\n=== COMPLETE: {total} keys translated ===")

if __name__ == "__main__":
    main()