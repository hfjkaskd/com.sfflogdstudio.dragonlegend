# Cash window entrance animation

RecoveredCashOutEntrance is configured on the actual CashOutWindow prefab. Its
owning window must call Play after cash list initialization, matching native
OnBeforeShow's final LoadAnim call. Production root lifecycle binding remains
outstanding; this addition does not claim that main/core can open this window yet.

Evidence:
- OnBeforeShow23a7b28 ends with openType=0, CheckAccount(1), CheckAll, reparenting
  payment frame to the first payment button, then LoadAnim().Forget().
- LoadAnim23a7094 delegates to state machine23ab52c.
- Predicate23aa630 waits for non-null cash list and its isFinish field (+0x20).
  RecoveredCashOutList.IsCreateFinished becomes true after its first population.
- Continuation checks current openType after waiting; gift mode does not animate.
- It visits actual ScrollRect.content children in sibling order (including any
  pooled children), activates each, sets scale zero, then starts scale-to-one with
  .2 seconds duration, index*.05 seconds delay, Ease27 OutBack.
- After scheduling children, Bottom starts at captured initial position minus
  450 on Y and moves to initial Y over .3 seconds, native default OutQuad.
  This starts concurrently with card tweens, not after their delays complete.
- ELF literals dbc540=.2, dbc564=.05, dbc664=.3 verified from PT_LOAD bytes.
  Native OnInit stores original Bottom anchoredPosition for this transition.

Implementation uses official Unity AnimationCurve assets and the shared scaled
update runner. Existing/overlapping plays keep their own motions, while Cancel
and destruction release owned waits/motions. Completed motion references are
pruned before another play; no per-frame object creation or hierarchy lookup is
introduced. Parameters and curves are serialized by BuildCashOutWindow.

Validation: Unity 2022.3.62f3 PlayMode `cash-entrance.xml`, 2/2 passed, PID5340
exited. Tests instantiate the actual window and check waiting before population,
initial zero scales/-450 offset while paused, staggered progression, OutBack
overshoot, final scale/position, cancellation and current-mode evaluation after
the wait. They do not substitute a timing-only mock for the actual UI transforms.

Further lifecycle evidence: OnInitProperty23a6dc4 writes windowType300 and
bgMaskMode1 (ELF dbc1f0); remaining defaults come from UIWindowProperty ctor
23f99b0 (maskAlpha .65, allowBecomeTopWindow true). This must be applied when
recovering the owning window manager/close behavior, rather than treating the
source prefab Canvas sortingOrder0 as the full runtime window policy.

Outstanding: root lifecycle/Adapt, close flow, cash/gift tab control, child
account/tip windows, task continuation and main/core entry. SDK is unchanged.
