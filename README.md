# EinkVirtualPen

Experimental Windows 11 workaround for Lenovo ThinkBook Plus Gen 1 (20TG): translate the working ITE E-Ink tablet-mode HID stream (VID_048D&PID_8951, MI_03/COL05) into a virtual Windows pen.

## Status

This repository is now an x64 Windows 11 driver scaffold for a virtual pen workaround, not a ready-to-install signed driver. The verified source report mapping is documented, the VHF virtual-pen driver skeleton is included, and the bridge-to-driver contract is defined in the user-mode helper and INF template. The remaining integration step is to transport parsed COL05 reports from the bridge into the VHF source driver through the private IOCTL boundary.

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

## Architecture

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

- Visual Studio 2022
- Windows Driver Kit matching the installed Windows SDK
- KMDF driver toolchain

The driver uses Virtual HID Framework (VHF), so it links against VhfKm.lib.

## Driver installation

Do not install an unsigned experimental kernel driver on a production machine yet. Finish the IOCTL boundary, build, test in a VM/test-signing environment, then create/sign an INF package.

## Next session

1. Add a control device/interface to the KMDF driver.
2. Define a private IOCTL carrying `EVP_PEN_REPORT`.
3. Have the user-mode COL05 reader submit each parsed report to that IOCTL.
4. Call `VhfReadReportSubmit` for each accepted report.
5. Validate in `Get-PnpDevice`, Paint/OneNote, and a pressure-aware drawing app.
6. Only after validation, add tilt and decide how to map status bit 0x02.
