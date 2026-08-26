using System.Text.Json;

namespace MouseNudge;

internal sealed class ConfigurationRoot
{
    public NudgeOptions? MouseNudge { get; init; }
}

internal sealed class NudgeOptions
{
    public string Mode { get; init; } = nameof(NudgeMode.Mouse);

    public int IntervalSeconds { get; init; } = 30;

    public bool LogActions { get; init; } = true;

    public MouseOptions Mouse { get; init; } = new();

    public KeyboardOptions Keyboard { get; init; } = new();

    public static NudgeOptions Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file was not found: {path}", path);
        }

        var json = File.ReadAllText(path);
        var root = JsonSerializer.Deserialize<ConfigurationRoot>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return root?.MouseNudge
            ?? throw new InvalidOperationException("The configuration must contain a 'MouseNudge' section.");
    }

    public void Validate()
    {
        if (IntervalSeconds is < 1 or > 86_400)
        {
            throw new InvalidOperationException("IntervalSeconds must be between 1 and 86400.");
        }

        switch (GetMode())
        {
            case NudgeMode.Mouse:
                Mouse.Validate();
                break;
            case NudgeMode.Keyboard:
                Keyboard.ResolveVirtualKeyCode();
                break;
            default:
                throw new InvalidOperationException($"Unsupported mode: {Mode}");
        }
    }

    public NudgeMode GetMode()
    {
        if (Enum.TryParse<NudgeMode>(Mode, ignoreCase: true, out var mode))
        {
            return mode;
        }

        throw new InvalidOperationException("Mode must be either 'Mouse' or 'Keyboard'.");
    }

    public string DescribeAction() => GetMode() switch
    {
        NudgeMode.Mouse => $"Move mouse {Mouse.GetDirection()} by {Mouse.DistancePixels} pixel(s)" +
                           (Mouse.ReturnToStart ? " and return" : string.Empty),
        NudgeMode.Keyboard => $"Press key {Keyboard.DescribeKey()}",
        _ => throw new InvalidOperationException($"Unsupported mode: {Mode}")
    };
}

internal sealed class MouseOptions
{
    public string Direction { get; init; } = nameof(MouseDirection.Right);

    public int DistancePixels { get; init; } = 5;

    public bool ReturnToStart { get; init; } = true;

    public int ReturnDelayMilliseconds { get; init; } = 150;

    public void Validate()
    {
        _ = GetDirection();

        if (DistancePixels is < 1 or > 1_000)
        {
            throw new InvalidOperationException("Mouse.DistancePixels must be between 1 and 1000.");
        }

        if (ReturnDelayMilliseconds is < 0 or > 5_000)
        {
            throw new InvalidOperationException("Mouse.ReturnDelayMilliseconds must be between 0 and 5000.");
        }
    }

    public MouseDirection GetDirection()
    {
        if (Enum.TryParse<MouseDirection>(Direction, ignoreCase: true, out var direction))
        {
            return direction;
        }

        throw new InvalidOperationException(
            "Mouse.Direction must be Right, Left, Up, Down, UpRight, UpLeft, DownRight, or DownLeft.");
    }
}

internal sealed class KeyboardOptions
{
    public string Key { get; init; } = "F15";

    public int? VirtualKeyCode { get; init; }

    public ushort ResolveVirtualKeyCode()
    {
        if (VirtualKeyCode.HasValue)
        {
            if (VirtualKeyCode.Value is < 1 or > 255)
            {
                throw new InvalidOperationException("Keyboard.VirtualKeyCode must be between 1 and 255.");
            }

            return (ushort)VirtualKeyCode.Value;
        }

        if (VirtualKeys.TryResolve(Key, out var keyCode))
        {
            return keyCode;
        }

        throw new InvalidOperationException(
            $"Keyboard.Key '{Key}' is not supported. Use a supported key name or set Keyboard.VirtualKeyCode.");
    }

    public string DescribeKey() => VirtualKeyCode.HasValue
        ? $"VK {VirtualKeyCode.Value}"
        : Key;
}

internal enum NudgeMode
{
    Mouse,
    Keyboard
}

internal enum MouseDirection
{
    Right,
    Left,
    Up,
    Down,
    UpRight,
    UpLeft,
    DownRight,
    DownLeft
}
