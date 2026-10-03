using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EinkPenInjector;

internal static class HidSourceReader
{
    private const uint DigcfPresent = 0x00000002;
    private const uint DigcfDeviceInterface = 0x00000010;
    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint OpenExisting = 3;
    private const uint FileFlagOverlapped = 0x40000000;
    private const int ErrorNoMoreItems = 259;

    public static string? FindSourcePath()
    {
        HidD_GetHidGuid(out Guid hidClassGuid);
        IntPtr deviceSet = SetupDiGetClassDevs(ref hidClassGuid, IntPtr.Zero, IntPtr.Zero,
            DigcfPresent | DigcfDeviceInterface);
        if (deviceSet == new IntPtr(-1))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not enumerate HID devices.");
        }

        try
        {
            for (uint index = 0; ; index++)
            {
                var interfaceData = new DeviceInterfaceData
                {
                    Size = Marshal.SizeOf<DeviceInterfaceData>()
                };

                if (!SetupDiEnumDeviceInterfaces(deviceSet, IntPtr.Zero, ref hidClassGuid, index, ref interfaceData))
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == ErrorNoMoreItems)
                    {
                        return null;
                    }

                    throw new Win32Exception(error, "Could not enumerate HID device interfaces.");
                }

                SetupDiGetDeviceInterfaceDetail(deviceSet, ref interfaceData, IntPtr.Zero, 0,
                    out uint requiredSize, IntPtr.Zero);
                int sizeError = Marshal.GetLastWin32Error();
                if (requiredSize < 6 || sizeError != ErrorInsufficientBuffer)
                {
                    throw new Win32Exception(sizeError, "Could not read a HID device path.");
                }

                IntPtr detailData = Marshal.AllocHGlobal(checked((int)requiredSize));
                try
                {
                    Marshal.WriteInt32(detailData, IntPtr.Size == 8 ? 8 : 6);
                    if (!SetupDiGetDeviceInterfaceDetail(deviceSet, ref interfaceData, detailData,
                        requiredSize, out _, IntPtr.Zero))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not read a HID device path.");
                    }

                    string? path = Marshal.PtrToStringUni(IntPtr.Add(detailData, sizeof(uint)));
                    if (path is not null &&
                        path.IndexOf("vid_048d&pid_8951", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        path.IndexOf("mi_03", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        path.IndexOf("col05", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return path;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detailData);
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceSet);
        }
    }

    public static FileStream OpenReadOnly(string path)
    {
        SafeFileHandle handle = CreateFile(path, GenericRead, FileShareRead | FileShareWrite,
            IntPtr.Zero, OpenExisting, FileFlagOverlapped, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "Could not open the ITE pen collection for read-only input.");
        }

        return new FileStream(handle, FileAccess.Read, 4096, isAsync: true);
    }

    private const int ErrorInsufficientBuffer = 122;

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceInterfaceData
    {
        public int Size;
        public Guid InterfaceClassGuid;
        public int Flags;
        public IntPtr Reserved;
    }

    [DllImport("hid.dll")]
    private static extern void HidD_GetHidGuid(out Guid hidGuid);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(
        ref Guid classGuid, IntPtr enumerator, IntPtr parent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiEnumDeviceInterfaces(
        IntPtr deviceInfoSet, IntPtr deviceInfoData, ref Guid interfaceClassGuid,
        uint memberIndex, ref DeviceInterfaceData deviceInterfaceData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(
        IntPtr deviceInfoSet, ref DeviceInterfaceData deviceInterfaceData, IntPtr detailData,
        uint detailDataSize, out uint requiredSize, IntPtr deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);
}
