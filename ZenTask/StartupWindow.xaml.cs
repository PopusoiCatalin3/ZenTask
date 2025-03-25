using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ZenTask
{
    /// <summary>
    /// Interaction logic for StartupWindow.xaml
    /// </summary>
    public partial class StartupWindow : Window
    {
        public StartupWindow()
        {
            InitializeComponent();

            // Set up simple animation for progress bar
            StartSmoothProgressAnimation();
        }

        /// <summary>
        /// Update the progress display with new status and percentage
        /// </summary>
        public void UpdateProgress(string status, int percentage)
        {
            // Ensure we're on the UI thread
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => UpdateProgress(status, percentage));
                return;
            }

            // Update status text
            StatusText.Text = status;

            // Update progress bar smoothly
            AnimateProgressBarTo(percentage);
        }

        /// <summary>
        /// Animate the progress bar to a target value
        /// </summary>
        private void AnimateProgressBarTo(double targetValue)
        {
            // Create smooth animation
            var animation = new DoubleAnimation
            {
                To = targetValue,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Apply the animation
            LoadingProgress.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
        }

        /// <summary>
        /// Start a smooth indeterminate animation for the progress bar
        /// </summary>
        private void StartSmoothProgressAnimation()
        {
            // Create pulsing animation
            var animation = new DoubleAnimation
            {
                From = 0,
                To = 35,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            // Apply the animation
            LoadingProgress.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
        }

        /// <summary>
        /// Play exit animation and close
        /// </summary>
        public void PlayExitAnimationAndClose()
        {
            var exitAnimation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            exitAnimation.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, exitAnimation);
        }
    }
}