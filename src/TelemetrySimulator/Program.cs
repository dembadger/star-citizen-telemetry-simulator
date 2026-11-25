using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using StarCitizenMobiGlass.Telemetry.Generated;
using StarCitizenMobiGlass.TelemetryHarness;

namespace StarCitizenMobiGlass.TelemetrySimulator;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        ApplicationConfiguration.Initialize();
        var state = new SimulatorState();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 2942,
            listener => listener.Protocols = HttpProtocols.Http2));
        builder.Services.AddGrpc();
        builder.Services.AddSingleton(state);
        var host = builder.Build();
        host.MapGrpcService<ManualTelemetryService>();
        try
        {
            host.StartAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not start the simulator on 127.0.0.1:2942. Close Star Citizen or any other simulator using that port.\n\n{ex.GetBaseException().Message}",
                "Telemetry simulator", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            host.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        try
        {
            Application.Run(new SimulatorWindow(state));
        }
        finally
        {
            host.StopAsync().GetAwaiter().GetResult();
            host.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}

public sealed class ManualTelemetryService(SimulatorState state) : GameTelemetryService.GameTelemetryServiceBase
{
    public override async Task TelemetryStream(TelemetryStreamRequest request,
        IServerStreamWriter<TelemetryStreamResponse> stream, ServerCallContext context)
    {
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await stream.WriteAsync(state.Current);
            await Task.Delay(250, context.CancellationToken);
        }
    }
}

public sealed record SimulatorShip(string Name, string ClassName, string Manufacturer, long Instance)
{
    public static SimulatorShip[] Fleet { get; } =
    [
        new("C8R Pisces", "ANVL_C8R_Pisces", "Anvil", 930568944773),
        new("Gladius", "AEGS_Gladius", "Aegis", 1002),
        new("Cutlass Black", "DRAK_Cutlass_Black", "Drake", 1003),
        new("Vulture", "DRAK_Vulture", "Drake", 1004),
        new("Prospector", "MISC_Prospector", "MISC", 1005),
        new("Constellation Andromeda", "RSI_Constellation_Andromeda", "RSI", 1006)
    ];
}

public sealed class SimulatorState
{
    private readonly IReadOnlyList<TelemetryStreamResponse> _examples = SyntheticScenarios.Frames();
    private TelemetryStreamResponse _current;
    public TelemetryStreamResponse Current => Volatile.Read(ref _current);

    public SimulatorState() => _current = _examples[0];

    public void ApplyPreset(AlertPreset preset) => Volatile.Write(ref _current, PresetFrames.Apply(Current, preset));

    public void Select(ActorLocomotionState mode, SimulatorShip ship)
    {
        var frame = _examples.Single(example => example.Actor.State.Locomotion == mode).Clone();
        var random = Random.Shared;
        var vitals = frame.Actor.Vitals;
        vitals.Health.Current = random.Next(40, 101);
        vitals.Oxygen.Current = random.Next(10, 101);
        vitals.Hunger.Current = random.Next(1, 101);
        vitals.Thirst.Current = random.Next(1, 101);
        vitals.BodyTemperature.Current = random.Next(345, 391) / 10f;
        vitals.HeartRate = random.Next(55, 141);
        if (frame.Vehicle is { } vehicle)
        {
            vehicle.State.Name = $"{ship.ClassName}_{ship.Instance}";
            vehicle.State.ClassName = ship.ClassName;
            vehicle.State.Manufacturer = ship.Manufacturer;
            var instruments = vehicle.State.Flight.Instruments;
            instruments.Speed = random.Next(0, 501);
            instruments.VerticalSpeedIndicator = random.Next(-25, 26);
            vehicle.State.Flight.State.Gear = random.Next(2) == 0 ? VehicleComponentDeploymentState.Retracted : VehicleComponentDeploymentState.Deployed;
            instruments.Heading = random.Next(360);
            instruments.RadarAltitude = random.Next(1, 2001);
            instruments.Altitude = instruments.RadarAltitude + random.Next(0, 501);
            instruments.HydrogenFuel = random.Next(0, 101);
            instruments.QuantumFuel = random.Next(0, 101);
            vehicle.State.Shields.Level = random.Next(0, 101);
        }

        Volatile.Write(ref _current, frame);
    }
}

