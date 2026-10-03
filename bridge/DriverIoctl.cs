using System;
using System.Runtime.InteropServices;

namespace EinkPenBridge
{
    public static class DriverIoctl
    {
        // Private control device contract for the WDF/VHF driver.
        // This mirrors the kernel-side CTL_CODE definition in driver/EinkVirtualPen.h.
        public const int FileDeviceEinkVirtualPen = 0xE100;
        public const int EvpSubmitReport = 0x801;

        public static int SubmitReportIoctl =>
            CTL_CODE(FileDeviceEinkVirtualPen, EvpSubmitReport, 0, 0);

        private static int CTL_CODE(int deviceType, int function, int method, int access)
        {
            return ((deviceType << 16) | (access << 14) | (function << 2) | method);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct EvpIoControlRequest
    {
        public readonly byte ReportId;
        public readonly byte Flags;
        public readonly ushort X;
        public readonly ushort Y;
        public readonly ushort Pressure;

        public EvpIoControlRequest(byte flags, ushort x, ushort y, ushort pressure)
        {
            ReportId = 0x01;
            Flags = flags;
            X = x;
            Y = y;
            Pressure = pressure;
        }
    }
}
