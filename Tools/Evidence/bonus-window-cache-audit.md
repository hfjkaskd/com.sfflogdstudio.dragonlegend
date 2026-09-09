# Bonus ordinary hide and cached-show source audit

Baseline 871d2a5. This audit addresses the earlier explicit uncertainty in
bonus-selection.md: a retained selection controller preserves isNeedPlayAd,
but that fact alone does not show how the original manager obtains a window.

Inspected current original sources under reconstruction/mumu-current:

- native-functions/dependency/33bc958.c, Base_GetWindow: queries the dictionary
  at manager +0x28 with TryGetValue, returning the stored window when found.
- 33bdeb8.c, PrepareToShowWindow: calls Base_GetWindow first. CreateWindow is
  reached when the returned Unity object compares null. An existing hidden
  window proceeds through ApplyWindowShowProperty without recreation, subject
  to its existing transition-state guard at +0x39.
- 33bcb9c.c, Base_ShowWindow: passes that obtained object through BeforeShow,
  activation and the existing entrance lifecycle; it does not unconditionally
  instantiate another window.
- 33bce80.c, Base_HideWindow: the nonanimated path calls Internal_OnHide,
  SetActive(false), then AfterHide. The animated path invokes the window's exit
  interface and its completion callback. It removes the entry from manager
  +0x30 (shown windows), not the +0x28 cache. Base_DestroyWindow is a distinct
  method at 33bd5c0. These generic-method files are decompiled pseudocode with
  verified entry bytes; indirect calls still require their own interpretation.
- native/game/023f9358.asm and 023f9360.asm: ordinary BaseWindow.Hide forwards
  animate=true to UIManager.HideWindow. Bonus callback 0239bc90.asm uses Hide.
- 0239b5cc.asm, Bonus OnAfterHide: calls base hook, hides the pooled finger,
  kills/clears its hint tween, and cancels/disposes its cancellation source.
  No isClick or isNeedPlayAd reset is present.
- 023f8ed0.asm, BaseWindow.AfterHide: clears its own lifecycle flags at +0x38,
  dispatches the virtual hide hook and cached lifecycle listeners. These flags
  are not the Bonus selection fields.

Current RecoveredBonusWindow.Show creates RecoveredBonusSelection only when
selection is null. OnDisable stops presentation and hint work, while
BeforeShow resets cards/end state and retains the two source selection flags.
This matches ordinary cached hide/show in the inspected manager path. A prior
successful free-threshold session therefore retains its ad requirement when
that object is reused; recreating the controller every Show would erase source
state without support from this path.

Scope remains limited: this does not establish that no external manager event
listener or lifecycle component ever destroys a window, nor prove a complete
second naturally generated Android Bonus session. Explicit destruction and
profile/scene recreation must be assessed separately. No runtime mutation or
test rerun was justified by this audit. Latest targeted Bonus flow result
remains 3/3 in bonus-failed-ad-exit-tests.xml; it covers first-window paths,
not the unresolved full repeat-session boundary.
