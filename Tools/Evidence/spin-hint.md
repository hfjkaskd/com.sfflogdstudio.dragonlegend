# Spin idle finger

Native InitSpinSequence 23ba59c appends a two-second scaled interval followed by
23bf46c. The callback reads Main.Spine (+168), gets its transform.parent, and calls
PoolManager.ShowFinger with the existing Main.SpineFingerObj (+210). PoolManager
238b954 spawns only if no existing object, otherwise reparents it; both paths use
unit local scale and zero anchored position. HideFinger 238bdbc only deactivates it.
Accepted Spin 23bd970..23bd9b4 hides it and kills/nulls the pending sequence.

Initial Main setup 23bb0ec calls InitSpinSequence at 23bb288. Successful end flow
23c56f0 calls it at 23c67cc, before clearing isSpin and saving. Production core Bind
now starts the initial timer; core completion restarts it before unlock/save.
Accepted Spin hides/cancels it. Explicit GM core teardown also hides/cancels it.

Original UIMainView.Spine points to component 114459574422864414 on GameObject
1433013960508438, Rect 224333476462637410. Its parent is SpinBtn Rect
224871677951225513 at (417,-23.793), size (222,219.41). RecoveredSpinButton is on that
button root, so the hint destination is the component's own transform. Using its
parent incorrectly targets Bottom/Main; a fresh capture exposed and corrected that
initial authoring error even though general rendering checks passed.

Finger.prefab retains root size 100x100 and child size (160.88226,159.71727), both
centered, unit scale, zero position. Original ef_shouzhi binary is fully consumed:
16933 bytes, one animation named animation, duration 1.833333373, 32 timelines.
It is converted offline to the existing native Unity Animation/mesh rig pipeline.
No Spine or tween plugin assembly is added. The large atlas stays Resources-loaded.
The shared geometry regression compares 56 independently sampled source frames and
checks zero managed allocation across 1000 warmed pose samples.

The production-scene test covers the scaled delay including pause, real rendered
pixel contribution, accepted-click hiding, no hint during settlement, delayed reuse
after completion, and GM cancellation. It captures the current project to
Artifacts/current-spin-finger.png. The hint is presentation only, with raycastTarget
false; the existing standard Unity Button remains the interaction owner.

This implements the Spin idle hint only. Bank/review/cash-out end-flow waits and
other onboarding steps are still incomplete; this does not claim complete guidance.

Final validation: Artifacts/spin-hint-final.xml passes 357/357 PlayMode tests. The
fresh current-spin-finger.png was inspected after the destination fix: the finger
points at the right-side Spin control, not the centered GOOD LUCK text.
