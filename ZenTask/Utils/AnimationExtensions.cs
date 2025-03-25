using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace ZenTask.Utils
{
    /// <summary>
    /// Extension methods for animations on UI elements
    /// </summary>
    public static class AnimationExtensions
    {
        /// <summary>
        /// Animates the opacity of an element
        /// </summary>
        public static void AnimateOpacity(this UIElement element, double from, double to, double durationInSeconds, Action completedAction = null)
        {
            AnimationUtility.AnimateOpacity(element, from, to, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a scale transform on an element
        /// </summary>
        public static void AnimateScale(this UIElement element, double fromScale, double toScale, double durationInSeconds, Action completedAction = null)
        {
            AnimationUtility.AnimateScale(element, fromScale, toScale, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a translation transform on an element
        /// </summary>
        public static void AnimateTranslate(this UIElement element, double fromX, double toX, double fromY, double toY, double durationInSeconds, Action completedAction = null)
        {
            AnimationUtility.AnimateTranslate(element, fromX, toX, fromY, toY, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a smooth fade in for an element
        /// </summary>
        public static void FadeIn(this UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            AnimationUtility.FadeIn(element, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a smooth fade out for an element
        /// </summary>
        public static void FadeOut(this UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            AnimationUtility.FadeOut(element, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a smooth slide in from the right
        /// </summary>
        public static void SlideInFromRight(this UIElement element, double distance = 50, double durationInSeconds = 0.4, Action completedAction = null)
        {
            AnimationUtility.SlideInFromRight(element, distance, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a smooth slide out to the right
        /// </summary>
        public static void SlideOutToRight(this UIElement element, double distance = 50, double durationInSeconds = 0.4, Action completedAction = null)
        {
            AnimationUtility.SlideOutToRight(element, distance, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a scale in (grow) effect
        /// </summary>
        public static void ScaleIn(this UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            AnimationUtility.ScaleIn(element, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a scale out (shrink) effect
        /// </summary>
        public static void ScaleOut(this UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            AnimationUtility.ScaleOut(element, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a bounce effect
        /// </summary>
        public static void Bounce(this UIElement element, double strength = 1.1, double durationInSeconds = 0.3, Action completedAction = null)
        {
            AnimationUtility.Bounce(element, strength, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a pulse effect
        /// </summary>
        public static void Pulse(this UIElement element, double strength = 1.05, double durationInSeconds = 0.5, int pulseCount = 1, Action completedAction = null)
        {
            AnimationUtility.Pulse(element, strength, durationInSeconds, pulseCount, completedAction);
        }

        /// <summary>
        /// Animates a property of an element
        /// </summary>
        public static void AnimateProperty<T>(this DependencyObject element, DependencyProperty property, T from, T to, double durationInSeconds, Action completedAction = null) where T : struct
        {
            // Ensure we're dealing with an Animatable object
            if (!(element is Animatable animatableElement))
            {
                throw new ArgumentException("Element must be animatable", nameof(element));
            }

            // Create animation based on property type
            if (typeof(T) == typeof(double))
            {
                var animation = new DoubleAnimation
                {
                    From = (double)(object)from,
                    To = (double)(object)to,
                    Duration = TimeSpan.FromSeconds(durationInSeconds)
                };

                // Add easing
                animation.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut };

                // Add completed event handler
                if (completedAction != null)
                {
                    animation.Completed += (s, e) => completedAction();
                }

                // Start animation
                animatableElement.BeginAnimation(property, animation);
            }
            else if (typeof(T) == typeof(int))
            {
                var animation = new Int32Animation
                {
                    From = (int)(object)from,
                    To = (int)(object)to,
                    Duration = TimeSpan.FromSeconds(durationInSeconds)
                };

                // Add completed event handler
                if (completedAction != null)
                {
                    animation.Completed += (s, e) => completedAction();
                }

                // Start animation
                animatableElement.BeginAnimation(property, animation);
            }
            else if (typeof(T) == typeof(Thickness))
            {
                var animation = new ThicknessAnimation
                {
                    From = (Thickness)(object)from,
                    To = (Thickness)(object)to,
                    Duration = TimeSpan.FromSeconds(durationInSeconds)
                };

                // Add easing
                animation.EasingFunction = new PowerEase { EasingMode = EasingMode.EaseOut };

                // Add completed event handler
                if (completedAction != null)
                {
                    animation.Completed += (s, e) => completedAction();
                }

                // Start animation
                animatableElement.BeginAnimation(property, animation);
            }
        }

        /// <summary>
        /// Applies an animated transition when switching visibility
        /// </summary>
        public static void AnimateVisibility(this UIElement element, Visibility targetVisibility, double durationInSeconds = 0.3, Action completedAction = null)
        {
            // If current visibility matches target, nothing to do
            if (element.Visibility == targetVisibility)
            {
                completedAction?.Invoke();
                return;
            }

            if (targetVisibility == Visibility.Visible)
            {
                // Fade in
                element.FadeIn(durationInSeconds, completedAction);
            }
            else
            {
                // Fade out
                element.FadeOut(durationInSeconds, completedAction);
            }
        }

        /// <summary>
        /// Animation that shakes an element to indicate error or warning
        /// </summary>
        public static void Shake(this UIElement element, double durationInSeconds = 0.5, Action completedAction = null)
        {
            Storyboard storyboard = new Storyboard();

            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.1)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5)), new PowerEase { EasingMode = EasingMode.EaseOut }));

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            storyboard.Children.Add(animation);

            // Ensure the element has a TranslateTransform
            if (element.RenderTransform == null || !(element.RenderTransform is System.Windows.Media.TranslateTransform))
            {
                element.RenderTransform = new System.Windows.Media.TranslateTransform();
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Add completed event handler
            if (completedAction != null)
            {
                storyboard.Completed += (s, e) => completedAction();
            }

            // Start the storyboard
            storyboard.Begin();
        }
    }
}