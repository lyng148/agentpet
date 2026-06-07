<div align="center">

<img src="icon.png" width="120" alt="AgentPet" />

# AgentPet

**A desktop pet for Windows that reacts to your AI coding agent in real time.**

When your agent is thinking, working, or waiting for your input, AgentPet shows it — right on your desktop, so you never have to babysit a terminal.

<video controls src="20260607-1107-22.1467086.mp4" title="AgentPet demo"></video>

[English](README.md) · [Tiếng Việt](docs/readme/README.vi.md) · [日本語](docs/readme/README.ja.md) · [简体中文](docs/readme/README.zh-Hans.md)

</div>

---

## Features

- **Live agent status** — the pet animates through *working*, *waiting*, and *done* states as your agent runs.
- **Needs-your-attention alerts** — when the agent pauses for permission or input, the pet switches to a waiting mood and surfaces a chat bubble.
- **Multi-agent support** — works with Claude Code, Codex, Gemini, Cursor, Windsurf, and OpenCode hooks.
- **Always-on companion** — a lightweight tray app that sits quietly until something happens.
- **Customizable pets** — swap in your own sprite packs.

## Supported agents

| Agent | How it connects |
| --- | --- |
| Claude Code | Lifecycle hooks (`SessionStart`, `Notification`, `Stop`, …) |
| Codex | Lifecycle hooks |
| Gemini | Lifecycle hooks |
| Cursor | Lifecycle hooks |
| Windsurf | Cascade hooks |
| OpenCode | Session events |

## Installation

### From the installer (recommended)

1. Download `AgentPet-Setup.exe` from the [latest release](../../releases/latest).
2. Run it and follow the wizard. It installs to `%LocalAppData%\AgentPet` and creates a Start Menu shortcut (desktop and run-at-startup are optional).
3. Launch AgentPet. It registers the agent hooks for you on first run.

> The installer is self-contained — you do **not** need to install the .NET runtime separately.

### From source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download).

```powershell
git clone https://github.com/lyng148/agentpet.git
cd agentpet
dotnet run --project AgentPetApp
```

## How it works

```
┌─────────────┐   hook event    ┌──────────────┐   named pipe   ┌──────────────┐
│  AI agent   │ ───────────────▶│ agentpet CLI │ ──────────────▶│ AgentPet app │
│ (Claude...) │   (JSON stdin)  │   hook cmd   │   AgentPetPipe │   (the pet)  │
└─────────────┘                 └──────────────┘                └──────────────┘
```

1. Your agent fires a lifecycle hook (for example, Claude Code's `Notification` event when it needs permission).
2. The hook runs `agentpet hook`, which reads the event JSON and forwards it over a local named pipe.
3. The AgentPet app maps the event to a mood and animates the pet accordingly.

State mapping (Claude Code example):

| Event | Pet state |
| --- | --- |
| `SessionStart` | registered |
| `UserPromptSubmit`, `PreToolUse`, `PostToolUse` | working |
| `Notification` | waiting (needs your attention) |
| `Stop`, `SubagentStop` | done |

## Building the installer

The repo ships a one-step build script that publishes the app and packages it with [Inno Setup](https://jrsoftware.org/isinfo.php):

```powershell
# Requires the Inno Setup compiler (iscc) on your machine:
#   winget install -e --id JRSoftware.InnoSetup

dotnet publish AgentPetApp/AgentPetApp.csproj -c Release -r win-x64 --self-contained true
iscc AgentPet.iss
```

The resulting installer is written to `ReleasePackage\AgentPet-Setup.exe`.

## Releasing

Pushing a version tag triggers the [release workflow](.github/workflows/release.yml), which builds the installer on Windows and publishes it as a GitHub Release asset:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## Project layout

| Path | Purpose |
| --- | --- |
| `AgentPetApp/` | WPF desktop app (the pet, tray icon, settings) |
| `AgentPetCore/` | Shared core: event model, state mapping, IPC server, hook installer |
| `AgentPetCLI/` | `agentpet` command-line hook bridge |
| `AgentPetInstaller/` | Self-extracting installer (alternative to Inno Setup) |
| `pets/` | Bundled sprite packs |

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines, and please open an issue to discuss substantial changes before submitting a pull request.

## License

Released under the terms of the [LICENSE](LICENSE) file in this repository.
