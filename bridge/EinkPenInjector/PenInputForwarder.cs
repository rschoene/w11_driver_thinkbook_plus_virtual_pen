using System.ComponentModel;
using System.Runtime.InteropServices;
using EinkPenBridge;
using Microsoft.Win32.SafeHandles;

namespace EinkPenInjector;

internal sealed class PenInputForwarder : IDisposable
{
    private const uint PointerPen = 3;
    private const uint PointerFeedbackDefault = 3; // POINTER_FEEDBACK_NONE: no visual feedback/busy cursor
    private const uint PointerFlagNew = 0x00000001;
    private const uint PointerFlagInRange = 0x00000002;
    private const uint PointerFlagInContact = 0x00000004;
    private const uint PointerFlagPrimary = 0x00002000;
    private const uint PointerFlagCanceled = 0x00008000;
    private const uint PointerFlagDown = 0x00010000;
    private const uint PointerFlagUpdate = 0x00020000;
    private const uint PointerFlagUp = 0x00040000;
    private const uint PointerFlagSecondButton = 0x00000020;
    private const uint PenFlagBarrel = 0x00000001;
    private const uint PenFlagEraser = 0x00000004;
    private const uint PenMaskPressure = 0x00000001;
    private const int MaxSourceX = 23904;
    private const int MaxSourceY = 13446;
    private const int MaxSourcePressure = 4095;
    private readonly SyntheticPointerHandle _device;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private bool _inRange;
    private bool _inContact;
    private uint _pointerId;
    private readonly AppSettings _settings;
    private bool _frontPressed;
    private bool _secondPressed;
    private uint _penFlags;
    private uint _buttonPointerFlags;