internal sealed class SimulatorWindow : Form
{
    private readonly SimulatorState _state;
    private readonly ComboBox _ships = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Label _readings = new() { AutoSize = true, Margin = new Padding(4, 12, 4, 4) };

    public SimulatorWindow(SimulatorState state)
    {
        _state = state;
        Text = "Star Citizen telemetry simulator";
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MaximizeBox = false;
        var layout = new FlowLayoutPanel
        {
            AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, Padding = new Padding(16)
        };
        Controls.Add(layout);
        layout.Controls.Add(new Label { AutoSize = true, Text = "Streaming locally on 127.0.0.1:2942" });
        var picker = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        picker.Controls.Add(new Label { AutoSize = true, Text = "Test ship" });
        _ships.DisplayMember = nameof(SimulatorShip.Name);
        _ships.DataSource = SimulatorShip.Fleet;
        _ships.SelectedIndexChanged += (_, _) =>
        {
            if (_state.Current.Actor.State.Locomotion == ActorLocomotionState.Seated)
                SetState(ActorLocomotionState.Seated);
        };
        picker.Controls.Add(_ships);
        layout.Controls.Add(picker);
        var buttons = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        layout.Controls.Add(buttons);
        foreach (var (caption, mode) in new[] { ("On foot", ActorLocomotionState.OnFoot),
            ("EVA", ActorLocomotionState.Eva), ("Ship", ActorLocomotionState.Seated) })
        {
            var button = new Button { Text = caption, AutoSize = true };
            button.Click += (_, _) => SetState(mode);
            buttons.Controls.Add(button);
        }

        var randomise = new Button { Text = "Randomise readings", AutoSize = true };
        randomise.Click += (_, _) => SetState(_state.Current.Actor.State.Locomotion);
        buttons.Controls.Add(randomise);
        var presets = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        foreach (var (label, preset) in new[] { ("Low hunger", AlertPreset.LowHunger), ("Low thirst", AlertPreset.LowThirst),
            ("Low oxygen", AlertPreset.LowOxygen), ("Unbreathable", AlertPreset.Unbreathable), ("Recover", AlertPreset.Recover) })
        {
            var button = new Button { Text = label, AutoSize = true };
            button.Click += (_, _) => { _state.ApplyPreset(preset); ShowReadings(); };
            presets.Controls.Add(button);
        }
        layout.Controls.Add(presets);
        layout.Controls.Add(_readings);
        SetState(ActorLocomotionState.OnFoot);
    }

    private void SetState(ActorLocomotionState mode)
    {
        var ship = _ships.SelectedItem as SimulatorShip ?? SimulatorShip.Fleet[0];
        _state.Select(mode, ship);
        ShowReadings();
    }

    private void ShowReadings()
    {
        var frame = _state.Current;
        var vitals = frame.Actor.Vitals;
        _readings.Text = $"{frame.Actor.State.Locomotion}    Health {vitals.Health.Current}%    Oxygen {vitals.Oxygen.Current}%\n" +
            $"Hunger {vitals.Hunger.Current}%    Thirst {vitals.Thirst.Current}%    Heart rate {vitals.HeartRate} bpm" +
            $"\nAtmosphere: {(frame.Actor.Environment.BreathableAtmosphere ? "breathable" : "not breathable")}";
        if (frame.Vehicle is { } vehicle)
            _readings.Text += $"\n{(_ships.SelectedItem as SimulatorShip)?.Name}    Speed {vehicle.State.Flight.Instruments.Speed} m/s    Heading {vehicle.State.Flight.Instruments.Heading}°";
    }
}
