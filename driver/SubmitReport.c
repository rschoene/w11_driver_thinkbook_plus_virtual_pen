#include "EinkVirtualPen.h"

// Call this after the private bridge->driver IOCTL is implemented.
NTSTATUS EvpSubmitPenReport(PDEVICE_CONTEXT Context, const EVP_PEN_REPORT* Report)
{
    HID_XFER_PACKET packet;
    RtlZeroMemory(&packet, sizeof(packet));
    packet.reportBuffer = (PUCHAR)Report;
    packet.reportBufferLen = sizeof(*Report);
    packet.reportId = EVP_REPORT_ID;
    return VhfReadReportSubmit(Context->VhfHandle, &packet);
}
