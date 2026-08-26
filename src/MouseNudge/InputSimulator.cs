using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MouseNudge;

internal sealed class InputSimulator
{
    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint MouseEventMove = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;

    public void Execute(NudgeOptions options)
    {
        switch (options.GetMode())
        {
            case NudgeMode.Mouse:
                MoveMouse(options.Mouse);
                break;
            case NudgeMode.Keyboard:
                PressKey(options.Keyboard.ResolveVirtualKeyCode());
                break;
            default:
                throw new InvalidOperationException($"Unsupported mode: {options.Mode}");
        }
    }

    private static void MoveMouse(MouseOptions options)
    {
        var (deltaX, deltaY) = GetMovement(options.GetDirection(), options.DistancePixels);
        var canRestore = options.ReturnToStart && GetCursorPos(out var originalPosition);

        SendMouseMove(deltaX, deltaY);

        if (!canRestore)
        {
            return;
        }

        var hasMovedPosition = GetCursorPos(out var movedPosition);

        if (options.ReturnDelayMilliseconds > 0)
        {
            Thread.Sleep(options.ReturnDelayMilliseconds);
        }

        // Do not fight the user: restore only if the pointer has not moved again.
        if (hasMovedPosition &&
            GetCursorPos(out var currentPosition) &&
            currentPosition.X == movedPosition.X &&
            currentPosition.Y == movedPosition.Y)
        {
            _ = SetCursorPos(originalPosition.X, originalPosition.Y);
        }
    }

    private static (int DeltaX, int DeltaY) GetMovement(MouseDirection direction, int distance) => direction switch
    {
        MouseDirection.Right => (distance, 0),
        MouseDirection.Left => (-distance, 0),
        MouseDirection.Up => (0, -distance),
        MouseDirection.Down => (0, distance),
        MouseDirection.UpRight => (distance, -distance),
        MouseDirection.UpLeft => (-distance, -distance),
        MouseDirection.DownRight => (distance, distance),
        MouseDirection.DownLeft => (-distance, distance),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };

    private static void SendMouseMove(int deltaX, int deltaY)
    {
        var inputs = new[]
        {
            new Input
            {
                Type = InputMouse,
                Union = new InputUnion
                {
                    Mouse = new MouseInput
                    {
                        DeltaX = deltaX,
                        DeltaY = deltaY,
                        Flags = MouseEventMove
                    }
                }
            }
        };

        Send(inputs);
    }

    private static void PressKey(ushort virtualKeyCode)
    {
        var inputs = new[]
        {
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = virtualKeyCode
                    }
                }
            },
            new Input
            {
                Type = InputKeyboard,
                Union = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = virtualKeyCode,
                        Flags = KeyEventKeyUp
                    }
                }
            }
        };

        Send(inputs);
    }

    private static void Send(Input[] inputs)
    {
        var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());

        if (sent != inputs.Length)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows did not accept the simulated input.");
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetCursorPos(int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputUnion Union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MouseInput Mouse;

        [FieldOffset(0)]
        public KeyboardInput Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int DeltaX;
        public int DeltaY;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }
}
