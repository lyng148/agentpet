using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgentPetCore
{
    public static class EventSender
    {
        private const string PipeName = "AgentPetPipe";

        public static async Task<bool> SendAsync(AgentEvent ev)
        {
            try
            {
                var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                string json = JsonSerializer.Serialize(ev, options);

                using var clientStream = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.Asynchronous);
                
                // Try to connect with a short timeout. If daemon is not running, we fail fast.
                await clientStream.ConnectAsync(500);

                using var writer = new StreamWriter(clientStream);
                await writer.WriteLineAsync(json);
                await writer.FlushAsync();
                
                return true;
            }
            catch (TimeoutException)
            {
                // Fallback: write to queue dir (if needed) or just return false
                // On macOS it wrote to a queue dir for later processing.
                WriteToQueue(ev);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send event: {ex.Message}");
                return false;
            }
        }

        private static void WriteToQueue(AgentEvent ev)
        {
            try
            {
                var queueDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AgentPet", "Queue");
                Directory.CreateDirectory(queueDir);
                
                var name = $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}-{Guid.NewGuid()}.json";
                var fullPath = Path.Combine(queueDir, name);
                
                var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                File.WriteAllText(fullPath, JsonSerializer.Serialize(ev, options));
            }
            catch
            {
                // Ignore queue write errors silently as per Swift version
            }
        }
    }
}
