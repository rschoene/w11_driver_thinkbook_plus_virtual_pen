# EinkVirtualPen

Experimental Windows 11 workaround for Lenovo ThinkBook Plus Gen 1 (20TG): translate the ITE E-Ink tablet-mode HID stream (VID_048D&PID_8951, MI_03/COL05) into Windows pen input.

## License

Licensed under the [GNU General Public License v3.0](LICENSE) Bundled Microsoft components are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Status

The repository contains a user-mode pen forwarder and an unfinished kernel-driver prototype. The forwarder does not require the WDK, administrator rights, or a service. Its source-device read and synthetic pen-input path build, but still need testing on the ThinkBook hardware. The VHF driver path is **untested** and not a ready-to-install signed driver; contributors are invited to finalize it (see the kernel-driver section below).

## No-driver pen forwarder

`bridge/EinkPenInjector` is a small WinForms desktop app (.NET Framework 4.8, which ships with Windows 10/11; single ~400 KB executable, nothing to install). It scans once per second for the ITE collection, reads its 18-byte reports using `GENERIC_READ` only and submits pen input with Windows' synthetic pointer API. It maps the digitizer's full coordinate range to the primary display, scales pressure to 0ÃƒÂ¢Ã¢â€šÂ¬Ã¢â‚¬Å“1024, and lets you assign each of the two side buttons (front, second) in the UI to: nothing, pen barrel button, eraser, right click, Undo (Ctrl+Z) or Redo (Ctrl+Y). Defaults: front = barrel button, second = nothing. Choices apply immediately and are stored in `%LOCALAPPDATA%\EinkPenInjector\settings.ini`. The UI is localized (English, German; add languages in `bridge/EinkPenInjector/Loc.cs`). Forwarding starts automatically; closing or minimizing the window hides it to the notification area (tray icon: click to open, right-click for Open/Exit). Tilt is not mapped.

The result is software-injected pointer input, **not a physical HID device**; applications or anti-cheat systems may identify it as injected. This app is separate from the experimental kernel driver and does not install or communicate with that driver.

Build with the .NET SDK (no workloads or extra installs needed):

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publish-bridge.ps1
```

The single executable is written to `dist\single\EinkPenInjector.exe`. Releases (tag `vX.Y.Z`) attach it as `EinkPenInjector-<tag>-win-x64.exe`.

## Architecture and target

- Platform target: x64 Windows 11 kernel driver only.
- x86 kernel drivers are not the correct target for a Win11 device driver here.
- The project uses WDF + VHF to emulate a Windows pen device without touching the original ITE control plane.
- Main files: [bridge/ReportMapping.cs](bridge/ReportMapping.cs), [bridge/DriverIoctl.cs](bridge/DriverIoctl.cs), [driver/EinkVirtualPen.c](driver/EinkVirtualPen.c), [driver/EinkVirtualPen.h](driver/EinkVirtualPen.h), [driver/EinkVirtualPen.inf](driver/EinkVirtualPen.inf), [driver/EinkVirtualPen.vcxproj](driver/EinkVirtualPen.vcxproj).

## Safety boundary

The bridge opens COL05 with GENERIC_READ only. It must not issue HidD_GetFeature, HidD_SetFeature, output reports, or ITE control requests. Previous active feature access was observed to disable E-Ink input until reboot.

## Verified source report (18 bytes, Report ID 0x06)

- byte 0: report ID = 0x06
- byte 1: status
  - bit 0x20: in range
  - bit 0x01: tip
  - bit 0x04: barrel button (front / toward tip)
  - bit 0x02: second side-button state (candidate for eraser/secondary mapping)
- bytes 2..3: X, little-endian, logical range 0..23904
- bytes 4..5: Y, little-endian, logical range 0..13446
- bytes 6..7: tip pressure, little-endian, range 0..4095

COL05 also exposes X/Y tilt in its HID capabilities; v1 deliberately leaves tilt out until exact byte offsets are verified.

## Legacy kernel-driver prototype

> **Untested.** The kernel driver (`driver/`) has only been compiled and test-signed. It has never been installed or run on any machine, and the bridge-to-driver transport is unfinished.
>
> **Contributions welcome:** you are invited to implement and finalize the driver build and release process, including the private IOCTL transport, validation on a test machine, and production signing.

    ITE MI_03/COL05 --read-only--> EinkPenBridge.exe
                                      |
                                      | private IOCTL (TODO)
                                      v
                                EinkVirtualPen.sys
                                      |
                                      v
                               Windows VHF/HID Pen
                                      |
                                  WM_POINTER

## Build prerequisites

- Visual Studio 2022 or later (the CI uses Visual Studio 2026)
- Windows SDK 10.0.28000 and the matching WDK, including the x64 desktop libraries
- KMDF driver toolchain

The driver uses Virtual HID Framework (VHF), so it links against VhfKm.lib.

## Driver builds

The driver is not part of GitHub releases. The `Driver compile check` workflow builds it on every pull request and on manual runs (`workflow_dispatch`) using the `windows-2025` runner (Visual Studio 2026, WDK extension). Pushes to branches only build the pen forwarder.

To build locally you need the prerequisites above and `scripts\build-driver.ps1`; `scripts\package-release.ps1 -Version local` creates a zip.

## Driver installation

Do not install an unsigned experimental kernel driver on a production machine yet. Finish the IOCTL boundary, build, test in a VM/test-signing environment, then create/sign an INF package.

## Next session

1. Add a control device/interface to the KMDF driver.
2. Define a private IOCTL carrying `EVP_PEN_REPORT`.
3. Have the user-mode COL05 reader submit each parsed report to that IOCTL.
4. Call `VhfReadReportSubmit` for each accepted report.
5. Validate in `Get-PnpDevice`, Paint/OneNote, and a pressure-aware drawing app.
6. Only after validation, add tilt and decide how to map status bit 0x02.
