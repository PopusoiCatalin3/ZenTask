using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ZenTask.Utils;
using ZenTask.ViewModels;

namespace ZenTask.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        private LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();

            DataContextChanged += LoginView_DataContextChanged;
            Loaded += LoginView_Loaded;
        }

        private void LoginView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel = DataContext as LoginViewModel;

            if (_viewModel != null)
            {
                // Subscribe to property changes to animate errors
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void LoginView_Loaded(object sender, RoutedEventArgs e)
        {
            // Animate the login container when loaded
            var contentBorder = FindVisualChild<Border>(this);
            if (contentBorder != null)
            {
                contentBorder.ScaleIn(0.5);
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Animate error message when it appears
            //if (e.PropertyName == "HasError" && _viewModel.HasError)
            //{
            //    var errorBorder = FindVisualChild<Border>(this, border =>
            //        border.Background != null &&
            //        border.Background.ToString().Contains("#3C1C1C"));

            //    if (errorBorder != null)
            //    {
            //        errorBorder.Shake();
            //    }
            //}

            //// Animate processing indicator
            //if (e.PropertyName == "IsProcessing")
            //{
            //    var processingBorder = FindVisualChild<Border>(this, border =>
            //        border.Visibility == Visibility.Visible &&
            //        border.DataContext == _viewModel &&
            //        VisualTreeHelper.GetChildrenCount(border) > 0 &&
            //        VisualTreeHelper.GetChild(border, 0) is StackPanel panel &&
            //        panel.Children.Count > 0 &&
            //        panel.Children[0] is TextBlock text &&
            //        text.Text == "Processing...");

            //    if (processingBorder != null)
            //    {
            //        if (_viewModel.IsProcessing)
            //        {
            //            processingBorder.FadeIn(0.3);
            //        }
            //        else
            //        {
            //            processingBorder.FadeOut(0.3);
            //        }
            //    }
            //}
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            // Animate the button click
            if (sender is Button button)
            {
                button.Pulse();
            }

            if (string.IsNullOrWhiteSpace(_viewModel.Username))
            {
                _viewModel.ErrorMessage = "Username is required";
                return;
            }

            if (string.IsNullOrEmpty(LoginPasswordBox.Password))
            {
                _viewModel.ErrorMessage = "Password is required";
                return;
            }

            _viewModel.LoginWithPassword(LoginPasswordBox.Password);
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            // Animate the button click
            if (sender is Button button)
            {
                button.Pulse();
            }

            if (string.IsNullOrWhiteSpace(_viewModel.Username) ||
                string.IsNullOrWhiteSpace(_viewModel.Email) ||
                string.IsNullOrWhiteSpace(_viewModel.FirstName) ||
                string.IsNullOrWhiteSpace(_viewModel.LastName))
            {
                _viewModel.ErrorMessage = "Please fill in all fields";
                return;
            }

            if (string.IsNullOrEmpty(RegisterPasswordBox.Password))
            {
                _viewModel.ErrorMessage = "Password is required";
                return;
            }

            if (RegisterPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                _viewModel.ErrorMessage = "Passwords don't match";
                return;
            }

            if (RegisterPasswordBox.Password.Length < 6)
            {
                _viewModel.ErrorMessage = "Password must be at least 6 characters long";
                return;
            }

            _viewModel.RegisterWithPassword(RegisterPasswordBox.Password);

            if (!_viewModel.HasError)
            {
                LoginTabs.SelectedIndex = 0;

                RegisterPasswordBox.Clear();
                ConfirmPasswordBox.Clear();
            }
        }

        // Helper method to find a visual child of specified type
        private static T FindVisualChild<T>(DependencyObject parent, Predicate<T> condition = null) where T : DependencyObject
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Check if this child is the desired type and meets the condition
                if (child is T typedChild && (condition == null || condition(typedChild)))
                {
                    return typedChild;
                }

                // Recursively search in this child's children
                var result = FindVisualChild<T>(child, condition);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}