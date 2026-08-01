using Microsoft.Maui.Controls;

namespace MediaHub.Animations;

/// <summary>
/// Soft fade-in for elements that become visible through a trigger,
/// e.g. the progress section when a download starts.
/// </summary>
public sealed class FadeInAction : TriggerAction<VisualElement>
{
    protected override async void Invoke(VisualElement sender)
    {
        sender.Opacity = 0;
        await sender.FadeTo(1, 250, Easing.CubicOut);
    }
}
