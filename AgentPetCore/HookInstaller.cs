using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentPetCore
{
    public static class HookInstaller
    {
        private static bool IsOurs(string command) =>
            !string.IsNullOrEmpty(command) &&
            command.Contains("hook", StringComparison.OrdinalIgnoreCase) &&
            (command.Contains("AgentPetCLI", StringComparison.OrdinalIgnoreCase) ||
             command.Contains("AgentPetCodexHook", StringComparison.OrdinalIgnoreCase));

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
            var hookExecutablePath = GetHookExecutablePath(kind, cliExecutablePath);
            var baseCommand = BuildBaseCommand(kind, hookExecutablePath, agentName);

            var events = GetEvents(kind);
            foreach (var ev in events)
            {
                var command = $"{baseCommand} --event {ev}";

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
                    if (kind == AgentKind.Codex)
                    {
                        ourHook["command_windows"] = command;
                    }
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

        private static string GetHookExecutablePath(AgentKind kind, string cliExecutablePath)
        {
            if (kind != AgentKind.Codex)
            {
                return cliExecutablePath;
            }

            var cliDir = Path.GetDirectoryName(cliExecutablePath);
            if (string.IsNullOrEmpty(cliDir))
            {
                return cliExecutablePath;
            }

            var wrapperPath = Path.Combine(cliDir, "AgentPetCodexHook.cmd");
            return File.Exists(wrapperPath) ? wrapperPath : cliExecutablePath;
        }

        private static string BuildBaseCommand(AgentKind kind, string hookExecutablePath, string agentName)
        {
            var commandPath = hookExecutablePath;
            if (kind == AgentKind.Codex)
            {
                commandPath = GetUnquotedCodexCommandPath(hookExecutablePath);
                if (!commandPath.Contains(' '))
                {
                    return $"{commandPath} hook --agent {agentName}";
                }
            }

            return $"\"{commandPath}\" hook --agent {agentName}";
        }

        private static string GetUnquotedCodexCommandPath(string path)
        {
            if (!path.Contains(' '))
            {
                return path;
            }

            var shortPath = TryGetShortPath(path);
            return !string.IsNullOrEmpty(shortPath) && !shortPath.Contains(' ') ? shortPath : path;
        }

        private static string? TryGetShortPath(string path)
        {
            if (!OperatingSystem.IsWindows())
            {
                return null;
            }

            var capacity = GetShortPathName(path, null, 0);
            if (capacity == 0)
            {
                return null;
            }

            var buffer = new StringBuilder(capacity);
            var length = GetShortPathName(path, buffer, buffer.Capacity);
            return length > 0 ? buffer.ToString() : null;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetShortPathName(string longPath, StringBuilder? shortPath, int bufferLength);

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
