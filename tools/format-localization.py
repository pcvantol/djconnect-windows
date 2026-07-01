#!/usr/bin/env python3
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RESOURCE_DIR = ROOT / "src" / "DJConnect.Windows" / "Resources"
RESOURCE_FILES = [
    RESOURCE_DIR / "Strings.resx",
    RESOURCE_DIR / "Strings.nl.resx",
    RESOURCE_DIR / "Strings.de.resx",
    RESOURCE_DIR / "Strings.fr.resx",
    RESOURCE_DIR / "Strings.es.resx",
]


def format_resx(path: Path) -> None:
    tree = ET.parse(path)
    ET.indent(tree, space="  ")
    tree.write(path, encoding="utf-8", xml_declaration=True)

    text = path.read_text(encoding="utf-8")
    text = text.replace("<?xml version='1.0' encoding='utf-8'?>", '<?xml version="1.0" encoding="utf-8"?>', 1)
    if not text.endswith("\n"):
        text += "\n"
    path.write_text(text, encoding="utf-8")


for resource_file in RESOURCE_FILES:
    format_resx(resource_file)

print(f"Formatted {len(RESOURCE_FILES)} localization files.")
