using System.Windows;
using System.Windows.Controls;
using ZenTask.Resources.Animations;

namespace ZenTask.Utils
{
    public static class AnimationExtension
    {
        // Fade animations
        public static void FadeIn(this UIElement element, Action completedAction = null)
        {
            AnimationService.FadeIn(element, completedAction);
        }

        public static void FadeOut(this UIElement element, Action completedAction = null)
        {
            AnimationService.FadeOut(element, completedAction);
        }

        // Scale animations
        public static void ScaleIn(this UIElement element, Action completedAction = null)
        {
            AnimationService.ScaleIn(element, completedAction);
        }

        public static void ScaleOut(this UIElement element, Action completedAction = null)
        {
            AnimationService.ScaleOut(element, completedAction);
        }

        // Button animations
        public static void Pulse(this Button button, Action completedAction = null)
        {
            AnimationService.ButtonClick(button, completedAction);
        }

        // Error feedback
        public static void Shake(this UIElement element, Action completedAction = null)
        {
            AnimationService.Shake(element, completedAction);
        }

        // Play any named animation
        public static void PlayAnimation(this UIElement element, string animationName, Action completedAction = null)
        {
            AnimationService.PlayAnimation(element, animationName, completedAction);
        }

        // Animated visibility changes
        public static void ShowAnimated(this UIElement element, Action completedAction = null)
        {
            AnimationService.FadeIn(element, completedAction);
        }

        public static void HideAnimated(this UIElement element, Action completedAction = null)
        {
            AnimationService.FadeOut(element, completedAction);
        }
    }
}
