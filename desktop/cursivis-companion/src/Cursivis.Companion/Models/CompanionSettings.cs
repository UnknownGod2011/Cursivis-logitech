namespace Cursivis.Companion.Models;

public readonly record struct CompanionSettings(
    InteractionMode Mode,
    bool ShowOrbDuringWorkflow,
    TakeActionPromptPreference TakeActionPromptPreference,
    CompanionThemeMode ThemeMode);
