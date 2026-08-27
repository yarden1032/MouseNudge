using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MouseNudge;

internal sealed class InputSimulator
{
    private const uint InputMouse = 0;
    private const uint InputKeyboard = 1;
    private const uint MouseEventMove = 0x0001;
    private const uint KeyEventExtendedKey = 0x0001;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventScanCode = 0x0008;
    private const uint MapVirtualKeyToScanCodeExtended = 4;

    public string Execute(NudgeOptions options)
    {
        return options.GetMode() switch
        {
            NudgeMode.Mouse => ExecuteMouse(options),
            NudgeMode.Keyboard => ExecuteKeyboard(options),
            _ => throw new InvalidOperationException($"Unsupported mode: {options.Mode}")
        };
    }

    private static string ExecuteMouse(NudgeOptions options)
    {
        MoveMouse(options.Mouse);
        return options.DescribeAction();
    }

    private static string ExecuteKeyboard(NudgeOptions options)
    {
        var durationMilliseconds = PressKey(options.Keyboard);
        return $"{options.DescribeAction()} (held {durationMilliseconds} ms)";
    }

    private static void MoveMouse(MouseOptions options)
    {
        var (deltaX, deltaY) = GetMovement(options.GetDirection(), options.DistancePixels);
        Point originalPosition = default;
        var canRestore = options.ReturnToStart && GetCursorPos(out originalPosition);

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
            // Send the return as another input event so remote desktop clients can observe it.
            SendMouseMove(
                originalPosition.X - currentPosition.X,
                originalPosition.Y - currentPosition.Y);

            // Pointer acceleration or a screen edge can make a relative move imprecise.
            if (GetCursorPos(out var restoredPosition) &&
                (restoredPosition.X != originalPosition.X || restoredPosition.Y != originalPosition.Y))
            {
                _ = SetCursorPos(originalPosition.X, originalPosition.Y);
            }
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

    private static int PressKey(KeyboardOptions options)
    {
        var virtualKeyCode = options.ResolveVirtualKeyCode();
        var durationMilliseconds = RandomNumberGenerator.GetInt32(
            options.MinPressDurationMilliseconds,
            options.MaxPressDurationMilliseconds + 1);
        var keyDown = CreateKeyboardInput(virtualKeyCode, options.UseScanCode, keyUp: false);
        var keyUp = CreateKeyboardInput(virtualKeyCode, options.UseScanCode, keyUp: true);

        Send([keyDown]);

        try
        {
            if (durationMilliseconds > 0)
            {
                Thread.Sleep(durationMilliseconds);
            }
        }
        finally
        {
            Send([keyUp]);
        }

        return durationMilliseconds;
    }

    private static Input CreateKeyboardInput(ushort virtualKeyCode, bool useScanCode, bool keyUp)
    {
        var keyboardInput = new KeyboardInput
        {
            VirtualKey = virtualKeyCode,
            Flags = keyUp ? KeyEventKeyUp : 0
        };

        if (useScanCode)
        {
            var mappedScanCode = MapVirtualKey(virtualKeyCode, MapVirtualKeyToScanCodeExtended);

            if (mappedScanCode != 0)
            {
                keyboardInput.VirtualKey = 0;
                keyboardInput.ScanCode = (ushort)(mappedScanCode & 0xFF);
                keyboardInput.Flags |= KeyEventScanCode;

                var prefix = mappedScanCode & 0xFF00;
                if (prefix is 0xE000 or 0xE100)
                {
                    keyboardInput.Flags |= KeyEventExtendedKey;
                }
            }
        }

        return new Input
        {
            Type = InputKeyboard,
            Union = new InputUnion
            {
                Keyboard = keyboardInput
            }
        };
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
    private static extern uint MapVirtualKey(uint code, uint mapType);

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
