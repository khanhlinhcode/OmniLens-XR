# S01 toolchain installation evidence

**Captured:** 2026-10-30  
**Scope:** T-S01-01, T-S01-02, T-S01-03  
**Current result:** PASS for S01 inventory/toolchain acceptance — device build/run remains S02

## Unity Hub

Official source:

```text
https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup-arm64.dmg
```

Observed verification:

| Check | Actual result |
|---|---|
| HTTP artifact availability | PASS, arm64 DMG available |
| SHA-256 | `c767b8c3df4a9361d8a2aeb2cd7ef1730956bcb63a89c83140858be314c6dbf0` |
| `codesign --verify --deep --strict` | PASS, valid on disk and satisfies Designated Requirement |
| `spctl --assess` | PASS, accepted as Notarized Developer ID |
| Signing authority | `Developer ID Application: Unity Technologies SF (9QW8UQUTAA)` |
| Notarization ticket | Stapled |
| Installed version | `3.22.5` |
| Architecture | arm64 |

## Unity Editor and module

The installed Hub release catalog returned `6000.3.27f1` for `arm64`. The exact install command was:

```text
/Applications/Unity\ Hub.app/Contents/MacOS/Unity\ Hub -- --headless install --version 6000.3.27f1 --architecture arm64 --module ios --errors
```

The first attempt placed `--errors` before the `install` command and failed with `Error: '--errors' is not a Hub command.` It did not install anything and is not counted as evidence. The corrected command places `--errors` at the end.

Hub resolved official changeset `672d562a42a7` and these primary CDN artifacts:

| Artifact | Source | HTTP metadata |
|---|---|---|
| Apple-silicon Editor | `https://download.unity3d.com/download_unity/672d562a42a7/MacEditorInstallerArm64/Unity-6000.3.27f1.pkg` | HTTP 200; 5,219,750,914 bytes; last modified 2026-10-14 |
| iOS Build Support | `https://download.unity3d.com/download_unity/672d562a42a7/MacEditorTargetInstaller/UnitySetup-iOS-Support-for-Editor-6000.3.27f1.pkg` | HTTP 200; 471,089,015 bytes; last modified 2026-10-14 |

Hub logged that manifest checksums were supplied and that file integrity would be checked at download completion. Installed-content verification is recorded below.

The completed iOS-support package was independently checked before Hub installation:

- SHA-256: `3894a5f55becd41ec4937ce32b6e3bbc998c8def70c71e762eee6c8eb804848b`.
- `pkgutil --check-signature`: **PASS**, Developer ID Installer `Unity Technologies SF (9QW8UQUTAA)` with trusted timestamp `2026-10-13 23:53:46 +0000`.

Final Hub result:

```text
[Unity (6000.3.27f1)] installed successfully.
[iOS Build Support] installed successfully.
All Tasks Completed Successfully.
```

Post-install verification:

| Check | Actual result |
|---|---|
| Hub installed-editor inventory | PASS — `6000.3.27f1`, `arm64`, expected `/Applications/Unity/Hub/Editor/6000.3.27f1/Unity.app` |
| Editor bundle version | PASS — `6000.3.27f1` |
| Editor bundle signature | PASS — Developer ID Application `Unity Technologies SF (9QW8UQUTAA)` |
| iOS module path | PASS — `PlaybackEngines/iOSSupport` exists |
| Installed footprint | 11 GiB |
| Free workspace volume after install | 70 GiB |

Hub periodically logged an `Experiment` endpoint `ECONNREFUSED` to localhost, but the primary downloads advanced and both install tasks ultimately reported success. The diagnostic is retained and was not used as a compatibility PASS.

Unity CLI `1.0.0-beta.13` is bundled with Hub. The user confirmed eligible personal use and EULA; OAuth sign-in and Unity Personal activation succeeded. No account credential, token, serial, machine identifier, or license file is stored in repository evidence. Optional Unity CLI telemetry was declined.

## Project creation attempt

The no-cloud command selected the exact Editor and downloaded official template `com.unity.template.3d@9.1.0`, but returned exit code `6` because the Editor itself returned licensing exit `198` before import/compile:

```text
Unity couldn't check its license, so the Editor stopped before doing any work (exit 198).
```

The command did not create a valid `ProjectVersion.txt`. Its partial directory was moved intact to `/tmp/omnilens-incomplete-project-20261030` so it cannot be mistaken for repository evidence. This first attempt is retained as **FAIL/BLOCKED**, not rewritten as a PASS.

## Project and package verification after activation

After the user confirmed eligible personal use/EULA and completed Unity sign-in:

| Check | Actual result |
|---|---|
| Unity Personal status | **PASS** — active, signed in via OAuth |
| Official project creation | **PASS** — `client/OmniLensXR`, template `com.unity.template.3d@9.1.0`, cloud disabled |
| Project version/revision | **PASS** — `6000.3.27f1 (672d562a42a7)` |
| Cardboard manifest pin | **PASS** — Git URL tag `v1.36.0` |
| Cardboard package lock | **PASS** — version `1.36.0`, hash `7fb2c0e438d2ef686f51cd59b53da16cc07b1b68` |
| Cloud/analytics/ads/purchasing/diagnostics settings | **PASS** — disabled |
| Final batch import/compile | **PASS**, exit code 0 |
| Unity EditMode tests | **PASS**, 3/3 |

