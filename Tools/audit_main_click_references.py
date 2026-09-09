"""Resolve native Main click GOT references using the supplied ELF and Il2Cpp dump."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("reverse_root", type=Path)
parser.add_argument("output", type=Path)
parser.add_argument("--function", default="23bd4e8", help="Game function RVA in hexadecimal")
args = parser.parse_args()
root = args.reverse_root
elf_path = root / "exports/com.sfflogdstudio.dragonlegend-20260907/unpacked/split_config.arm64_v8a/lib/arm64-v8a/libil2cpp.so"
assert re.fullmatch(r"[0-9a-fA-F]+", args.function), "Expected hexadecimal RVA"
source_path = root / f"reconstruction/mumu-current/native-functions/game/{args.function.lower()}.c"
script_path = root / "reconstruction/mumu-current/analysis/script.json"
elf = elf_path.read_bytes()
assert elf[:6] == b"\x7fELF\x02\x01", "Expected little-endian ELF64"
source = source_path.read_text(encoding="utf-8")
script = json.loads(script_path.read_text(encoding="utf-8-sig"))
metadata = {row["Address"]: {"kind": kind, "value": row.get("Name", row.get("Value"))}
            for kind in ("ScriptString", "ScriptMetadata", "ScriptMetadataMethod")
            for row in script[kind]}
section_offset = struct.unpack_from("<Q", elf, 40)[0]
section_size, section_count = struct.unpack_from("<HH", elf, 58)
relocations = {}
for index in range(section_count):
    section = struct.unpack_from("<IIQQQQIIQQ", elf, section_offset + index * section_size)
    if section[1] != 4:  # SHT_RELA
        continue
    assert section[9] >= 24
    for offset in range(section[4], section[4] + section[5], section[9]):
        address, info, addend = struct.unpack_from("<QQq", elf, offset)
        if info & 0xffffffff == 1027:  # R_AARCH64_RELATIVE
            relocations[address] = addend
references = []
for address in sorted(set(re.findall(r"PTR_DAT_([0-9a-f]+)", source))):
    target = relocations.get(int(address, 16) - 0x100000)
    if target in metadata:
        references.append({"ghidra_pointer": address, "metadata_address": hex(target), **metadata[target]})
by_pointer = {row["ghidra_pointer"]: row["value"] for row in references}
if args.function.lower() == "23bd4e8":
    assert by_pointer["0501e330"] == "CashOutb"
    assert by_pointer["0501e318"] == "WithdrawBtn"
    assert by_pointer["0501bcf0"] == "Method$Game.UI.UIManager.ShowWindow<UICashOutView>()"
args.output.write_text(json.dumps({"function_rva": "0x" + args.function.lower(),
    "elf_sha256": hashlib.sha256(elf).hexdigest(),
    "source_sha256": hashlib.sha256(source_path.read_bytes()).hexdigest(),
    "references": references}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Resolved {len(references)} references for RVA {args.function}.")