    public PenInputForwarder(AppSettings settings)
    {
        _settings = settings;
        _screenWidth = GetSystemMetrics(0);
        _screenHeight = GetSystemMetrics(1);
        if (_screenWidth <= 0 || _screenHeight <= 0)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Loc.Get("ErrorDisplaySize"));
        }

        IntPtr handle = CreateSyntheticPointerDevice(PointerPen, 1, PointerFeedbackDefault);
        if (handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Loc.Get("ErrorCreatePointer"));
        }

        _device = new SyntheticPointerHandle(handle);
    }

    public void Forward(VirtualPenReport report, bool secondButton)
    {
        bool inRange = (report.Flags & 0x04) != 0;
        bool inContact = (report.Flags & 0x01) != 0;
        if (!inRange)
        {
            ApplyButtons(front: false, second: false);
            if (_inRange)
            {
                if (_inContact)
                {
                    Inject(report, PointerFlagInRange | PointerFlagPrimary | PointerFlagUp, inContact: false);
                }

                Inject(report, PointerFlagPrimary | PointerFlagUpdate, inContact: false);
                _inRange = false;
                _inContact = false;
            }

            return;
        }

        ApplyButtons((report.Flags & 0x02) != 0, secondButton);
        uint pointerFlags;
        if (!_inRange)
        {
            _pointerId = _pointerId == uint.MaxValue ? 1 : _pointerId + 1;
            pointerFlags = PointerFlagNew | PointerFlagInRange | PointerFlagPrimary | PointerFlagUpdate;
            if (inContact)
            {
                pointerFlags |= PointerFlagInContact | PointerFlagDown;
            }
        }
        else if (_inContact != inContact)
        {
            pointerFlags = PointerFlagInRange | PointerFlagPrimary;
            pointerFlags |= inContact
                ? PointerFlagInContact | PointerFlagDown
                : PointerFlagUp;
        }
        else
        {
            pointerFlags = PointerFlagInRange | PointerFlagPrimary | PointerFlagUpdate;
            if (inContact)
            {
                pointerFlags |= PointerFlagInContact;
            }
        }

        Inject(report, pointerFlags | _buttonPointerFlags, inContact);
        _inRange = true;
        _inContact = inContact;
    }

    public void Cancel()
    {
        if (_inRange)
        {
            ApplyButtons(front: false, second: false);
            Inject(new VirtualPenReport(0, 0, 0, 0), PointerFlagCanceled, inContact: false);
            _inRange = false;
            _inContact = false;
        }
    }

    public void Dispose()
    {
        try
        {
            Cancel();
        }
        finally
        {
            _device.Dispose();
        }
    }

    private void ApplyButtons(bool front, bool second)
    {
        _penFlags = 0;
        _buttonPointerFlags = 0;
        ApplyButton(_settings.FrontButton, front, front && !_frontPressed);
        ApplyButton(_settings.SecondButton, second, second && !_secondPressed);
        _frontPressed = front;
        _secondPressed = second;
    }

    private void ApplyButton(ButtonAction action, bool pressed, bool justPressed)
    {
        if (!pressed)
        {
            return;
        }

        switch (action)
        {
            case ButtonAction.BarrelButton:
                _penFlags |= PenFlagBarrel;
                break;
            case ButtonAction.EraserButton:
                _penFlags |= PenFlagEraser;
                break;
            case ButtonAction.RightClick:
                _buttonPointerFlags |= PointerFlagSecondButton;
                break;
            case ButtonAction.Undo when justPressed:
                SendCtrlShortcut(0x5A);
                break;
            case ButtonAction.Redo when justPressed:
                SendCtrlShortcut(0x59);
                break;
        }
    }

    private static void SendCtrlShortcut(ushort virtualKey)
    {
        const ushort VkControl = 0x11;
        var inputs = new[]
        {
            KeyInput(VkControl, keyUp: false),
            KeyInput(virtualKey, keyUp: false),
            KeyInput(virtualKey, keyUp: true),
            KeyInput(VkControl, keyUp: true)
        };

        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<KeyboardInputRecord>()) != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Loc.Get("ErrorSendKeys"));
        }
    }

    private static KeyboardInputRecord KeyInput(ushort virtualKey, bool keyUp) => new()
    {
        Type = 1,
        Data = new KeyboardInputUnion
        {
            Keyboard = new KeyboardInput { VirtualKey = virtualKey, Flags = keyUp ? 2u : 0u }
        }
    };

    private void Inject(VirtualPenReport report, uint pointerFlags, bool inContact)
    {
        int x = (int)Math.Round((double)report.X / MaxSourceX * (_screenWidth - 1));
        int y = (int)Math.Round((double)report.Y / MaxSourceY * (_screenHeight - 1));
        var penInfo = new PointerPenInfo
        {
            PointerInfo = new PointerInfo
            {
                PointerType = PointerPen,
                PointerId = _pointerId,
                PointerFlags = pointerFlags,
                PixelLocation = new NativePoint { X = x, Y = y },
                PixelLocationRaw = new NativePoint { X = x, Y = y }
            },
            PenFlags = _penFlags,
            PenMask = PenMaskPressure,
            Pressure = inContact
                ? (uint)Math.Round((double)report.Pressure * 1024 / MaxSourcePressure)
                : 0
        };
        var input = new PointerTypeInfo
        {
            Type = PointerPen,
            Data = new PointerTypeInfoUnion
            {
                PenInfo = penInfo
            }
        };

        if (!InjectSyntheticPointerInput(_device, ref input, 1))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), Loc.Get("ErrorInjectRejected"));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointerInfo
    {
        public uint PointerType;
        public uint PointerId;
        public uint FrameId;
        public uint PointerFlags;
        public IntPtr SourceDevice;
        public IntPtr TargetWindow;
        public NativePoint PixelLocation;
        public NativePoint HimetricLocation;
        public NativePoint PixelLocationRaw;
        public NativePoint HimetricLocationRaw;
        public uint Time;
        public uint HistoryCount;
        public int InputData;
        public uint KeyStates;
        public ulong PerformanceCount;
        public int ButtonChangeType;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointerPenInfo
    {
        public PointerInfo PointerInfo;
        public uint PenFlags;
        public uint PenMask;
        public uint Pressure;
        public uint Rotation;
        public int TiltX;
        public int TiltY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointerTouchInfo
    {
        public PointerInfo PointerInfo;
        public uint TouchFlags;
        public uint TouchMask;
        public NativeRect Contact;
        public NativeRect ContactRaw;
        public uint Orientation;
        public uint Pressure;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PointerTypeInfoUnion
    {
        [FieldOffset(0)]
        public PointerInfo PointerInfo;

        [FieldOffset(0)]
        public PointerTouchInfo TouchInfo;

        [FieldOffset(0)]
        public PointerPenInfo PenInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointerTypeInfo
    {
        public uint Type;
        public PointerTypeInfoUnion Data;
    }

    private sealed class SyntheticPointerHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SyntheticPointerHandle(IntPtr handle) : base(ownsHandle: true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle() => DestroySyntheticPointerDevice(handle);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreateSyntheticPointerDevice(uint pointerType, uint maxCount, uint feedbackMode);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InjectSyntheticPointerInput(
        SyntheticPointerHandle device, ref PointerTypeInfo pointerInfo, uint count);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroySyntheticPointerDevice(IntPtr device);

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInputPlaceholder
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct KeyboardInputUnion
    {
        [FieldOffset(0)]
        public KeyboardInput Keyboard;

        [FieldOffset(0)]
        public MouseInputPlaceholder Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInputRecord
    {
        public uint Type;
        public KeyboardInputUnion Data;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, KeyboardInputRecord[] inputs, int size);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetSystemMetrics(int index);
}
