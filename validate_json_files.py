#!/usr/bin/env python3
import json
import glob
import sys

errors = 0
for f in sorted(glob.glob('Locales/*.json')):
    try:
        json.load(open(f))
    except json.JSONDecodeError as e:
        print(f'{f}: {e}')
        errors += 1

print(f'Total JSON errors: {errors}')
sys.exit(0 if errors == 0 else 1)
