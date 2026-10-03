#include "EinkVirtualPen.h"

// Minimal Integrated Pen descriptor for the first prototype.
// X/Y physical dimensions are placeholders and MUST be replaced with accurate
// digitizer dimensions before treating this as a finished Windows pen device.
// NOTE: this descriptor is intentionally a prototype. Before deployment on a
// real Windows 11 device, the physical dimensions and units should be validated
// against the target digitizer and the device's real HID capabilities.
static const UCHAR g_ReportDescriptor[] = {
    0x05, 0x0D,       // Usage Page (Digitizers)
    0x09, 0x02,       // Usage (Pen)
    0xA1, 0x01,       // Collection (Application)
    0x85, EVP_REPORT_ID,
    0x09, 0x20,       //   Usage (Stylus)
    0xA1, 0x00,       //   Collection (Physical)

    0x09, 0x42,       //     Tip Switch
    0x09, 0x44,       //     Barrel Switch
    0x09, 0x32,       //     In Range
    0x15, 0x00,
    0x25, 0x01,
    0x75, 0x01,
    0x95, 0x03,
    0x81, 0x02,       //     Input (Data,Var,Abs)
    0x75, 0x05,
    0x95, 0x01,
    0x81, 0x03,       //     padding

    0x05, 0x01,       //     Usage Page (Generic Desktop)
    0x09, 0x30,       //     X
    0x16, 0x00, 0x00,
    0x26, 0x60, 0x5D, //     Logical Max 23904
    0x36, 0x00, 0x00,
    0x46, 0x60, 0x5D, //     TODO physical max: placeholder
    0x55, 0x00,
    0x65, 0x00,       //     TODO units: placeholder
    0x75, 0x10,
    0x95, 0x01,
    0x81, 0x02,

    0x09, 0x31,       //     Y
    0x26, 0x86, 0x34, //     Logical Max 13446
    0x46, 0x86, 0x34, //     TODO physical max: placeholder
    0x81, 0x02,

    0x05, 0x0D,       //     Usage Page (Digitizers)
    0x09, 0x30,       //     Tip Pressure
    0x16, 0x00, 0x00,
    0x26, 0xFF, 0x0F, //     4095
    0x75, 0x10,
    0x95, 0x01,
    0x81, 0x02,

    0xC0,
    0xC0
};

VOID EvtDeviceContextCleanup(WDFOBJECT DeviceObject)
{
    PDEVICE_CONTEXT ctx = DeviceGetContext(DeviceObject);
    if (ctx->VhfHandle) {
        VhfDelete(ctx->VhfHandle, TRUE);
        ctx->VhfHandle = NULL;
    }
}

NTSTATUS EvtDeviceAdd(WDFDRIVER Driver, PWDFDEVICE_INIT DeviceInit)
{
    UNREFERENCED_PARAMETER(Driver);
    NTSTATUS status;
    WDFDEVICE device;
    WDF_OBJECT_ATTRIBUTES attrs;
    VHF_CONFIG vhfConfig;
    PDEVICE_CONTEXT ctx;

    WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attrs, DEVICE_CONTEXT);
    attrs.EvtCleanupCallback = EvtDeviceContextCleanup;

    status = WdfDeviceCreate(&DeviceInit, &attrs, &device);
    if (!NT_SUCCESS(status)) return status;

    ctx = DeviceGetContext(device);
    ctx->VhfHandle = NULL;

    VHF_CONFIG_INIT(&vhfConfig,
                    WdfDeviceWdmGetDeviceObject(device),
                    sizeof(g_ReportDescriptor),
                    (PUCHAR)g_ReportDescriptor);

    vhfConfig.VendorID  = 0x1209; // prototype/community VID range; revisit before distribution
    vhfConfig.ProductID = 0xE1A1;
    vhfConfig.VersionNumber = 0x0001;

    status = VhfCreate(&vhfConfig, &ctx->VhfHandle);
    if (!NT_SUCCESS(status)) return status;

    status = VhfStart(ctx->VhfHandle);
    return status;
}

NTSTATUS DriverEntry(PDRIVER_OBJECT DriverObject, PUNICODE_STRING RegistryPath)
{
    WDF_DRIVER_CONFIG config;
    WDF_DRIVER_CONFIG_INIT(&config, EvtDeviceAdd);

    // The actual production driver should expose a control interface that user-mode
    // code uses to submit sanitized reports. The bridge contract is defined in
    // EVP_IO_CONTROL_REQUEST and the corresponding user-mode helper code in
    // bridge/DriverIoctl.cs.
    return WdfDriverCreate(DriverObject,
                           RegistryPath,
                           WDF_NO_OBJECT_ATTRIBUTES,
                           &config,
                           WDF_NO_HANDLE);
}
