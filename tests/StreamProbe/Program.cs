using Grpc.Core;
using Grpc.Net.Client;
using StarCitizenMobiGlass.Telemetry.Generated;

using var channel = GrpcChannel.ForAddress("http://127.0.0.1:2942");
var client = new GameTelemetryService.GameTelemetryServiceClient(channel);
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
while (true)
{
    try
    {
        using var call = client.TelemetryStream(new TelemetryStreamRequest(), cancellationToken: timeout.Token);
        if (!await call.ResponseStream.MoveNext(timeout.Token))
            throw new InvalidOperationException("Stream ended before a frame arrived.");
        var actor = call.ResponseStream.Current.Actor;
        if (actor?.State?.Locomotion != ActorLocomotionState.OnFoot || actor.Vitals?.Health is null)
            throw new InvalidOperationException("Expected on-foot vitals at startup.");
        Console.WriteLine("Received on-foot vitals from the simulator.");
        break;
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable && !timeout.IsCancellationRequested)
    {
        await Task.Delay(250, timeout.Token);
    }
}
