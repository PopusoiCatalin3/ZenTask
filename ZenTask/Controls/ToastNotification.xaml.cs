using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ZenTask.Controls
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public partial class ToastNotification : UserControl
    {
        private DispatcherTimer _timer;
        private Storyboard _enterAnimation;
        private Storyboard _exitAnimation;

        // Dependency properties for binding
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ToastNotification), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(string), typeof(ToastNotification), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty ToastTypeProperty =
            DependencyProperty.Register("ToastType", typeof(ToastType), typeof(ToastNotification), new PropertyMetadata(ToastType.Info));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register("Duration", typeof(int), typeof(ToastNotification), new PropertyMetadata(3000));

        // Public properties
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public ToastType ToastType
        {
            get { return (ToastType)GetValue(ToastTypeProperty); }
            set { SetValue(ToastTypeProperty, value); }
        }

        public int Duration
        {
            get { return (int)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        // Event for when the toast is closed
        public event EventHandler ToastClosed;

        public ToastNotification()
        {
            InitializeComponent();
            DataContext = this;

            // Get animations
            _enterAnimation = FindResource("ToastEnterAnimation") as Storyboard;
            _exitAnimation = FindResource("ToastExitAnimation") as Storyboard;

            if (_exitAnimation != null)
            {
                _exitAnimation.Completed += (s, e) =>
                {
                    // Raise the closed event
                    ToastClosed?.Invoke(this, EventArgs.Empty);
                };
            }

            // Setup timer
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
        }

        // Start the toast notification
        public void Show()
        {
            // Start enter animation
            _enterAnimation?.Begin(ToastBorder);

            // Start the auto-close timer if duration > 0
            if (Duration > 0)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(Duration);
                _timer.Start();
            }
        }

        // Manually close the toast
        public void Close()
        {
            _timer.Stop();
            _exitAnimation?.Begin(ToastBorder);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}