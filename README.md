# Cursivis

Logitech-first cursor-native AI workflow layer for **MX Creative Console**, **MX Master 4**, and **Actions Ring**.

Cursivis turns on-screen context into direct action:

- select text, an image region, or a live browser task
- trigger from the orb today and Logitech hardware next
- get the most useful response for that context
- optionally execute the result directly in the current browser workflow

This repository is now centered on the **DevStudio 2026 by Logitech Challenge**, specifically the **MX Creative Console + MX Master 4 & Actions Ring** track.

## What Cursivis Is

Cursivis is a Windows-first interaction system built around a simple idea:

> **Selection = Context, Trigger = Intent, Cursivis = Action**

Instead of opening a chatbot and manually re-explaining context, the user stays in flow. The selected content, the active app, the trigger type, and optional voice instruction all combine into one interaction layer that feels native to Logitech’s control surfaces.

## Current Product Direction

Cursivis is being shaped as a Logitech-native productivity plugin and companion for:

- MX Creative Console
- MX Master 4 + Actions Ring
- browser-first workflows
- voice-assisted prompt refinement
- guided and smart control paths

The project is now being refined specifically as a premium Logitech ecosystem experience.

## Current Status

The current local demo already supports:

- Windows companion app with orb + result UI
- Smart and Guided modes
- text selection capture
- lasso image selection and text+image reasoning
- hold-to-talk voice capture and transcription
- result panel with insert / more options / take action
- browser-first `Take Action` execution for forms, MCQs, email, and live web tasks
- Chromium extension + native host path for acting in the user’s real logged-in tab
- Logitech plugin workstream under `plugin/logitech-plugin`
- haptics / trigger IPC / dial-oriented interaction model

## Repository Layout

```text
cursivis/
  ARCHITECTURE_PLAN.md
  docs/
  plugin/
    logitech-plugin/
  desktop/
    cursivis-companion/
    browser-action-agent/
  backend/
    gemini-agent/
  shared/
    ipc-protocol/
```

## Quick Start

1. Set `GOOGLE_API_KEY` in your terminal environment.
   The current backend implementation uses Google’s API today, but the product positioning and long-term workflow are Logitech-first.
2. Run:

```powershell
Set-Location -LiteralPath "C:\Users\Admin\OneDrive\Desktop\Cursivis! - Copy\cursivis"
powershell -ExecutionPolicy Bypass -File .\scripts\run-demo.ps1 -WithBridge -ApiKey "<YOUR_GOOGLE_API_KEY>" -EnableStreamingTranscription
```

`run-demo.ps1` starts:

- AI backend
- browser action agent
- browser extension bridge host
- WPF companion app
- optional Logitech bridge when `-WithBridge` is used

To enable `Take Action` in a real logged-in Chromium tab:

1. Load or reload the unpacked extension from `desktop/browser-extension-chromium/README.md`
2. Refresh the target tab once
3. Keep that tab active when you use `Take Action`

## Reproducible Testing

### Prerequisites

- Windows 10 or 11
- .NET 8 SDK
- Node.js 20+
- API key for the current backend implementation
- Chrome, Edge, Brave, or another Chromium-family browser

### Start The Full Local Demo

```powershell
Set-Location -LiteralPath "C:\Users\Admin\OneDrive\Desktop\Cursivis! - Copy\cursivis"
powershell -ExecutionPolicy Bypass -File .\scripts\run-demo.ps1 -WithBridge -ApiKey "<YOUR_GOOGLE_API_KEY>" -EnableStreamingTranscription
```

### Fast Smoke Test

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\smoke-test.ps1 -ApiKey "<YOUR_GOOGLE_API_KEY>"
```

### Manual Test Flows

1. Smart text flow
   - Select a long article, brief, or report and press `Trigger`
   - Expected: Cursivis chooses a useful action and returns a strong result
2. Guided flow
   - Select text in Guided mode and press `Trigger`
   - Expected: relevant options appear around the orb, including dynamic options and Custom
3. Voice flow
   - Select text, hold `Talk`, speak, then pause
   - Expected: voice is transcribed and combined with the current selection
4. Image flow
   - Trigger with image selection and analyze a lasso region
   - Expected: the selected region is interpreted correctly
5. MCQ / form flow
   - Select the question set, run `Trigger`, then press `Take Action`
   - Expected: the live browser form is filled reliably
6. Email flow
   - Select an email thread, run `Trigger`, then optionally `Take Action`
   - Expected: Cursivis drafts or inserts a useful response in the current browser workflow

## Logitech Focus

The main Logitech workstream is here:

- `plugin/logitech-plugin`

That area contains:

- plugin packaging
- bridge/runtime pieces
- Logitech-side control map
- haptic event flow
- future default profiles for MX Creative Console and Actions Ring

## Architecture

- Primary diagram: `docs/ARCHITECTURE_DIAGRAM_CHATGPT.png`
- Vector diagram: `docs/ARCHITECTURE_DIAGRAM.svg`
- Diagram notes: `docs/ARCHITECTURE_DIAGRAM.md`
- Architecture plan: `ARCHITECTURE_PLAN.md`

## Optional Cloud Deployment

The backend can still be deployed for remote demos, team sharing, or final-event proof:

- deployment notes: `docs/DEPLOYMENT_GCLOUD.md`
- deploy script: `scripts/deploy-cloudrun.ps1`

That deployment path is optional and separate from the core Logitech story.
