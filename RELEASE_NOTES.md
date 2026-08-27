# MouseNudge v1.1.0

VDI and keep-awake reliability update.

- Keeps the local or remote Windows system and display awake through the Windows execution-state API.
- Sends keyboard input as scan codes by default, with a configurable key-down duration for better remote-client forwarding.
- Returns the mouse using a second input event so VDI clients can observe both movements.
- Uses the VDI-friendly `F15` keyboard mode in the bundled default configuration.
- Documents the boundary between client-side input and administrator-enforced VDI timeout policies.
- Includes a self-contained `win-x64` executable; no .NET installation is required.

Download `MouseNudge-v1.1.0-win-x64.zip`, extract it, edit `appsettings.json` if needed, and run `MouseNudge.exe`.
