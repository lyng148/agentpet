using System;
using System.Text.Json.Serialization;

namespace AgentPetCore
{
    public enum AgentKind
    {
        [JsonPropertyName("claude")]
        Claude,
        [JsonPropertyName("codex")]
        Codex,
        [JsonPropertyName("gemini")]
        Gemini,
        [JsonPropertyName("cursor")]
        Cursor,
        [JsonPropertyName("windsurf")]
        Windsurf,
        [JsonPropertyName("opencode")]
        Opencode,
        [JsonPropertyName("cli")]
        Cli,
        [JsonPropertyName("unknown")]
        Unknown
    }

    public enum AgentState
    {
        [JsonPropertyName("working")]
        Working,
        [JsonPropertyName("waiting")]
        Waiting,
        [JsonPropertyName("done")]
        Done,
        [JsonPropertyName("registered")]
        Registered,
        [JsonPropertyName("idle")]
        Idle
    }

    public enum PetMood
    {
        [JsonPropertyName("idle")]
        Idle,
        [JsonPropertyName("working")]
        Working,
        [JsonPropertyName("waiting")]
        Waiting,
        [JsonPropertyName("done")]
        Done,
        [JsonPropertyName("celebrate")]
        Celebrate
    }

    public class AgentEvent
    {
        [JsonPropertyName("agentKind")]
        public AgentKind AgentKind { get; set; } = AgentKind.Unknown;

        [JsonPropertyName("eventName")]
        public string EventName { get; set; } = string.Empty;

        [JsonPropertyName("sessionId")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public enum SessionSource
    {
        Hook,
        Cli
    }

    public class AgentSession
    {
        public string Id { get; set; } = string.Empty;
        public AgentKind AgentKind { get; set; } = AgentKind.Unknown;
        public string? Project { get; set; }
        public AgentState State { get; set; } = AgentState.Idle;
        public string? Message { get; set; }
        public SessionSource Source { get; set; } = SessionSource.Hook;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime StateSince { get; set; } = DateTime.UtcNow;
    }
}
