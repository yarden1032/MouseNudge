namespace MouseNudge;

internal static class VirtualKeys
{
    private static readonly IReadOnlyDictionary<string, ushort> Values = BuildValues();

    public static bool TryResolve(string key, out ushort virtualKeyCode)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            virtualKeyCode = 0;
            return false;
        }

        return Values.TryGetValue(key.Trim(), out virtualKeyCode);
    }

    private static IReadOnlyDictionary<string, ushort> BuildValues()
    {
        var values = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            ["Backspace"] = 0x08,
            ["Tab"] = 0x09,
            ["Enter"] = 0x0D,
            ["Shift"] = 0x10,
            ["Control"] = 0x11,
            ["Ctrl"] = 0x11,
            ["Alt"] = 0x12,
            ["Pause"] = 0x13,
            ["CapsLock"] = 0x14,
            ["Escape"] = 0x1B,
            ["Esc"] = 0x1B,
            ["Space"] = 0x20,
            ["PageUp"] = 0x21,
            ["PageDown"] = 0x22,
            ["End"] = 0x23,
            ["Home"] = 0x24,
            ["Left"] = 0x25,
            ["Up"] = 0x26,
            ["Right"] = 0x27,
            ["Down"] = 0x28,
            ["Insert"] = 0x2D,
            ["Delete"] = 0x2E,
            ["NumLock"] = 0x90,
            ["ScrollLock"] = 0x91
        };

        for (var character = 'A'; character <= 'Z'; character++)
        {
            values[character.ToString()] = character;
        }

        for (var digit = 0; digit <= 9; digit++)
        {
            var code = (ushort)(0x30 + digit);
            values[digit.ToString()] = code;
            values[$"D{digit}"] = code;
            values[$"NumPad{digit}"] = (ushort)(0x60 + digit);
        }

        for (var functionKey = 1; functionKey <= 24; functionKey++)
        {
            values[$"F{functionKey}"] = (ushort)(0x6F + functionKey);
        }

        return values;
    }
}
