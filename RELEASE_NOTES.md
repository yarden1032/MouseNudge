# MouseNudge v1.1.1

Cryptographically randomized key-duration update.

- Selects a new key-down duration for every press using `RandomNumberGenerator.GetInt32`.
- Uses a configurable, inclusive range of 50-150 milliseconds by default.
- Logs the duration that was actually used for each keyboard press.
- Retains the VDI-friendly scan-code input and Windows keep-awake behavior from v1.1.0.
- Includes a self-contained `win-x64` executable; no .NET installation is required.

Download `MouseNudge-v1.1.1-win-x64.zip`, extract it, edit `appsettings.json` if needed, and run `MouseNudge.exe`.
