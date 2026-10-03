using System;

namespace EinkPenBridge
{
    public readonly struct VirtualPenReport
    {
        public readonly byte ReportId;
        public readonly byte Flags;
        public readonly ushort X;
        public readonly ushort Y;
        public readonly ushort Pressure;

        public VirtualPenReport(byte flags, ushort x, ushort y, ushort pressure)
        {
            ReportId = 0x01;
            Flags = flags;
            X = x;
            Y = y;
            Pressure = pressure;
        }
    }

    public static class ReportMapping
    {
        // Source: ITE VID_048D&PID_8951 MI_03/COL05, 18-byte report ID 0x06.
        public static bool TryParse(byte[] src, out VirtualPenReport pen)
        {
            pen = default;
            if (src.Length < 18 || src[0] != 0x06) return false;

            byte status = src[1];
            bool inRange = (status & 0x20) != 0;
            bool tip = inRange && (status & 0x01) != 0;
            bool barrel = inRange && (status & 0x04) != 0;

            ushort x = (ushort)(src[2] | (src[3] << 8));
            ushort y = (ushort)(src[4] | (src[5] << 8));
            ushort pressure = (ushort)(src[6] | (src[7] << 8));

            if (x > 23904 || y > 13446 || pressure > 4095) return false;

            byte flags = 0;
            if (tip) flags |= 0x01;
            if (barrel) flags |= 0x02;
            if (inRange) flags |= 0x04;

            pen = new VirtualPenReport(flags, x, y, pressure);
            return true;
        }
    }
}
