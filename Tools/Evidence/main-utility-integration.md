# Main help and settings entries

Restored the original UIMainView HelpBtn and SettingBtn artwork, size (88 x 187), and top-container coordinates. Static structure lives in MainUtility prefabs; runtime instantiates configured windows lazily and binds standard Button listeners. GM moved right to avoid covering HelpBtn.

UIHelpView uses the three original page images. Native ChangePage (0x23b28f0) wraps 0..2; reopening resets to page 0. Settings uses UISettingView and the existing player store. Native Music changes IsMusic (+0x38), saves, and calls SoundManager.SetMusic. The object named Sound is the vibration switch (phone vibration artwork), changing IsVibrate (+0x39). No new vibration dispatch was invented; existing gameplay currently has no vibration callers.

HOW TO PLAY closes settings and opens help. TERM OF USE opens the recovered scrollable UIPrivacyView. CONTACT US opens a mail composer using the original serialized email; no mail is automatically sent. Original decorative shine components are omitted rather than mapped to text. Popup background is translucent and blocks underlying gameplay input; popup depth uses the existing CoreRound window system.

Validation: isolated Unity 2022.3.62f3 PlayMode full-scene integration passed (Artifacts/main-utility-tests.xml). It checks actual EventSystem hit ordering on both entries, popup controls and close buttons; page wrap; settings persistence through a new store instance; immediate BGM stop; settings-to-help/privacy navigation; scrollable privacy content; and restored entry interaction after close. Rendered entry, help and settings screenshots were visually checked. Device mail app launch was not exercised.
