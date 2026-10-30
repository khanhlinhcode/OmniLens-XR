# S01 environment and device inventory

**Captured:** 2026-10-29, Asia/Ho_Chi_Minh  
**Privacy:** Hostname, serial number, Apple Account, signing identity, UDID, and other unique device identifiers were neither recorded nor stored.

## Host

| Item | Observed value | Evidence command | State |
|---|---|---|---|
| macOS | 27.0.1, build 26A434 | `sw_vers` | Verified locally |
| Kernel | Darwin 27.0.0 | `uname -s`; `uname -r` | Verified locally |
| Architecture | arm64 | `uname -m` | Verified locally |
| Hardware model | MacBookAir10,1 | `sysctl -n hw.model` | Verified locally |
| Memory | 8 GiB | `sysctl -n hw.memsize` | Verified locally |
| Logical CPUs | 8 | `sysctl -n hw.ncpu` | Verified locally |
| Workspace volume | 228 GiB total, 83 GiB available at capture | `df -h .` | Verified locally |

Unity documents 8 GB as a minimum recommendation for Unity 6. This machine meets that minimum but has no measured headroom for the planned Editor, Xcode, simulator, 3D assets, and local AI workloads. Performance remains unknown.

## Apple development tools

| Item | Observed value | State |
|---|---|---|
| Selected developer directory | `/Library/Developer/CommandLineTools` | Command Line Tools only |
| Full Xcode | Not installed/selected | **BLOCKED** |
| `xcodebuild -version` | Fails because active directory is Command Line Tools | **BLOCKED** |
| Apple clang | 21.0.0 | Verified locally |
| Swift | 6.4 | Verified locally |
| `devicectl` | Not available | **BLOCKED** by missing full Xcode |

## Unity

| Item | Observed value | State |
|---|---|---|
| Unity Hub | Not found in `/Applications` | **BLOCKED** |
| Unity Editor | No editor found in the standard Hub editor directory | **BLOCKED** |
| iOS Build Support | Cannot exist/verify without the selected editor | **BLOCKED** |

## Local general tooling

| Tool | Version/state |
|---|---|
| Git | 2.54.0 (Apple Git-157) |
| Python | 3.12.10 |
| Node.js | 22.23.2 |
| npm | 10.9.8 |
| .NET | Not found |
| Java runtime | Not found |

No global install was performed.

## iPhone 11 and viewer

| Item | Observed value | State |
|---|---|---|
| iPhone over USB | No iPhone/Apple Mobile Device detected in the sanitized USB presence check | **UNKNOWN / USER ACTION REQUIRED** |
| Exact phone model | Not discoverable | **UNKNOWN** |
| iOS version/build | Not discoverable | **UNKNOWN** |
| Signing team/provisioning | Not inspected | **UNKNOWN** |
| Cable/data trust | Not testable without a connected phone and full Xcode | **UNKNOWN** |
| Gyroscope/head-pose behavior | Not tested | **UNKNOWN** |
| Cardboard/VR Box model | Not provided | **UNKNOWN** |
| Viewer trigger, lens spacing, QR profile, camera obstruction | Not tested | **UNKNOWN** |

## T-S01-01 result at initial capture

**PARTIAL / BLOCKED.** At the 2026-10-29 capture, the Mac inventory was reproducible and sanitized while full Xcode, Unity, and a discoverable physical iPhone 11 were absent. The continuation below supersedes the tool-installation fields without erasing that baseline.

## Continuation — 2026-10-30

The original capture above is retained as the pre-install baseline. New verified state:

- Unity Hub `3.22.5` arm64: installed; Developer ID/notarization checks passed.
- Unity Editor `6000.3.27f1` arm64: installed; bundle version and code signature checked.
- iOS Build Support: installed; `PlaybackEngines/iOSSupport` exists.
- Installed Editor footprint: 11 GiB; workspace volume had 70 GiB free after installation.
- Unity Personal: user-confirmed eligible personal use/EULA; signed in and active. Project generation, Cardboard package resolution, batch compile, and 3/3 EditMode tests passed after activation.
- Xcode `27.1` (`27A9275`): installed from the Mac App Store, selected at `/Applications/Xcode.app/Contents/Developer`, license accepted, signature/Gatekeeper assessment passed, and iOS SDK `27.1` found.
- Xcode CLI readiness: `xcodebuild -checkFirstLaunchStatus` now exits `0`; `devicectl 651.13.5` version/help and sanitized device list exit `0`.
- Xcode iOS components: explicitly confirmed, downloaded and installed; Components shows iOS `27.1` at `15.94 GB`, and `simctl` registers runtime `27.1` build `24A94425`.
- Xcode external agent access: `While Xcode is Open`; watchOS/tvOS/visionOS/AI/Metal optional downloads remain disabled.
- Physical iPhone: Xcode discovers exactly one iPhone 11 over USB, running iOS `27.2 (24B5089g)`, `available=true`, paired/trusted, Developer Mode enabled, and no device error marker. Name, serial and UDID were not retained.
- `system_profiler SPUSBDataType` did not label an iPhone even though both `xcdevice` and `devicectl` discovered the available USB device; Xcode's device services are the authoritative evidence for this inventory.
- Apple-published iPhone 11 sensor baseline: Face ID, barometer, three-axis gyro, accelerometer, proximity sensor and ambient-light sensor. Runtime behavior is deliberately deferred to S02/S03 tests; source: https://support.apple.com/en-us/111865.
- Code signing: after installing Apple's official WWDR G3 intermediate in System Keychain, `security find-identity -v -p codesigning` reports exactly `1` valid Apple Development identity. No signing account, Team ID, fingerprint or private-key material was retained.

T-S01-01 is **DONE / PASS** for inventory acceptance: required Mac/iPhone model/build, toolchain, storage, cable/trust, sensor baseline and signing-readiness fields are known before build. This is not a device-build or behavior PASS; those begin in S02, while detailed viewer profiling begins in S03.
