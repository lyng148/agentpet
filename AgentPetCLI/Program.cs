using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AgentPetCore;

namespace AgentPetCLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || args[0] != "hook")
            {
                Console.WriteLine("Usage: agentpet hook --agent <agent> [--event <event>]");
                return 1;
            }

            var agentEvent = new AgentEvent();
            
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--agent" when i + 1 < args.Length:
                        if (Enum.TryParse<AgentKind>(args[++i], true, out var kind))
                            agentEvent.AgentKind = kind;
                        break;
                    case "--event" when i + 1 < args.Length:
                        agentEvent.EventName = args[++i];
                        break;
                    case "--session" when i + 1 < args.Length:
                        agentEvent.SessionId = args[++i];
                        break;
                    case "--project" when i + 1 < args.Length:
                        agentEvent.Project = args[++i];
                        break;
                }
            }

            string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AgentPet", "cli_debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now}] Starting hook. args: {string.Join(" ", args)}\n");
            
            // If Claude Code pipes JSON into STDIN, read it
            if (Console.IsInputRedirected)
            {
                try
                {
                    string json = await Console.In.ReadToEndAsync();
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Received STDIN: {json}\n");
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var root = JsonNode.Parse(json)?.AsObject();
                        if (root != null)
                        {
                            if (root.ContainsKey("hook_event_name") && string.IsNullOrEmpty(agentEvent.EventName))
                                agentEvent.EventName = root["hook_event_name"]?.ToString();
                            else if (root.ContainsKey("type") && string.IsNullOrEmpty(agentEvent.EventName))
                                agentEvent.EventName = root["type"]?.ToString();
                                
                            if (root.ContainsKey("session_id") && string.IsNullOrEmpty(agentEvent.SessionId))
                                agentEvent.SessionId = root["session_id"]?.ToString();
                                
                            if (root.ContainsKey("cwd") && string.IsNullOrEmpty(agentEvent.Project))
                                agentEvent.Project = root["cwd"]?.ToString();
                            else if (root.ContainsKey("project_path") && string.IsNullOrEmpty(agentEvent.Project))
                                agentEvent.Project = root["project_path"]?.ToString();
                        }
                    }
                }
                catch (Exception ex) { File.AppendAllText(logPath, $"[{DateTime.Now}] Parsing error: {ex}\n"); }
            }
            else
            {
                File.AppendAllText(logPath, $"[{DateTime.Now}] No STDIN redirected.\n");
            }

            if (string.IsNullOrEmpty(agentEvent.SessionId))
                agentEvent.SessionId = "default";
                
            if (string.IsNullOrEmpty(agentEvent.EventName))
            {
                Console.WriteLine("Error: EventName missing. Either pass --event or pipe JSON payload.");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Error: EventName missing.\n");
                return 1;
            }

            bool success = await EventSender.SendAsync(agentEvent);
            if (!success)
            {
                Console.WriteLine("Failed to send event. Is the AgentPet daemon running?");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Failed to send event (IPC failure).\n");
                return 1;
            }
            
            File.AppendAllText(logPath, $"[{DateTime.Now}] Success! Sent event: {agentEvent.EventName}\n");
            
            Console.WriteLine("Event sent successfully.");
            return 0;
        }
    }
}
