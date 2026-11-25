# Star Citizen Telemetry Simulator

A standalone Windows x64 application for testing Star Citizen telemetry displays without starting the game. It serves synthetic frames over gRPC at `127.0.0.1:2942`.

Download the ZIP from [Releases](../../releases), extract it and run `StarCitizenTelemetrySimulator.exe`. Close Star Citizen first so the simulator can use the telemetry port. Start your telemetry client, choose On foot, EVA or Ship, and use the controls to change ships, randomise readings or exercise player alerts.

## Development

Use the .NET 10 SDK on Windows. Run `dotnet run --project src/TelemetrySimulator` to launch the simulator. Feature branches target main through pull requests. CI starts the simulator and checks a frame with a separate gRPC client. Use Actions → Release simulator on main to publish a self-contained build and its SHA-256 checksum.
