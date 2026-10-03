#pragma once
#include <ntddk.h>
#include <wdf.h>
#include <hidclass.h>
#include <vhf.h>
#include <hidport.h>

#define EVP_REPORT_ID 0x01

// Windows 11 x64 prototype: user-mode bridge sends a private IOCTL to the
// control device. This is intentionally a minimal, testable contract that can be
// expanded once the actual HID mapping is validated.
#define FILE_DEVICE_EINK_VIRTUAL_PEN 0xE100
#define EVP_IOCTL_SUBMIT_REPORT     CTL_CODE(FILE_DEVICE_EINK_VIRTUAL_PEN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

typedef struct _EVP_IO_CONTROL_REQUEST {
    UCHAR ReportId;
    UCHAR Flags;
    USHORT X;
    USHORT Y;
    USHORT Pressure;
} EVP_IO_CONTROL_REQUEST, *PEVP_IO_CONTROL_REQUEST;

#pragma pack(push, 1)
typedef struct _EVP_PEN_REPORT {
    UCHAR ReportId;
    UCHAR Flags;      // bit0 Tip, bit1 Barrel, bit2 InRange
    USHORT X;         // 0..23904
    USHORT Y;         // 0..13446
    USHORT Pressure;  // 0..4095
} EVP_PEN_REPORT, *PEVP_PEN_REPORT;
#pragma pack(pop)

typedef struct _DEVICE_CONTEXT {
    VHFHANDLE VhfHandle;
} DEVICE_CONTEXT, *PDEVICE_CONTEXT;

WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(DEVICE_CONTEXT, DeviceGetContext);
EVT_WDF_DRIVER_DEVICE_ADD EvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP EvtDeviceContextCleanup;
