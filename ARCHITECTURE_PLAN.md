# Cursivis Architecture Plan

## 1. Objective

Cursivis must deliver a premium, Logitech-native workflow layer where **MX Creative Console**, **MX Master 4**, and **Actions Ring** become the control surface for context-aware AI actions.

Primary competition target:

- **DevStudio 2026 by Logitech**
- Category: **MX Creative Console + MX Master 4 & Actions Ring**

Primary demo KPI:

- short-context interactions should feel immediate, clear, and hardware-native

## 2. System Components

### A. Logitech Plugin (`plugin/logitech-plugin`)

- Runtime: C# + Logi Actions SDK
- Role:
  - receive trigger actions
  - receive dial / ring style control input
  - forward commands to the companion over local IPC
  - emit haptic feedback events

### B. Companion App (`desktop/cursivis-companion`)

- Runtime: C# + WPF
- Role:
  - capture current context from the active workflow
  - render orb and result UI
  - orchestrate Smart / Guided / Talk / Snip-it / Action flows
  - manage clipboard, selection, image, and voice interactions
  - execute browser-integrated actions

### C. AI Backend (`backend/gemini-agent`)

- Runtime: Node.js
- Role:
  - analyze text, image, and voice-enhanced requests
  - infer the most useful action in Smart mode
  - generate ranked actions in Guided mode
  - return result text plus structured browser action plans

### D. Browser Action Layer

- `desktop/browser-action-agent`
- `desktop/browser-extension-chromium`
- `desktop/browser-native-host`

Role:

- inspect browser context
- execute safe structured actions
- act in the current real browser tab when available
- fall back to managed-browser automation when needed

### E. Shared Contracts (`shared/ipc-protocol`)

- trigger events
- agent requests
- agent responses
- browser action plans
- haptic metadata

## 3. Core Experience

### 3.1 Smart Mode

1. User selects current context
2. User triggers from orb or Logitech hardware
3. Companion captures the active selection
4. AI backend decides the most useful action
5. Cursivis returns the result
6. User can optionally press `Take Action`

### 3.2 Guided Mode

1. User selects current context
2. User presses `Trigger`
3. Orb shows relevant predefined options
4. Orb expands with dynamic context-aware options
5. Custom remains available at all times
6. User chooses one path and gets the result

### 3.3 Talk Mode

1. User selects context
2. User holds `Talk`
3. Voice is captured and transcribed
4. Spoken instruction is combined with the current selection
5. Cursivis runs the refined request

### 3.4 Take Action

1. User reviews the generated result
2. User presses `Take Action`
3. Backend turns the result into a structured browser plan
4. Current-tab execution is attempted first
5. Managed fallback is used when necessary

## 4. Selection Model

Cursivis supports:

- text
- image
- text + image
- text + voice
- image + voice
- text + image + voice when useful

This is important because the interaction model is not “open chat, then explain.” It is “capture what is already on screen and act on it.”

## 5. Logitech Interaction Model

The long-term hardware mapping is:

- `Trigger` = run Smart or Guided action
- `Talk` = hold-to-talk refinement
- `Snip-it` = image or region selection
- `Action` = execute result in the active workflow
- dial / ring = navigate options, adjust focus, confirm intent

This is what makes Cursivis feel like a true Logitech-native product instead of a generic assistant.

## 6. Reliability Priorities

- always use the latest active selection
- keep Smart Mode decision-making flexible and context-aware
- keep Guided Mode relevant to the current selection
- preserve browser action safety and reversibility where possible
- keep the orb and result UI minimal and fast

## 7. Local Runtime Topology

- Logitech plugin or bridge sends trigger events
- companion captures context and orchestrates UI
- backend returns analysis and plans
- browser layer executes real web actions

## 8. Definition Of Done

Cursivis is considered competition-ready when:

- Smart Mode reliably chooses useful actions from live context
- Guided Mode offers relevant options around the orb
- voice commands combine correctly with the current selection
- image selection reflects the correct on-screen region
- `Take Action` works reliably on live browser workflows
- Logitech plugin integration feels native and polished
