using System;

namespace AgentPetCore
{
    public static class StateMapper
    {
        public static bool IsSessionEnd(AgentKind kind, string eventName)
        {
            return kind switch
            {
                AgentKind.Claude => eventName == "SessionEnd",
                AgentKind.Gemini => eventName == "SessionEnd",
                AgentKind.Cursor => eventName == "sessionEnd",
                _ => false
            };
        }

        public static AgentState? State(AgentKind kind, string eventName)
        {
            if (Enum.TryParse<AgentState>(eventName, true, out var directState))
            {
                return directState;
            }

            switch (kind)
            {
                case AgentKind.Claude:
                    return eventName switch
                    {
                        "SessionStart" => AgentState.Registered,
                        "UserPromptSubmit" or "PreToolUse" or "PostToolUse" => AgentState.Working,
                        "Notification" => AgentState.Waiting,
                        "Stop" or "SubagentStop" => AgentState.Done,
                        _ => null
                    };
                case AgentKind.Codex:
                    return eventName switch
                    {
                        "SessionStart" => AgentState.Registered,
                        "UserPromptSubmit" or "PreToolUse" or "PostToolUse" or "SubagentStart" => AgentState.Working,
                        "PermissionRequest" => AgentState.Waiting,
                        "Stop" or "SubagentStop" => AgentState.Done,
                        _ => null
                    };
                case AgentKind.Gemini:
                    return eventName switch
                    {
                        "SessionStart" => AgentState.Registered,
                        "BeforeAgent" or "BeforeModel" or "BeforeTool" or "AfterTool" or "BeforeToolSelection" or "AfterModel" => AgentState.Working,
                        "Notification" => AgentState.Waiting,
                        "AfterAgent" or "SessionEnd" => AgentState.Done,
                        _ => null
                    };
                case AgentKind.Cursor:
                    return eventName switch
                    {
                        "sessionStart" => AgentState.Registered,
                        "beforeSubmitPrompt" or "preToolUse" or "beforeShellExecution" => AgentState.Working,
                        "stop" or "subagentStop" or "sessionEnd" => AgentState.Done,
                        _ => null
                    };
                case AgentKind.Windsurf:
                    return eventName switch
                    {
                        "pre_user_prompt" => AgentState.Working,
                        "post_cascade_response" or "post_cascade_response_with_transcript" => AgentState.Done,
                        _ => null
                    };
                case AgentKind.Opencode:
                    return eventName switch
                    {
                        "session.created" => AgentState.Working,
                        "session.idle" => AgentState.Done,
                        _ => null
                    };
                case AgentKind.Cli:
                case AgentKind.Unknown:
                default:
                    return null;
            }
        }
    }
}
