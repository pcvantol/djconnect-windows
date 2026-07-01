#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_DIR = ROOT / "src" / "DJConnect.Windows" / "Resources"
SUPPORTED = ["en", "nl", "de", "fr", "es"]


PLACEHOLDER = re.compile(r"\{[0-9]+(?::[^}]*)?\}")


def validate_formatting(path: Path) -> None:
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    if not lines or lines[0] != '<?xml version="1.0" encoding="utf-8"?>':
        raise ValueError(f"{path.relative_to(ROOT)} must use the standard XML declaration. Run tools/format-localization.py.")
    if "</data><data" in text:
        raise ValueError(f"{path.relative_to(ROOT)} has collapsed data elements. Run tools/format-localization.py.")
    for index, line in enumerate(lines, start=1):
        if "<data name=" in line and not line.startswith("  <data name="):
            raise ValueError(
                f"{path.relative_to(ROOT)} has an unformatted data element on line {index}. "
                "Run tools/format-localization.py."
            )


def load_resources(path: Path) -> dict[str, str]:
    validate_formatting(path)
    tree = ET.parse(path)
    resources: dict[str, str] = {}
    keys_casefolded: dict[str, str] = {}
    duplicates: list[str] = []
    case_duplicates: list[str] = []
    for node in tree.getroot().findall("data"):
        if "name" not in node.attrib:
            continue
        key = node.attrib["name"]
        if key in resources:
            duplicates.append(key)
        folded = key.casefold()
        if folded in keys_casefolded and keys_casefolded[folded] != key:
            case_duplicates.append(f"{keys_casefolded[folded]} / {key}")
        keys_casefolded[folded] = key
        value_node = node.find("value")
        resources[key] = "" if value_node is None or value_node.text is None else value_node.text
    if duplicates:
        raise ValueError(f"{path.relative_to(ROOT)} has duplicate keys: {', '.join(sorted(duplicates))}")
    if case_duplicates:
        raise ValueError(
            f"{path.relative_to(ROOT)} has case-insensitive duplicate keys: "
            + ", ".join(sorted(case_duplicates))
        )
    return resources


def placeholders(value: str) -> set[str]:
    return set(PLACEHOLDER.findall(value))


base = RESOURCE_DIR / "Strings.resx"
files = {"en": base}
files.update({locale: RESOURCE_DIR / f"Strings.{locale}.resx" for locale in SUPPORTED if locale != "en"})

missing_files = [str(path.relative_to(ROOT)) for path in files.values() if not path.exists()]
if missing_files:
    print("Missing localization files:")
    for path in missing_files:
        print(f"  - {path}")
    sys.exit(1)

resources_by_locale = {locale: load_resources(path) for locale, path in files.items()}
keys_by_locale = {locale: set(resources.keys()) for locale, resources in resources_by_locale.items()}
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
    if locale == "en":
        continue

    placeholder_mismatches = []
    for key in sorted(keys_by_locale["en"] & keys_by_locale[locale]):
        expected = placeholders(resources_by_locale["en"][key])
        actual = placeholders(resources_by_locale[locale][key])
        if expected != actual:
            placeholder_mismatches.append((key, expected, actual))
    if placeholder_mismatches:
        failed = True
        print(f"{locale}: {len(placeholder_mismatches)} placeholder mismatch(es)")
        for key, expected, actual in placeholder_mismatches:
            print(f"  - {key}: expected {sorted(expected)}, found {sorted(actual)}")

if failed:
    sys.exit(1)

print(f"Localization OK: {len(all_keys)} keys in {', '.join(SUPPORTED)}.")
