using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ZenTask.Utils
{
    /// <summary>
    /// Utility class for easily applying animations to UI elements
    /// </summary>
    public static class AnimationUtility
    {
        /// <summary>
        /// Animates the opacity of an element
        /// </summary>
        public static void AnimateOpacity(UIElement element, double from, double to, double durationInSeconds, Action completedAction = null)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(durationInSeconds)
            };

            // Add easing
            animation.EasingFunction = new PowerEase { EasingMode = to > from ? EasingMode.EaseOut : EasingMode.EaseIn };

            // Add completed event handler
            if (completedAction != null)
            {
                animation.Completed += (s, e) => completedAction();
            }

            // Start the animation
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        /// <summary>
        /// Animates a scale transform on an element
        /// </summary>
        public static void AnimateScale(UIElement element, double fromScale, double toScale, double durationInSeconds, Action completedAction = null)
        {
            // Ensure the element has a ScaleTransform
            ScaleTransform scaleTransform = element.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1, 1);
                element.RenderTransform = scaleTransform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Create X scale animation
            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = TimeSpan.FromSeconds(durationInSeconds)
            };

            // Create Y scale animation
            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                From = fromScale,
                To = toScale,
                Duration = TimeSpan.FromSeconds(durationInSeconds)
            };

            // Add easing
            var easingFunction = toScale > fromScale
                ? (IEasingFunction)new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                : new PowerEase { EasingMode = EasingMode.EaseIn, Power = 2 };

            scaleXAnimation.EasingFunction = easingFunction;
            scaleYAnimation.EasingFunction = easingFunction;

            // Add completed event handler
            if (completedAction != null)
            {
                scaleYAnimation.Completed += (s, e) => completedAction();
            }

            // Start the animations
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        /// <summary>
        /// Animates a translation transform on an element
        /// </summary>
        public static void AnimateTranslate(UIElement element, double fromX, double toX, double fromY, double toY, double durationInSeconds, Action completedAction = null)
        {
            // Ensure the element has a TranslateTransform
            TranslateTransform translateTransform = element.RenderTransform as TranslateTransform;
            TransformGroup transformGroup = element.RenderTransform as TransformGroup;

            if (translateTransform == null)
            {
                if (transformGroup != null)
                {
                    // Look for an existing TranslateTransform in the group
                    foreach (Transform transform in transformGroup.Children)
                    {
                        if (transform is TranslateTransform)
                        {
                            translateTransform = transform as TranslateTransform;
                            break;
                        }
                    }

                    // If not found, add a new one
                    if (translateTransform == null)
                    {
                        translateTransform = new TranslateTransform(0, 0);
                        transformGroup.Children.Add(translateTransform);
                    }
                }
                else
                {
                    // Create a new TranslateTransform
                    translateTransform = new TranslateTransform(0, 0);
                    element.RenderTransform = translateTransform;
                    element.RenderTransformOrigin = new Point(0.5, 0.5);
                }
            }

            // Create X translation animation
            DoubleAnimation translateXAnimation = new DoubleAnimation
            {
                From = fromX,
                To = toX,
                Duration = TimeSpan.FromSeconds(durationInSeconds)
            };

            // Create Y translation animation
            DoubleAnimation translateYAnimation = new DoubleAnimation
            {
                From = fromY,
                To = toY,
                Duration = TimeSpan.FromSeconds(durationInSeconds)
            };

            // Add easing
            var easingFunction = new PowerEase { EasingMode = EasingMode.EaseOut, Power = 2 };
            translateXAnimation.EasingFunction = easingFunction;
            translateYAnimation.EasingFunction = easingFunction;

            // Add completed event handler
            if (completedAction != null)
            {
                translateYAnimation.Completed += (s, e) => completedAction();
            }

            // Start the animations
            translateTransform.BeginAnimation(TranslateTransform.XProperty, translateXAnimation);
            translateTransform.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);
        }

        /// <summary>
        /// Animates a smooth fade in for an element
        /// </summary>
        public static void FadeIn(UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            element.Opacity = 0;
            element.Visibility = Visibility.Visible;
            AnimateOpacity(element, 0, 1, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a smooth fade out for an element
        /// </summary>
        public static void FadeOut(UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            Action hideElement = () =>
            {
                element.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            AnimateOpacity(element, element.Opacity, 0, durationInSeconds, hideElement);
        }

        /// <summary>
        /// Animates a smooth slide in from the right
        /// </summary>
        public static void SlideInFromRight(UIElement element, double distance = 50, double durationInSeconds = 0.4, Action completedAction = null)
        {
            element.Opacity = 0;
            element.Visibility = Visibility.Visible;

            // Wrap in a TransformGroup if needed
            if (!(element.RenderTransform is TransformGroup))
            {
                TransformGroup group = new TransformGroup();
                if (element.RenderTransform != null && !(element.RenderTransform is Transform))
                {
                    group.Children.Add(element.RenderTransform);
                }
                group.Children.Add(new TranslateTransform());
                element.RenderTransform = group;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Setup combined animation
            AnimateTranslate(element, distance, 0, 0, 0, durationInSeconds);
            AnimateOpacity(element, 0, 1, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a smooth slide out to the right
        /// </summary>
        public static void SlideOutToRight(UIElement element, double distance = 50, double durationInSeconds = 0.4, Action completedAction = null)
        {
            // Wrap in a TransformGroup if needed
            if (!(element.RenderTransform is TransformGroup))
            {
                TransformGroup group = new TransformGroup();
                if (element.RenderTransform != null && !(element.RenderTransform is Transform))
                {
                    group.Children.Add(element.RenderTransform);
                }
                group.Children.Add(new TranslateTransform());
                element.RenderTransform = group;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            Action hideElement = () =>
            {
                element.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            // Setup combined animation
            AnimateTranslate(element, 0, distance, 0, 0, durationInSeconds);
            AnimateOpacity(element, element.Opacity, 0, durationInSeconds, hideElement);
        }

        /// <summary>
        /// Animates a scale in (grow) effect
        /// </summary>
        public static void ScaleIn(UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            element.Opacity = 0;
            element.Visibility = Visibility.Visible;
            AnimateScale(element, 0.8, 1, durationInSeconds);
            AnimateOpacity(element, 0, 1, durationInSeconds, completedAction);
        }

        /// <summary>
        /// Animates a scale out (shrink) effect
        /// </summary>
        public static void ScaleOut(UIElement element, double durationInSeconds = 0.3, Action completedAction = null)
        {
            Action hideElement = () =>
            {
                element.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            AnimateScale(element, 1, 0.8, durationInSeconds);
            AnimateOpacity(element, element.Opacity, 0, durationInSeconds, hideElement);
        }

        /// <summary>
        /// Animates a bounce effect
        /// </summary>
        public static void Bounce(UIElement element, double strength = 1.1, double durationInSeconds = 0.3, Action completedAction = null)
        {
            // Ensure the element has a ScaleTransform
            ScaleTransform scaleTransform = element.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1, 1);
                element.RenderTransform = scaleTransform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Create bounce animations
            DoubleAnimation scaleXAnimation = new DoubleAnimation
            {
                From = 1,
                To = strength,
                AutoReverse = true,
                Duration = TimeSpan.FromSeconds(durationInSeconds / 2)
            };

            DoubleAnimation scaleYAnimation = new DoubleAnimation
            {
                From = 1,
                To = strength,
                AutoReverse = true,
                Duration = TimeSpan.FromSeconds(durationInSeconds / 2)
            };

            // Add elastic easing
            var easingFunction = new ElasticEase
            {
                EasingMode = EasingMode.EaseOut,
                Oscillations = 2,
                Springiness = 3
            };

            scaleXAnimation.EasingFunction = easingFunction;
            scaleYAnimation.EasingFunction = easingFunction;

            // Add completed event handler
            if (completedAction != null)
            {
                scaleYAnimation.Completed += (s, e) => completedAction();
            }

            // Start the animations
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        /// <summary>
        /// Animates a pulse effect
        /// </summary>
        public static void Pulse(UIElement element, double strength = 1.05, double durationInSeconds = 0.5, int pulseCount = 1, Action completedAction = null)
        {
            // Create storyboard for multiple pulses
            Storyboard storyboard = new Storyboard();

            // Ensure the element has a ScaleTransform
            ScaleTransform scaleTransform = element.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1, 1);
                element.RenderTransform = scaleTransform;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // Create pulse animations
            DoubleAnimationUsingKeyFrames scaleXAnimation = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames scaleYAnimation = new DoubleAnimationUsingKeyFrames();

            // Calculate time per pulse
            double timePerPulse = durationInSeconds / pulseCount;

            // Add keyframes for each pulse
            for (int i = 0; i < pulseCount; i++)
            {
                double startTime = i * timePerPulse;
                double peakTime = startTime + (timePerPulse / 2);
                double endTime = startTime + timePerPulse;

                // X Scale keyframes
                scaleXAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime))));
                scaleXAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(strength, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(peakTime)), new PowerEase { EasingMode = EasingMode.EaseOut }));
                scaleXAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(endTime)), new PowerEase { EasingMode = EasingMode.EaseIn }));

                // Y Scale keyframes
                scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(startTime))));
                scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(strength, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(peakTime)), new PowerEase { EasingMode = EasingMode.EaseOut }));
                scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(endTime)), new PowerEase { EasingMode = EasingMode.EaseIn }));
            }

            // Configure storyboard
            Storyboard.SetTarget(scaleXAnimation, element);
            Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            storyboard.Children.Add(scaleXAnimation);

            Storyboard.SetTarget(scaleYAnimation, element);
            Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            storyboard.Children.Add(scaleYAnimation);

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