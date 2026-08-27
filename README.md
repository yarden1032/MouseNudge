# MouseNudge

[![Build and release](https://github.com/yarden1032/MouseNudge/actions/workflows/initial-release.yml/badge.svg)](https://github.com/yarden1032/MouseNudge/actions/workflows/initial-release.yml)

A tiny, configuration-only Windows console application that keeps Windows awake and periodically sends either a mouse movement or a keyboard press. There is no UI; edit `appsettings.json`, start the process, and stop it with `Ctrl+C`.

## Download a ready-to-run build

Open the repository's **Releases** page and download `MouseNudge-v1.1.1-win-x64.zip`. Extract the archive and run `MouseNudge.exe`; this self-contained build does not require .NET to be installed.

## Requirements

- Windows 10 or Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) when running from source
- The .NET 10 runtime is enough for a framework-dependent published build

## Run

```powershell
dotnet run --project .\src\MouseNudge
```

The application waits for the configured interval before sending the first input. Stop it with `Ctrl+C`.

## Configuration

Edit `src/MouseNudge/appsettings.json` before running or the `appsettings.json` beside the published executable.

### Recommended VDI/client configuration

```json
{
  "MouseNudge": {
    "Mode": "Keyboard",
    "IntervalSeconds": 30,
    "LogActions": true,
    "KeepAwake": {
      "Enabled": true,
      "KeepSystemAwake": true,
      "KeepDisplayOn": true
    },
    "Mouse": {
      "Direction": "Right",
      "DistancePixels": 5,
      "ReturnToStart": true,
      "ReturnDelayMilliseconds": 150
    },
    "Keyboard": {
      "Key": "F15",
      "VirtualKeyCode": null,
      "UseScanCode": true,
      "MinPressDurationMilliseconds": 50,
      "MaxPressDurationMilliseconds": 150
    }
  }
}
```

`F15` is the default because it normally has no visible effect. Scan-code mode plus a short key-down duration is more likely to be forwarded correctly by an active RDP, Citrix, VMware, or similar VDI client. A new duration from 50 through 150 milliseconds is selected for every press using .NET's cryptographically secure random-number generator.

When MouseNudge runs on the physical client, keep the VDI window active so Windows sends the configured input to that client. When it runs inside the remote desktop, the input is sent directly into that remote Windows session.

MouseNudge does not modify Group Policy, broker settings, or server-side timeout rules. A VDI platform can deliberately ignore injected input or enforce an idle, disconnected, or maximum-session timeout. If the session still closes after an exact fixed period, the VDI administrator must change that policy; MouseNudge does not bypass it.

### Windows keep-awake

The `KeepAwake` section uses the supported Windows execution-state API while MouseNudge is running:

- `KeepSystemAwake` prevents automatic system sleep.
- `KeepDisplayOn` prevents the display idle timeout.
- `Enabled: false` disables both requests without disabling the configured input action.

These requests are released when MouseNudge exits. They cover Windows power-management timers, not every screen-lock or VDI policy.

### Mouse mode

Change `Mode` to `Mouse`. Supported directions are `Right`, `Left`, `Up`, `Down`, `UpRight`, `UpLeft`, `DownRight`, and `DownLeft`.

With `ReturnToStart` enabled, the cursor moves and then returns to its original position. It returns only if the user did not move the cursor during `ReturnDelayMilliseconds`.

### Keyboard mode

```json
{
  "MouseNudge": {
    "Mode": "Keyboard",
    "IntervalSeconds": 30,
    "LogActions": true,
    "KeepAwake": {
      "Enabled": true,
      "KeepSystemAwake": true,
      "KeepDisplayOn": true
    },
    "Mouse": {
      "Direction": "Right",
      "DistancePixels": 5,
      "ReturnToStart": true,
      "ReturnDelayMilliseconds": 150
    },
    "Keyboard": {
      "Key": "F15",
      "VirtualKeyCode": null,
      "UseScanCode": true,
      "MinPressDurationMilliseconds": 50,
      "MaxPressDurationMilliseconds": 150
    }
  }
}
```

Supported names include `A`-`Z`, `0`-`9`, `F1`-`F24`, arrow keys, `Space`, `Tab`, `Enter`, `Escape`, `Home`, `End`, `PageUp`, `PageDown`, `Insert`, and `Delete`.

`UseScanCode` sends a hardware-style key position when Windows can map the configured virtual key. Set it to `false` to use a virtual-key event instead. `MinPressDurationMilliseconds` and `MaxPressDurationMilliseconds` define the inclusive random delay between key-down and key-up. MouseNudge uses `RandomNumberGenerator.GetInt32`, not the pseudo-random `Random` class, and logs the chosen duration after each press.

To use any other Windows virtual key, set its decimal value in `VirtualKeyCode`; when this value is set, it takes precedence over `Key`.

## Useful commands

Validate the configuration without sending input:

```powershell
dotnet run --project .\src\MouseNudge -- --validate
```

Send the configured input once and exit:

```powershell
dotnet run --project .\src\MouseNudge -- --once
```

Publish a small framework-dependent executable:

```powershell
dotnet publish .\src\MouseNudge -c Release -r win-x64 --self-contained false -o .\publish
```

Keep the generated `appsettings.json` beside `MouseNudge.exe` so it remains editable.

The GitHub Actions workflow also builds and smoke-tests a self-contained `win-x64` executable and publishes the current release.

## License

MouseNudge is available under the [MIT License](LICENSE).
