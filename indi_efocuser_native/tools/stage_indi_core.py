#!/usr/bin/env python3
"""Stage the EFucoser driver into a clean indilib/indi checkout."""

from __future__ import annotations

import argparse
import shutil
from pathlib import Path


CMAKE_BLOCK = """

# ############### EFucoser Focuser ################
set(efucoser_SRC
    efucoser.cpp
    efucoser_protocol.cpp)

add_executable(indi_efucoser_focuser ${efucoser_SRC})
target_link_libraries(indi_efucoser_focuser indidriver)
install(TARGETS indi_efucoser_focuser RUNTIME DESTINATION bin)
"""

DRIVER_XML_BLOCK = """        <device label="EFucoser Focuser" manufacturer="EFucoser">
            <driver name="EFucoser Focuser">indi_efucoser_focuser</driver>
            <version>1.0</version>
        </device>
"""


def require_file(path: Path) -> None:
    if not path.is_file():
        raise SystemExit(f"Required INDI file is missing: {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("indi_checkout", type=Path)
    args = parser.parse_args()

    checkout = args.indi_checkout.resolve()
    source = Path(__file__).resolve().parents[1]
    focuser_dir = checkout / "drivers" / "focuser"
    cmake_file = focuser_dir / "CMakeLists.txt"
    drivers_xml = checkout / "drivers.xml"
    require_file(cmake_file)
    require_file(drivers_xml)

    for filename in ("efucoser.cpp", "efucoser.h", "efucoser_protocol.cpp", "efucoser_protocol.h"):
        shutil.copy2(source / filename, focuser_dir / filename)

    doc_dir = focuser_dir / "doc" / "efucoser"
    doc_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source / "doc" / "index.md", doc_dir / "index.md")

    cmake_text = cmake_file.read_text(encoding="utf-8")
    if "add_executable(indi_efucoser_focuser" not in cmake_text:
        cmake_file.write_text(
            cmake_text.rstrip() + "\n\n" + CMAKE_BLOCK.strip() + "\n",
            encoding="utf-8",
            newline="\n",
        )

    xml_text = drivers_xml.read_text(encoding="utf-8")
    if "<driver name=\"EFucoser Focuser\">" not in xml_text:
        marker = '    <devGroup group="Focusers">\n'
        if marker not in xml_text:
            raise SystemExit("Focusers group was not found in drivers.xml")
        xml_text = xml_text.replace(marker, marker + DRIVER_XML_BLOCK, 1)
        drivers_xml.write_text(xml_text, encoding="utf-8", newline="\n")

    print(f"EFucoser staged in {checkout}")


if __name__ == "__main__":
    main()
