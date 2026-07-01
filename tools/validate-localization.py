#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_DIR = ROOT / "src" / "DJConnect.Windows" / "Resources"
SUPPORTED = ["en", "nl", "de", "fr", "es"]


def load_keys(path: Path) -> set[str]:
    tree = ET.parse(path)
    return {
        node.attrib["name"]
        for node in tree.getroot().findall("data")
        if "name" in node.attrib
    }


base = RESOURCE_DIR / "Strings.resx"
files = {"en": base}
files.update({locale: RESOURCE_DIR / f"Strings.{locale}.resx" for locale in SUPPORTED if locale != "en"})

missing_files = [str(path.relative_to(ROOT)) for path in files.values() if not path.exists()]
if missing_files:
    print("Missing localization files:")
    for path in missing_files:
        print(f"  - {path}")
    sys.exit(1)

keys_by_locale = {locale: load_keys(path) for locale, path in files.items()}
all_keys = set().union(*keys_by_locale.values())
failed = False

for locale in SUPPORTED:
    missing = sorted(all_keys - keys_by_locale[locale])
    extra = sorted(keys_by_locale[locale] - keys_by_locale["en"])
    if missing:
        failed = True
        print(f"{locale}: missing {len(missing)} key(s)")
        for key in missing:
            print(f"  - {key}")
    if extra:
        failed = True
        print(f"{locale}: extra {len(extra)} key(s) not present in en")
        for key in extra:
            print(f"  - {key}")

if failed:
    sys.exit(1)

print(f"Localization OK: {len(all_keys)} keys in {', '.join(SUPPORTED)}.")
