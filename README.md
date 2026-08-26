# MouseNudge

A tiny, configuration-only Windows console application that periodically sends either a mouse movement or a keyboard press. There is no UI; edit `appsettings.json`, start the process, and stop it with `Ctrl+C`.

## Download a ready-to-run build

Open the repository's **Releases** page and download `MouseNudge-v1.0.0-win-x64.zip`. Extract the archive and run `MouseNudge.exe`; this self-contained build does not require .NET to be installed.

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

### Mouse mode

```json
{
  "MouseNudge": {
    "Mode": "Mouse",
    "IntervalSeconds": 30,
    "LogActions": true,
    "Mouse": {
      "Direction": "Right",
      "DistancePixels": 5,
      "ReturnToStart": true,
      "ReturnDelayMilliseconds": 150
    },
    "Keyboard": {
      "Key": "F15",
      "VirtualKeyCode": null
    }
  }
}
```

Supported directions are `Right`, `Left`, `Up`, `Down`, `UpRight`, `UpLeft`, `DownRight`, and `DownLeft`.

With `ReturnToStart` enabled, the cursor moves and then returns to its original position. It returns only if the user did not move the cursor during `ReturnDelayMilliseconds`.

### Keyboard mode

Change `Mode` to `Keyboard`:

```json
{
  "MouseNudge": {
    "Mode": "Keyboard",
    "IntervalSeconds": 30,
    "LogActions": true,
    "Mouse": {
      "Direction": "Right",
      "DistancePixels": 5,
      "ReturnToStart": true,
      "ReturnDelayMilliseconds": 150
    },
    "Keyboard": {
      "Key": "F15",
      "VirtualKeyCode": null
    }
  }
}
```

`F15` is the default because it normally has no visible effect. Supported names include `A`-`Z`, `0`-`9`, `F1`-`F24`, arrow keys, `Space`, `Tab`, `Enter`, `Escape`, `Home`, `End`, `PageUp`, `PageDown`, `Insert`, and `Delete`.

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

The GitHub Actions workflow also builds a self-contained `win-x64` executable and creates the initial `v1.0.0` GitHub release automatically.

## License

MouseNudge is available under the [MIT License](LICENSE).
