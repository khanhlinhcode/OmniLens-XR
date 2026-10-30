# ADR-0001: Provisional XR foundation toolchain

- **Status:** Provisional; not device verified
- **Date:** 2026-10-29
- **Scope:** S01 / M01 / T-S01-02

## Context

OmniLens needs a reproducible Unity-to-iOS toolchain for an iPhone 11 Cardboard proof of concept. At the initial 2026-10-29 capture, the Apple-silicon Mac ran macOS 27.0.1 with only Apple Command Line Tools; Unity, full Xcode and a discoverable iPhone 11 were absent. The 2026-10-30 continuation below records the installed toolchain while retaining that baseline.

Google's Cardboard package states support for Unity 2019.4.25f1 or later, but that broad declaration is not evidence that its newest release works with this exact Unity/Xcode/iPhone combination. Cardboard `1.36.0` reports tests on iPhone 13 Mini and iPhone 16, not iPhone 11. Compatibility therefore remains a hypothesis until the physical HelloCardboard test.

## Decision

Use these **provisional pins** for the first clean compatibility sample:

| Component | Pin | Reason |
|---|---|---|
| Unity Editor | `6000.3.27f1`, Unity 6.3 LTS, Apple silicon | Current LTS patch found in Unity's official release pages; LTS reduces upgrade churn |
| Unity module | iOS Build Support | Required to generate an Xcode iOS project |
| Cardboard XR Plugin | `1.36.0`, tag `v1.36.0`, commit `7fb2c0e438d2ef686f51cd59b53da16cc07b1b68` | Current official release; immutable tag resolution avoids a moving branch |
| Xcode | `27.1` (`27A9275`) | Stable Apple release compatible with the observed host OS |
| iOS deployment target | `15.0` provisional | Within Xcode 27.1's published deployment range; Cardboard quickstart's older `12.0` floor is not used as the current toolchain floor |
| Signing | Apple Personal Team for local test only | No paid membership required for basic personal-device testing; provisioning is temporary and constrained |

The authoritative machine-readable copy is `config/toolchain-versions.json`.

## Rollback

- Unity rollback candidate: `6000.3.26f1`.
- Cardboard rollback candidate: `1.35.0`.
- Xcode rollback candidate: `27.0` (`27A266a`).
- A rollback must use a clean sample or a version-control branch created before upgrade. Do not open a project saved by a newer Unity version in an older Editor and call that a supported downgrade.
- If Cardboard cannot run on the iPhone 11, stop VR purchasing and retain a 2D client fallback. Do not switch to an unofficial plugin without a new ADR and license/security review.

## Consequences

- Installation requires explicit user approval because it downloads large SDKs and changes the development environment.
- S01 can verify software installation, package resolution and host-side compile, but cannot claim physical-device compatibility before the iPhone test.
- S02 must test signing and HelloCardboard on the actual iPhone 11 before G00 can PASS.
- `iOS 15.0` is a deployment target, not proof that Xcode can debug any arbitrary installed iOS. Xcode 27.1 publishes physical device support for iOS 17 or later; the phone's actual iOS is still unknown.
- The iPhone 11 is not listed in Cardboard `1.36.0` release testing. No compatibility, FPS, thermal, readability, or comfort claim is approved.

## Verification required

1. Install the exact pinned Editor/module and Xcode versions from official channels.
2. Record `xcodebuild -version` and Unity's version/module output.
3. Generate a clean Unity project with the pinned tag.
4. Resolve package dependencies and store the resulting lock file.
5. Build HelloCardboard through Xcode and run it on the physical iPhone 11.
6. Exercise camera permission denied/allowed, QR scan, background/resume, head rotation, recenter, and failure logs.

Steps 1–4 have local evidence. Steps 5–6 remain unrun and keep the decision **provisional**.

## Local continuation — 2026-10-30

- Installed official Unity Hub `3.22.5` arm64 after SHA-256, code-signature, Developer ID, and Apple notarization verification.
- Installed Unity `6000.3.27f1` arm64 plus iOS Build Support from Hub changeset `672d562a42a7`; Hub completed both tasks successfully, and local inventory/bundle/module/code-signature checks passed.
- `git ls-remote` resolved Cardboard tag `v1.36.0` to commit `7fb2c0e438d2ef686f51cd59b53da16cc07b1b68`.
- The plugin tag's `package.json` declares version `1.36.0`, minimum Unity `6000.0.58f2`, and dependencies on Input System `1.7.0`, XR Management `3.2.9`, and XR Legacy Input Helpers `2.1.4`.
- Apple's current requirements page lists Xcode `27.2 RC` as a prerelease and `27.1` as the stable line. The local software-update catalog offered Command Line Tools `27.2`, but that is not full Xcode and was deliberately not installed; the stable Xcode `27.1` pin is unchanged.
- Xcode `27.1` (`27A9275`) was installed from the Mac App Store, selected, license accepted, signature/Gatekeeper assessed, and its iOS `27.1` SDK verified. The user confirmed the iOS-only component download; Xcode registered iOS simulator runtime `27.1` build `24A94425`. watchOS, tvOS, visionOS, Predictive Code Completion and Metal Toolchain downloads remain disabled. External agent access was set to `While Xcode is Open` rather than the broader `Always` default.
- The first project-creation attempt stopped with licensing exit `198`; its incomplete output was moved outside the repository. After the user confirmed eligible personal use/EULA, Unity Personal activation succeeded, the official CLI generated the project with cloud disabled, Cardboard resolved to the pinned commit, batch compile passed, and all three EditMode foundation tests passed.
- Xcode generation, Apple signing, and physical iPhone execution remain unverified.
- Sanitized Xcode device discovery later confirmed the actual iPhone 11 on USB with iOS `27.2 (24B5089g)`, paired/trusted, available and Developer Mode enabled. The host currently has zero valid code-signing identities, so no build/run compatibility claim follows from discovery.
- Signing continuation supersedes the previous zero-identity observation: Computer Use confirmed the Apple Development certificate/private-key pair, diagnosed the missing WWDR G3 intermediate, and imported Apple's verified official G3 certificate into System Keychain after explicit user confirmation. Final inventory reports exactly one valid Apple Development identity; provisioning and build/run remain S02.

## Official sources checked 2026-10-29 and 2026-10-30

- Unity 6 release support: https://unity.com/releases/unity-6/support
- Unity `6000.3.27f1`: https://unity.com/releases/editor/whats-new/6000.3.27f1
- Unity `6000.3.26f1`: https://unity.com/releases/editor/whats-new/6000.3.26f1
- Unity 6 system requirements: https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html
- Unity iOS environment setup: https://docs.unity3d.com/6000.0/Documentation/Manual/ios-environment-setup.html
- Cardboard releases: https://github.com/googlevr/cardboard-xr-plugin/releases
- Cardboard `v1.36.0`: https://github.com/googlevr/cardboard-xr-plugin/releases/tag/v1.36.0
- Cardboard repository/package requirements: https://github.com/googlevr/cardboard-xr-plugin
- Cardboard Unity quickstart: https://developers.google.com/cardboard/develop/unity/quickstart
- Apple Xcode requirements: https://developer.apple.com/xcode/system-requirements
- Xcode `27.1` release: https://developer.apple.com/news/releases/?id=10152026a
- Apple Personal Team behavior: https://developer.apple.com/help/account/basics/about-your-developer-account/
