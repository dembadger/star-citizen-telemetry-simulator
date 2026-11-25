using StarCitizenMobiGlass.Telemetry.Generated;

namespace StarCitizenMobiGlass.TelemetryHarness;

public static class SyntheticScenarios
{
    public static IReadOnlyList<TelemetryStreamResponse> Frames() =>
    [
        new TelemetryStreamResponse
        {
            Actor = new ActorFrame
            {
                State = new ActorState { Locomotion = ActorLocomotionState.OnFoot },
                Vitals = new ActorVitals
                {
                    Health = new Gauge { Current = 100, Max = 100 },
                    Oxygen = new Gauge { Current = 100, Max = 100 },
                    Hunger = new Gauge { Current = 72, Max = 100 },
                    Thirst = new Gauge { Current = 61, Max = 100 },
                    BodyTemperature = new Gauge { Current = 36.5f, Max = 100 },
                    HeartRate = 68,
                    Pressure = 1
                },
                Environment = new ActorEnvironment
                {
                    ExternalTemperature = 21,
                    BreathableAtmosphere = true
                }
            }
        },
        new TelemetryStreamResponse
        {
            Actor = new ActorFrame
            {
                State = new ActorState { Locomotion = ActorLocomotionState.Eva },
                Vitals = new ActorVitals
                {
                    Health = new Gauge { Current = 87, Max = 100 },
                    Oxygen = new Gauge { Current = 54, Max = 100 },
                    Hunger = new Gauge { Current = 72, Max = 100 },
                    Thirst = new Gauge { Current = 61, Max = 100 },
                    BodyTemperature = new Gauge { Current = 34.8f, Max = 100 },
                    HeartRate = 94
                },
                Environment = new ActorEnvironment
                {
                    ExternalTemperature = -120,
                    BreathableAtmosphere = false
                }
            }
        },
        new TelemetryStreamResponse
        {
            Vehicle = new VehicleFrame
            {
                State = new VehicleState
                {
                    Name = "ANVL_C8R_Pisces_930568944773",
                    ClassName = "ANVL_C8R_Pisces",
                    Flight = new VehicleFlight
                    {
                        State = new VehicleFlightState { IsLanded = false },
                        Instruments = new VehicleFlightInstruments
                        {
                            Speed = 142,
                            Heading = 271,
                            Altitude = float.MaxValue,
                            RadarAltitude = 42,
                            HydrogenFuel = 0.83f,
                            QuantumFuel = 0.67f
                        }
                    },
                    Shields = new VehicleShields { Level = 0.96f },
                    MasterMode = new VehicleMasterModeState { Mode = VehicleMasterMode.Scm }
                }
            },
            Actor = new ActorFrame
            {
                State = new ActorState
                {
                    Locomotion = ActorLocomotionState.Seated,
                    CrewRole = ActorCrewRole.Pilot
                },
                Vitals = new ActorVitals
                {
                    Health = new Gauge { Current = 100, Max = 100 },
                    Oxygen = new Gauge { Current = 100, Max = 100 },
                    Hunger = new Gauge { Current = 72, Max = 100 },
                    Thirst = new Gauge { Current = 61, Max = 100 },
                    BodyTemperature = new Gauge { Current = 36.5f, Max = 100 },
                    HeartRate = 68
                },
                Environment = new ActorEnvironment { BreathableAtmosphere = true }
            }
        }
    ];
}

public enum AlertPreset { LowHunger, LowThirst, LowOxygen, Unbreathable, Recover }

public static class PresetFrames
{
    public static TelemetryStreamResponse Apply(TelemetryStreamResponse current, AlertPreset preset)
    {
        var frame = current.Clone();
        var actor = frame.Actor;
        var vitals = actor.Vitals;
        switch (preset)
        {
            case AlertPreset.LowHunger: vitals.Hunger.Current = 5; break;
            case AlertPreset.LowThirst: vitals.Thirst.Current = 5; break;
            case AlertPreset.LowOxygen: vitals.Oxygen.Current = 5; break;
            case AlertPreset.Unbreathable: actor.Environment.BreathableAtmosphere = false; break;
            case AlertPreset.Recover:
                vitals.Hunger.Current = vitals.Hunger.Max;
                vitals.Thirst.Current = vitals.Thirst.Max;
                vitals.Oxygen.Current = vitals.Oxygen.Max;
                actor.Environment.BreathableAtmosphere = actor.State.Locomotion != ActorLocomotionState.Eva;
                break;
        }
        return frame;
    }
}
