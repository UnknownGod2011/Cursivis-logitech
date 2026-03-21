# Cursivis Logitech Control Map

This file captures the intended default Logitech UX before MX hardware arrives.
It gives us a stable design target for virtual validation now and hardware tuning later.

## Design goals

- Keep the primary interaction extremely fast: select -> trigger -> review -> act
- Make the dial feel like an AI intent selector, not a generic volume knob
- Keep mouse-side Actions Ring controls focused on high-frequency actions
- Use haptics to confirm mode changes and execution state

## MX Creative Console default layout

### Keypad

Suggested default assignments:

1. `Cursivis Trigger`
   - Runs Smart Mode on the current selection
2. `Hold to Talk`
   - Starts voice capture for contextual commands
3. `Image Selection`
   - Starts lasso image capture
4. `Take Action`
   - Executes the current browser/UI action
5. `More Options`
   - Re-runs the latest selection through the improved action menu
6. `Undo`
   - Reverts the last replace/browser action when possible
7. `Copy`
   - Copies the current Cursivis result
8. `Insert`
   - Inserts/replaces with the current result

### Dialpad

Suggested default assignments:

- `Rotate`
  - Moves the Cursivis action ring selection
- `Press`
  - Executes the currently highlighted action
- `Hold`
  - Optional advanced mode: voice-first execution

## Actions Ring default layout

The Actions Ring should prioritize the four fastest “anywhere” actions:

1. `Trigger`
2. `Take Action`
3. `Hold to Talk`
4. `Image Selection`

Secondary ring candidates:

1. `More Options`
2. `Undo`
3. `Copy`
4. `Insert`

## Haptic intent

Current event semantics already wired through the plugin:

- `action_change`
  - light confirmation when the dial changes the selected AI action
- `action_execute`
  - medium confirmation when a trigger is executed
- `processing_start`
  - light pulse when Cursivis begins work
- `processing_complete`
  - strong confirmation when a result is ready

## Validation path before hardware

Without MX devices, we validate:

- plugin builds and packages
- plugin loads in Logi Plugin Service
- synthetic trigger events reach the companion
- haptic events come back into the plugin log

What still requires real devices:

- final button ergonomics
- dial sensitivity and tactile feel
- Actions Ring discoverability
- real haptic quality

## First hardware-day checklist

When MX hardware arrives:

1. test keypad trigger latency
2. test dial tick sensitivity and action-ring clarity
3. test hold-to-talk comfort and cancellation behavior
4. test Actions Ring discoverability on the MX mouse
5. tune default layout based on real-hand usage, not assumptions
