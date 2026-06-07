using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentPetCore
{
    public static class HookInstaller
    {
        private static bool IsOurs(string command) => command != null && command.Contains("AgentPetCLI") && command.Contains("hook");

        public static string GetSettingsPath(AgentKind kind)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return kind switch
            {
                AgentKind.Claude => Path.Combine(home, ".claude", "settings.json"),
                AgentKind.Codex => Path.Combine(home, ".codex", "hooks.json"),
                AgentKind.Gemini => Path.Combine(home, ".gemini", "settings.json"),
                AgentKind.Cursor => Path.Combine(home, ".cursor", "hooks.json"),
                AgentKind.Windsurf => Path.Combine(home, ".codeium", "windsurf", "hooks.json"),
                _ => throw new ArgumentException("Unsupported agent kind")
            };
        }

        public static string[] GetEvents(AgentKind kind)
        {
            return kind switch
            {
                AgentKind.Claude => new[] { "SessionStart", "UserPromptSubmit", "PreToolUse", "Notification", "Stop", "SubagentStop", "SessionEnd" },
                AgentKind.Codex => new[] { "SessionStart", "UserPromptSubmit", "PreToolUse", "PermissionRequest", "Stop", "SubagentStop" },
                AgentKind.Gemini => new[] { "SessionStart", "BeforeAgent", "BeforeTool", "AfterTool", "Notification", "AfterAgent", "SessionEnd" },
                AgentKind.Cursor => new[] { "sessionStart", "beforeSubmitPrompt", "preToolUse", "stop", "subagentStop", "sessionEnd" },
                AgentKind.Windsurf => new[] { "pre_user_prompt", "post_cascade_response" },
                _ => Array.Empty<string>()
            };
        }

        public enum HookStyle
        {
            Nested,
            CursorFlat,
            WindsurfFlat
        }

        public static HookStyle GetStyle(AgentKind kind)
        {
            return kind switch
            {
                AgentKind.Cursor => HookStyle.CursorFlat,
                AgentKind.Windsurf => HookStyle.WindsurfFlat,
                _ => HookStyle.Nested
            };
        }

        public static bool IsInstalled(AgentKind kind)
        {
            try
            {
                var path = GetSettingsPath(kind);
                if (!File.Exists(path)) return false;

                var json = File.ReadAllText(path);
                var root = JsonNode.Parse(json)?.AsObject();
                if (root == null || !root.ContainsKey("hooks")) return false;

                var hooks = root["hooks"]?.AsObject();
                if (hooks == null) return false;

                var events = GetEvents(kind);
                var style = GetStyle(kind);

                foreach (var ev in events)
                {
                    if (hooks.ContainsKey(ev) && hooks[ev] is JsonArray groups)
                    {
                        foreach (var group in groups)
                        {
                            if (group is JsonObject g)
                            {
                                if (style == HookStyle.Nested)
                                {
                                    if (GroupIsOurs(g)) return true;
                                }
                                else
                                {
                                    if (FlatItemIsOurs(g)) return true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool GroupIsOurs(JsonObject group)
        {
            if (group.ContainsKey("hooks") && group["hooks"] is JsonArray innerHooks)
            {
                foreach (var hook in innerHooks)
                {
                    if (hook is JsonObject h && h.ContainsKey("command"))
                    {
                        var cmd = h["command"]?.ToString();
                        if (IsOurs(cmd)) return true;
                    }
                }
            }
            return false;
        }

        private static bool FlatItemIsOurs(JsonObject item)
        {
            if (item.ContainsKey("command"))
            {
                var cmd = item["command"]?.ToString();
                return IsOurs(cmd);
            }
            return false;
        }

        public static void Install(AgentKind kind, string cliExecutablePath)
        {
            var path = GetSettingsPath(kind);
            JsonObject root;

            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            else
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                root = new JsonObject();
            }

            var style = GetStyle(kind);
            if (style == HookStyle.CursorFlat && !root.ContainsKey("version"))
            {
                root["version"] = 1;
            }

            if (!root.ContainsKey("hooks"))
            {
                root["hooks"] = new JsonObject();
            }
            var hooks = root["hooks"].AsObject();

            var agentName = kind.ToString().ToLowerInvariant();
            var command = $"\"{cliExecutablePath}\" hook --agent {agentName}";

            var events = GetEvents(kind);
            foreach (var ev in events)
            {
                if (!hooks.ContainsKey(ev))
                {
                    hooks[ev] = new JsonArray();
                }
                var groups = hooks[ev].AsArray();

                var newGroups = new JsonArray();
                foreach (var g in groups)
                {
                    if (g is JsonObject obj)
                    {
                        if (style == HookStyle.Nested && !GroupIsOurs(obj))
                        {
                            newGroups.Add(JsonNode.Parse(g.ToJsonString()));
                        }
                        else if (style != HookStyle.Nested && !FlatItemIsOurs(obj))
                        {
                            newGroups.Add(JsonNode.Parse(g.ToJsonString()));
                        }
                    }
                    else
                    {
                        newGroups.Add(JsonNode.Parse(g.ToJsonString()));
                    }
                }

                if (style == HookStyle.Nested)
                {
                    var ourHook = new JsonObject
                    {
                        ["type"] = "command",
                        ["command"] = command
                    };
                    var ourGroup = new JsonObject
                    {
                        ["hooks"] = new JsonArray { ourHook }
                    };
                    newGroups.Add(ourGroup);
                }
                else
                {
                    var entry = new JsonObject
                    {
                        ["command"] = command
                    };
                    if (style == HookStyle.CursorFlat)
                    {
                        entry["type"] = "command";
                    }
                    else if (style == HookStyle.WindsurfFlat)
                    {
                        entry["show_output"] = false;
                    }
                    newGroups.Add(entry);
                }

                hooks[ev] = newGroups;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options));
        }

        public static void Uninstall(AgentKind kind)
        {
            var path = GetSettingsPath(kind);
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path);
                var root = JsonNode.Parse(json)?.AsObject();
                if (root == null || !root.ContainsKey("hooks")) return;

                var hooks = root["hooks"]?.AsObject();
                if (hooks == null) return;

                var events = GetEvents(kind);
                var style = GetStyle(kind);

                foreach (var ev in events)
                {
                    if (hooks.ContainsKey(ev) && hooks[ev] is JsonArray groups)
                    {
                        var newGroups = new JsonArray();
                        foreach (var g in groups)
                        {
                            if (g is JsonObject obj)
                            {
                                if (style == HookStyle.Nested && !GroupIsOurs(obj))
                                {
                                    newGroups.Add(JsonNode.Parse(g.ToJsonString()));
                                }
                                else if (style != HookStyle.Nested && !FlatItemIsOurs(obj))
                                {
                                    newGroups.Add(JsonNode.Parse(g.ToJsonString()));
                                }
                            }
                            else
                            {
                                newGroups.Add(JsonNode.Parse(g.ToJsonString()));
                            }
                        }

                        if (newGroups.Count == 0)
                        {
                            hooks.Remove(ev);
                        }
                        else
                        {
                            hooks[ev] = newGroups;
                        }
                    }
                }

                if (hooks.Count == 0)
                {
                    root.Remove("hooks");
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(path, root.ToJsonString(options));
            }
            catch { }
        }
    }
}
