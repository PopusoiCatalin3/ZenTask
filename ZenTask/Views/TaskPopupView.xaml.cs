using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ZenTask.Views
{
    /// <summary>
    /// Interaction logic for TaskPopupView.xaml
    /// </summary>
    public partial class TaskPopupView : UserControl
    {
        private Storyboard _exitAnimation;

        // Add a dependency property for MaxHeight
        public static readonly DependencyProperty MaxHeightProperty =
            DependencyProperty.Register("MaxHeight", typeof(double), typeof(TaskPopupView),
                new PropertyMetadata(700.0)); // Default height

        public double MaxHeight
        {
            get { return (double)GetValue(MaxHeightProperty); }
            set { SetValue(MaxHeightProperty, value); }
        }

        public TaskPopupView()
        {
            InitializeComponent();
            Loaded += TaskPopupView_Loaded;
        }

        private void TaskPopupView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("TaskPopupView loaded");
                Debug.WriteLine($"Current MaxHeight: {MaxHeight}");

                // Get the exit animation for later use
                _exitAnimation = FindResource("PopupExitAnimation") as Storyboard;

                // Make sure animation targets this instance
                if (_exitAnimation != null)
                {
                    foreach (var timeline in _exitAnimation.Children)
                    {
                        Storyboard.SetTarget(timeline, this);
                    }
                }

                // Focus the title textbox when loaded
                if (TitleTextBox != null)
                {
                    TitleTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in TaskPopupView_Loaded: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// Play exit animation and notify when complete
        /// </summary>
        /// <param name="onCompleted">Action to run when animation completes</param>
        public void PlayExitAnimation(Action onCompleted)
        {
            try
            {
                if (_exitAnimation != null)
                {
                    Debug.WriteLine("Playing exit animation");

                    // Create event handler with reference so we can remove it later
                    EventHandler completedHandler = null;
                    completedHandler = (s, e) => {
                        // Unhook event to prevent memory leaks
                        _exitAnimation.Completed -= completedHandler;
                        Debug.WriteLine("Exit animation completed");
                        // Execute the callback
                        onCompleted?.Invoke();
                    };

                    // Hook up completion handler
                    _exitAnimation.Completed += completedHandler;

                    // Ensure this element is visible
                    this.Visibility = Visibility.Visible;

                    // Begin the animation
                    _exitAnimation.Begin(this, true);
                }
                else
                {
                    Debug.WriteLine("No exit animation found");
                    // If animation isn't available, just call the completion handler
                    onCompleted?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PlayExitAnimation: {ex.Message}");
                Debug.WriteLine(ex.StackTrace);
                // Ensure callback is called even if animation fails
                onCompleted?.Invoke();
            }
        }
    }
}