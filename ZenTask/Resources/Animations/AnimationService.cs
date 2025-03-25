using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ZenTask.Resources.Animations
{
    public static class AnimationService
    {
        /// <summary>
        /// Ensures that the element has the required transforms for animations
        /// </summary>
        public static void PrepareElementForAnimation(UIElement element)
        {
            if (element == null) return;

            // Ensure the element has a TransformGroup with the necessary transforms
            if (!(element.RenderTransform is TransformGroup transformGroup))
            {
                transformGroup = new TransformGroup();

                // Add transforms in the correct order
                transformGroup.Children.Add(new ScaleTransform(1.0, 1.0));
                transformGroup.Children.Add(new RotateTransform(0));
                transformGroup.Children.Add(new TranslateTransform(0, 0));

                element.RenderTransform = transformGroup;
                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            else
            {
                // Ensure the transform group has all required transforms
                bool hasScaleTransform = false;
                bool hasRotateTransform = false;
                bool hasTranslateTransform = false;

                foreach (Transform transform in transformGroup.Children)
                {
                    if (transform is ScaleTransform) hasScaleTransform = true;
                    if (transform is RotateTransform) hasRotateTransform = true;
                    if (transform is TranslateTransform) hasTranslateTransform = true;
                }

                // Add any missing transforms
                if (!hasScaleTransform) transformGroup.Children.Add(new ScaleTransform(1.0, 1.0));
                if (!hasRotateTransform) transformGroup.Children.Add(new RotateTransform(0));
                if (!hasTranslateTransform) transformGroup.Children.Add(new TranslateTransform(0, 0));

                element.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        /// <summary>
        /// Plays a storyboard on an element with proper setup
        /// </summary>
        public static void PlayAnimation(UIElement element, string animationName, Action completedAction = null)
        {
            if (element == null) return;

            try
            {
                // Ensure element is prepared for animation
                PrepareElementForAnimation(element);

                // Find the storyboard resource
                if (Application.Current.Resources[animationName] is Storyboard storyboard)
                {
                    // Clone the storyboard to avoid reuse issues
                    Storyboard animationClone = storyboard.Clone();

                    // Set the target for all animations
                    foreach (var timeline in animationClone.Children)
                    {
                        if (Storyboard.GetTargetName(timeline) == null)
                        {
                            Storyboard.SetTarget(timeline, element);
                        }
                    }

                    // Add completion handler if provided
                    if (completedAction != null)
                    {
                        EventHandler completionHandler = null;
                        completionHandler = (s, e) => {
                            animationClone.Completed -= completionHandler;
                            completedAction();
                        };

                        animationClone.Completed += completionHandler;
                    }

                    // Begin the animation - fix for the type issue
                    // Use the correct overload that takes a FrameworkElement
                    if (element is FrameworkElement frameworkElement)
                    {
                        animationClone.Begin(frameworkElement);
                    }
                    else
                    {
                        // Fallback for other element types
                        animationClone.Begin();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Animation '{animationName}' not found");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing animation: {ex.Message}");

                // Call the completion action even if animation fails
                completedAction?.Invoke();
            }
        }

        #region Animation Helpers

        /// <summary>
        /// Fades in an element
        /// </summary>
        public static void FadeIn(UIElement element, Action completedAction = null)
        {
            if (element == null) return;

            element.Visibility = Visibility.Visible;
            PlayAnimation(element, "FadeInAnimation", completedAction);
        }

        /// <summary>
        /// Fades out an element
        /// </summary>
        public static void FadeOut(UIElement element, Action completedAction = null)
        {
            if (element == null) return;

            Action completeWithHide = () => {
                element.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            PlayAnimation(element, "FadeOutAnimation", completeWithHide);
        }

        /// <summary>
        /// Scales an element in
        /// </summary>
        public static void ScaleIn(UIElement element, Action completedAction = null)
        {
            if (element == null) return;

            element.Visibility = Visibility.Visible;
            PlayAnimation(element, "ScaleInAnimation", completedAction);
        }

        /// <summary>
        /// Scales an element out
        /// </summary>
        public static void ScaleOut(UIElement element, Action completedAction = null)
        {
            if (element == null) return;

            Action completeWithHide = () => {
                element.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            PlayAnimation(element, "ScaleOutAnimation", completeWithHide);
        }

        /// <summary>
        /// Plays the button click animation
        /// </summary>
        public static void ButtonClick(Button button, Action completedAction = null)
        {
            PlayAnimation(button, "ButtonClickAnimation", completedAction);
        }

        /// <summary>
        /// Transitions between visible elements
        /// </summary>
        public static void TransitionElementsWithFade(UIElement currentElement, UIElement newElement, Action completedAction = null)
        {
            if (currentElement == null || newElement == null) return;

            // First fade out current element
            FadeOut(currentElement, () => {
                // Then fade in new element
                FadeIn(newElement, completedAction);
            });
        }

        /// <summary>
        /// Shows a popup with animation
        /// </summary>
        public static void ShowPopup(UIElement popupContent, Action completedAction = null)
        {
            if (popupContent == null) return;

            popupContent.Visibility = Visibility.Visible;
            PlayAnimation(popupContent, "PopupOpenAnimation", completedAction);
        }

        /// <summary>
        /// Hides a popup with animation
        /// </summary>
        public static void HidePopup(UIElement popupContent, Action completedAction = null)
        {
            if (popupContent == null) return;

            Action completeWithHide = () => {
                popupContent.Visibility = Visibility.Collapsed;
                completedAction?.Invoke();
            };

            PlayAnimation(popupContent, "PopupExitAnimation", completeWithHide);
        }

        /// <summary>
        /// Makes an element shake (for errors)
        /// </summary>
        public static void Shake(UIElement element, Action completedAction = null)
        {
            if (element == null) return;

            // Create a custom shake animation
            TranslateTransform transform = null;

            // Find or create the translate transform
            if (element.RenderTransform is TransformGroup group)
            {
                foreach (Transform t in group.Children)
                {
                    if (t is TranslateTransform translate)
                    {
                        transform = translate;
                        break;
                    }
                }
            }

            if (transform == null)
            {
                PrepareElementForAnimation(element);

                // Get the transform group and find the translate transform
                var transformGroup = element.RenderTransform as TransformGroup;
                foreach (Transform t in transformGroup.Children)
                {
                    if (t is TranslateTransform translate)
                    {
                        transform = translate;
                        break;
                    }
                }
            }

            // Create the shake animation
            Storyboard storyboard = new Storyboard();

            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(-15, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.1)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(12, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(4, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4)), new PowerEase { EasingMode = EasingMode.EaseOut }));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5)), new PowerEase { EasingMode = EasingMode.EaseOut }));

            Storyboard.SetTarget(animation, transform);
            Storyboard.SetTargetProperty(animation, new PropertyPath(TranslateTransform.XProperty));
            storyboard.Children.Add(animation);

            // Add completed event handler
            if (completedAction != null)
            {
                storyboard.Completed += (s, e) => completedAction();
            }

            // Start the storyboard - fix for the type issue
            if (element is FrameworkElement frameworkElement)
            {
                storyboard.Begin(frameworkElement);
            }
            else
            {
                // Fallback for other element types
                storyboard.Begin();
            }
        }

        #endregion
    }
}