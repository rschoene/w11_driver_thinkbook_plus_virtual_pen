# Engineering notes

## Confirmed from live reports

Examples establish status transitions 0x00 (out), 0x20 (hover), 0x21 (tip), 0x24 (front barrel), and 0x23 while the other side-button is active together with tip. Pressure occupies source bytes 6..7 and reaches 0x0FFF under strong pressure.

## Important Windows HID constraint

For an Integrated Windows Pen, Windows requires X, Y, Tip, In-Range, and Barrel. Pressure is optional but recommended. The HID descriptor must provide accurate physical ranges, units, and unit exponent for X/Y. Therefore the descriptor in the initial skeleton intentionally marks its physical dimensions as TODO rather than pretending the logical counts are physical measurements.

## Why VHF

VHF lets a HID source driver create a virtual HID device and submit input reports with VhfReadReportSubmit. The user-mode reader should remain isolated from the Lenovo/ITE control plane and only read COL05.

## Deliberately not implemented yet

- Driver control interface/private IOCTL
- INF/CAT/signing package
- Exact physical X/Y dimensions and units
- X/Y tilt byte mapping
- Mapping for source status bit 0x02
- power/sleep/reconnect handling

These are intentional stop points so the scaffold does not masquerade as a safe installable driver before those details are validated.
