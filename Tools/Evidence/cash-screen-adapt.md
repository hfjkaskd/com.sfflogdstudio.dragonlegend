# Original Adapt on cash window content

RecoveredScreenAdapt replaces the original Adapt script directly on the imported
Content object. The builder now maps that GUID instead of removing the component.
No static UI hierarchy is created by runtime code.

Evidence: Adapt.Awake238c094 caches RectTransform and scaler then adapts;
OnEnable238c4d4 and Start238c4d8 tail-call AdaptScreen238c210. There is no original
Update/LateUpdate in this class, so no unsolicited polling was added.
CacheScaler238c0fc prefers GetComponentInParent<CanvasScaler>, then the UIManager
root scaler. Recovered BindUiRootScaler supplies the latter dependency when needed;
the future root-window controller must provide it if no parent scaler exists.

AdaptScreen returns while reentrant, without rect/scaler, for screen width/height
<=1, or safe width/height not >1. It caches last safe area and screen dimensions,
sets anchors (0,0)..(1,1), then writes offsets. It uses the native arithmetic:

conversion = match*referenceHeight/screenHeight
             - referenceWidth*(match-1)/screenWidth

offsetMin = safe origin * conversion. offsetMax subtracts the separately blended
reference extents from safe maximum. This is the original linear reference blend,
not a substitution using CanvasScaler's logarithmic scale-factor computation or
normalized safe-area anchors. Unsupported/invalid dimensions do not overwrite
the prior layout or last successfully applied state.

Validation: Unity2022.3.62f3 PlayMode `cash-adapt.xml` 6/6 passed, PID52176 exited.
Three exact match-value cases, full-safe landscape, invalid/NaN dimensions,
parent-scaler precedence and missing-scaler behavior are tested against actual
RectTransform offsets. The full cash-window render/raycast regression also passed;
`Artifacts/current-cash-window.png` was regenerated and inspected. That render
has no simulated notch and does not claim physical device validation.

Outstanding: bind owning window/UI root lifecycle and scaler dependency, close
behavior, child account/tip windows, gift branch, task continuation and core entry.
No SDK or country-routing behavior changed.
