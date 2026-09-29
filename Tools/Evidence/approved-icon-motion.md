# Approved SPIN and CASH-OUT animation repair

The approved red/gold SPIN and wallet graphics had lost visible animation during the art replacement. The SPIN animation still targeted the original rotating leaf region, which is transparent in the new atlas. CASH-OUT authoring explicitly disabled the old animation rig but did not animate its replacement Image.

## Changes

- Keep the approved SPIN frame fixed and rotate its central yin-yang using an authored circular Sprite mesh from the existing atlas. Reuse the original four-second idle and one-second accepted-click rotation curves. Keep the existing `RecoveredSpinButton` click-to-idle runtime behavior.
- Give the approved wallet a two-second 1 → 1.06 → 1 breathing loop. Reuse the original wallet glow atlas region and its 0 → 1 → 0 alpha timing. Keep the old Cash/PayPal art disabled.
- Preserve the standard Button components, target graphics, code-bound events, labels and tutorial targets.
- Integrate the authoring helpers into `ApplyApprovedMainArt` and `ApprovedSideIconsAuthoring`, so regenerating the approved art preserves the animations.
- Generate and save geometry only in Editor; players consume the saved Sprite assets and animation clips. No bitmap assets, gameplay probabilities or rewards are changed by this repair.

## Validation

Unity 2022.3.62f3 successfully imported the assets and executed authoring twice, preserving asset identity and a single hierarchy. Sprite meshes were saved and forcibly reimported: frame 128 vertices / 384 indices; rotor 65 vertices / 192 indices.

Isolated Unity rendering verified both icons at distinct animation positions. Rotation has no exposed square corners. The wallet scale and glow match their authored loop endpoints. Button references and zero serialized event callbacks passed validation.

Evidence: `Artifacts/ApprovedIconMotion/validation.json`, `spin-0.png`, `spin-1.png`, `wallet-0.png`, `wallet-1.png`.

Live `GameEntry` observation also passed: 8.035 seconds recorded 723.196 degrees of rotor travel and four wallet glow rises. Wallet scale ranged from 1.00004 to 1.05999, and glow alpha from 0.00419 to 0.99782. The visual accepted-click animation started and the real runtime `LateUpdate` returned it to idle by the 1.315-second observation. No actual spin, cash-out request or Button event was invoked. The scene was unchanged.

Runtime evidence: `Artifacts/ApprovedIconMotion/runtime-validation.json` and `runtime.png`. Temporary validation scripts are archived in `Tools/Validation/ApprovedIconMotion`; only the two asset authoring helpers remain in the Unity Editor source folder.

Actual Android package testing is not included in this repair run.
