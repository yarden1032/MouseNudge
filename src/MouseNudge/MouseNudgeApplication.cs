using System.ComponentModel;
using System.Text.Json;

namespace MouseNudge;

internal static class MouseNudgeApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (HasArgument(args, "--help") || HasArgument(args, "-h"))
        {
            PrintHelp();
            return 0;
        }

        string configPath;
        NudgeOptions options;

        try
        {
            configPath = ResolveConfigPath(args);
            options = NudgeOptions.Load(configPath);
            options.Validate();
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or ArgumentException)
        {
            Console.Error.WriteLine($"Configuration error: {exception.Message}");
            return 1;
        }

        if (HasArgument(args, "--validate"))
        {
            Console.WriteLine($"Configuration is valid: {configPath}");
            Console.WriteLine($"Configured action: {options.DescribeAction()}");
            return 0;
        }

        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("MouseNudge uses the Windows user32 API and can only run on Windows.");
            return 1;
        }

        var inputSimulator = new InputSimulator();

        if (HasArgument(args, "--once"))
        {
            inputSimulator.Execute(options);
            Console.WriteLine($"Executed once: {options.DescribeAction()}");
            return 0;
        }

        WindowsKeepAwake? keepAwake;

        try
        {
            keepAwake = WindowsKeepAwake.Start(options.KeepAwake);
        }
        catch (Win32Exception exception)
        {
            Console.Error.WriteLine($"Keep-awake error: {exception.Message}");
            return 1;
        }

        using var keepAwakeScope = keepAwake;

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        Console.WriteLine("MouseNudge is running. Press Ctrl+C to stop.");
        Console.WriteLine($"Action: {options.DescribeAction()}");
        Console.WriteLine($"Interval: every {options.IntervalSeconds} second(s)");

        if (options.KeepAwake.Enabled)
        {
            Console.WriteLine($"Windows keep-awake: {options.KeepAwake.Describe()}");
        }

        Console.WriteLine(
            "VDI note: client-side input reaches the remote session only while the VDI window accepts keyboard/mouse input.");

        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), cancellation.Token);
                inputSimulator.Execute(options);

                if (options.LogActions)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {options.DescribeAction()}");
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            Console.WriteLine("MouseNudge stopped.");
            return 0;
        }
    }

    private static bool HasArgument(string[] args, string expected) =>
        args.Any(argument => string.Equals(argument, expected, StringComparison.OrdinalIgnoreCase));

    private static string ResolveConfigPath(string[] args)
    {
        var configIndex = Array.FindIndex(
            args,
            argument => string.Equals(argument, "--config", StringComparison.OrdinalIgnoreCase));

        if (configIndex < 0)
        {
            return Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        }

        if (configIndex == args.Length - 1 || string.IsNullOrWhiteSpace(args[configIndex + 1]))
        {
            throw new ArgumentException("--config must be followed by a file path.");
        }

        return Path.GetFullPath(args[configIndex + 1]);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("MouseNudge - configurable mouse or keyboard activity for Windows");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  MouseNudge [--config <path>] [--validate] [--once]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --config <path>  Use a different appsettings.json file");
        Console.WriteLine("  --validate       Validate the configuration without sending input");
        Console.WriteLine("  --once           Send one configured input and exit");
        Console.WriteLine("  --help, -h       Show this help");
    }
}
