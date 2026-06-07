using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentPetCore
{
    public class EventServer
    {
        private const string PipeName = "AgentPetPipe";
        private readonly Action<AgentEvent> _onEventReceived;
        private CancellationTokenSource? _cts;

        public EventServer(Action<AgentEvent> onEventReceived)
        {
            _onEventReceived = onEventReceived;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ServerLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task ServerLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var serverStream = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await serverStream.WaitForConnectionAsync(token);

                    _ = Task.Run(() => HandleClientAsync(serverStream, token), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Server loop error: {ex.Message}");
                    await Task.Delay(1000, token); // Backoff on error
                }
            }
        }

        private async Task HandleClientAsync(NamedPipeServerStream stream, CancellationToken token)
        {
            try
            {
                using var reader = new StreamReader(stream);
                string? line;
                while ((line = await reader.ReadLineAsync(token)) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        var agentEvent = JsonSerializer.Deserialize<AgentEvent>(line);
                        if (agentEvent != null)
                        {
                            _onEventReceived(agentEvent);
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore malformed json
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client handler error: {ex.Message}");
            }
            finally
            {
                if (stream.IsConnected) stream.Disconnect();
                stream.Dispose();
            }
        }
    }
}
