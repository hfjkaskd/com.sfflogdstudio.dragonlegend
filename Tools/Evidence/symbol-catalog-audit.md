# Base and Free symbol definitions: current source audit

The current scene ReferenceOriginal/Scenes/Main.unity contains GManager's
SymbolInfos: 11 ordered records with IDs 0..10. The audit reads these records
directly and resolves original GUIDs through the actual .meta files.

Native GManager.InitSymbols 2370fa4 copies SymbolInfos into each list. The Base
branch calls RemoveAt(8) at 2371074. The Free branch calls RemoveRange(7,Count-7)
at 23710dc. These operations remove by list position, not by a predicate on ID.
Main.SetInitShow 23bca34 reads GameSlotType (+2c), indexes allSymBolInfos (+1b8),
and writes the current list (+1c0) at 23bcadc..23bcae8 before changing visibility.
Both native ARM files were inspected in reconstruction/mumu-current/native/game.

Current RecoveredSymbolCatalog authors those ordered lists in a ScriptableObject:
Base [0,1,2,3,4,5,6,7,9,10], Free [0,1,2,3,4,5,6]. RecoveredReelView.RandomId
uses its bound mode's order; CoreRound passes the Free order to the actual Free
generation/entry pipeline. The original source shares SlotSymbolInfo records
between the two lists, so the current shared definition records do not conceal
different per-mode sprite definitions.

Run Tools/audit_symbol_catalog.py from the reconstruction project. It validates:

- All 11 names and IDs in exact source order.
- Both ordered mode lists derived from the native remove-by-index operations.
- All 22 Sprite asset payloads, allowing only line-ending/trailing-whitespace
  normalization. This includes rectangles, pivots, pixel-to-unit values, vertex
  data, index buffers and texture references, not just the sprite names.
- Every referenced texture's exact file bytes against the source GUID target.
- Original effect provenance paths in exact order, including all Wild effects.

The script passed and wrote symbol-catalog-audit.json with source scene/catalog,
normalized Sprite and texture SHA-256 values. It makes no game asset mutations.
No Unity run was required for this serialized-data audit; prior render tests
are separate evidence. Effect provenance equality does not prove animation
playback, shaders, importer settings, batching, world transforms or full visual
equivalence. Those remain subject to their own source/current-engine checks.

This resolves the old main-mode-view.md uncertainty about original symbol-list
membership for the current Base/Free definitions. It does not prove all mode
switch behavior or the full lifecycle, and adds no alternate-mode fallback.