The first compile run failed with two `CS0104` errors because `PackageInfo` was ambiguous between two Unity namespaces. The test was corrected with an explicit alias; no assertion was removed. The final compile log ends with `Exiting batchmode successfully now!`.

Non-fatal final-log diagnostics are retained: the Editor could not refresh a licensing access token, Unity cloud-config was unreachable, and `usbmuxd` had no connected device. License status remained active, project/package import completed, compilation exited 0, and tests passed; none of these messages is used as device evidence.

## Xcode

- Xcode `27.1` was installed from the official Mac App Store after explicit user confirmation.
- Selected developer directory: `/Applications/Xcode.app/Contents/Developer`.
- `xcodebuild -version`: **PASS** — `Xcode 27.1`, build `27A9275`.
- Xcode/Apple SDK license: **ACCEPTED** in the first-launch UI.
- `codesign --verify --strict`: **PASS**; identifier `com.apple.dt.Xcode`, Team ID `59GAB85EFG`.
- Gatekeeper assessment: **PASS** — accepted, source `Mac App Store`.
- iOS SDK inventory: **PASS** — `iphoneos27.1` and `iphonesimulator27.1`; `xcrun --sdk iphoneos --show-sdk-version` reports `27.1`.
- `xcrun --find devicectl`: **PASS** — full-Xcode tool path resolves.
- `xcodebuild -checkFirstLaunchStatus`: initially exit `69` immediately after license acceptance, then **PASS / exit 0** on the final check.
- `devicectl 651.13.5`: version/help and a sanitized `list devices` call **PASS / exit 0**. The output contained no `iPhone` name and no error marker; no identifiers were recorded.
- The user explicitly confirmed both the visible “Install Required System-wide components” action and the separately disclosed `iOS 27.1` `15.9 GB` “Download & Install” action.
- Xcode downloaded two iOS items. The first download reached 99.8% and registered simulator runtime `iOS 27.1 (24A94425)`; the second 8.05 GB item completed afterward. The Components screen now shows iOS `27.1` installed at `15.94 GB` with an Info control and no progress indicator.
- The first bounded runtime monitor ended with exit `124` at its 30-minute timeout while Xcode was still progressing. A follow-up monitor confirmed registration and final Components state; the timeout is not represented as an installation PASS or failure.
- `xcrun simctl list runtimes`: **PASS** — `iOS 27.1 (27.1 - 24A94425) - com.apple.CoreSimulator.SimRuntime.iOS-27-1`.
- watchOS `27.0`, tvOS `27.0`, visionOS `27.0`, Predictive Code Completion, and Metal Toolchain `27.1` remain uninstalled. Developer Documentation was downloaded automatically by Xcode.
- Xcode external-agent access was changed from its broad `Always` default to `While Xcode is Open`, a reversible least-privilege setting.
- Workspace volume after installation: approximately `47 GiB` free.
- Sanitized physical-device continuation: `xcdevice` discovered exactly one iPhone 11 on USB, iOS `27.2 (24B5089g)`, `available=true`, no device error. `devicectl device info details` returned exit `0` and reported paired/trusted with Developer Mode enabled. No name, serial, UDID or Apple Account was retained.
- `system_profiler SPUSBDataType` did not label the phone, while both Xcode device services discovered it; this discrepancy is retained rather than rewritten as a USB-scan PASS.
- Code-signing identity inventory: command succeeded, `0` valid identities. Signing remains blocked/user action required.
- After the user reported adding an Apple Account in Xcode, the sanitized identity inventory was rerun: exit `0`, still `0` valid identities, and no `Apple Development` identity marker. No account email, Team ID, certificate fingerprint or credential was read/stored.
- After the user reported creating/adding the certificate, Keychain contained exactly one `Apple Development` certificate and OpenSSL confirmed it is currently time-valid. `security find-identity -v -p codesigning` still returned `0` valid identities. The certificate is present but lacks an available matching signing identity/private-key pairing; no private-key material was inspected.
- Computer Use confirmed the leaf certificate and private key are paired under My Certificates, but the certificate was untrusted. Read-only chain inspection found the leaf issuer is WWDR `G3`, while System Keychain contained only the expired legacy WWDR intermediate (`2023-02-07`).
- The user explicitly confirmed installing the official Apple WWDR G3 intermediate into System Keychain. The downloaded certificate was verified before import: issuer Apple Root CA, expiry `2030-02-20T00:00:00Z`, SHA-256 `DCF21878C77F4198E4B4614F03D696D89C66C66008D4244E1B99161AAC91601F`.
- Final signing inventory: System Keychain WWDR G3 present; `security find-identity -v -p codesigning` exit `0`, exactly `1` valid identity, Apple Development marker present. No account details, Team ID, certificate fingerprint of the user leaf, or private-key material was retained.
- `softwareupdate --list` offered Command Line Tools `27.2` and macOS `27.2`; neither was installed because they do not provide the pinned full Xcode `27.1` and an OS update/restart is outside this task.

## Gate interpretation

Installing tools, validating signing readiness and discovering a phone are not equivalent to proving device compatibility. Unity Editor/module, project creation, Cardboard resolution, compile/tests, Xcode CLI/SDK/runtime, sanitized iPhone readiness and one valid development identity satisfy S01 inventory/toolchain acceptance. Unity-to-Xcode generation, provisioning and physical execution remain S02 and are not authorized by this proof.
