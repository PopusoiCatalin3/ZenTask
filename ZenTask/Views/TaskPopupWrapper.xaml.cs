using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ZenTask.ViewModels;

namespace ZenTask.Views
{
    /// <summary>
    /// Interaction logic for TaskPopupWrapper.xaml
    /// </summary>
    public partial class TaskPopupWrapper : UserControl
    {
        private TaskViewModel _viewModel;
        private TaskPopupView _currentPopupView;
        private const double MARGIN_BOTTOM = 50; 
        private const double MARGIN_TOP = 50;    

        public TaskPopupView PopupView => _currentPopupView;
        public TaskPopupWrapper()
        {
            InitializeComponent();

            DataContextChanged += TaskPopupWrapper_DataContextChanged;

            // Handle popup closing directly
            taskPopup.Closed += TaskPopup_Closed;
        }

        private void TaskPopupWrapper_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                // Unsubscribe from old view model
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            }

            _viewModel = DataContext as TaskViewModel;

            if (_viewModel != null)
            {
                // Subscribe to IsPopupOpen changes to handle showing/hiding
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsPopupOpen")
            {
                try
                {
                    if (_viewModel.IsPopupOpen)
                    {
                        OpenPopup();
                    }
                    else
                    {
                        ClosePopup();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in popup handling: {ex.Message}");
                    Debug.WriteLine(ex.StackTrace);

                    // Force reset state in case of error
                    CleanupPopup();
                }
            }
        }

        private void OpenPopup()
        {
            Debug.WriteLine("Opening popup");

            // Get available screen space using WPF's SystemParameters
            double availableHeight = GetAvailableScreenHeight();

            // Create a new popup view instance each time
            _currentPopupView = new TaskPopupView
            {
                DataContext = _viewModel,
                MaxHeight = availableHeight - MARGIN_BOTTOM - MARGIN_TOP // Allow for margins
            };

            // Set the content
            PopupContentContainer.Content = _currentPopupView;

            // Adjust popup settings for better positioning
            taskPopup.Placement = PlacementMode.Center;
            taskPopup.VerticalOffset = -(MARGIN_BOTTOM / 2); // Slight upward shift to avoid bottom cutoff

            // Show the popup
            taskPopup.IsOpen = true;

            // Focus the title textbox
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                var titleTextBox = _currentPopupView.FindName("TitleTextBox") as TextBox;
                titleTextBox?.Focus();
            }));
        }

        private double GetAvailableScreenHeight()
        {
            // Use WPF's SystemParameters to get screen information
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            Window mainWindow = Application.Current.MainWindow;

            // Default to a reasonable size if we can't determine exact measurements
            if (mainWindow == null)
                return 600;

            // If window is maximized, use adjusted screen height
            if (mainWindow.WindowState == WindowState.Maximized)
            {
                // Account for taskbar and window chrome
                return screenHeight - 100; // Rough estimate for taskbar and window chrome
            }

            // For normal window state, use either window height or 80% of screen height
            return Math.Min(mainWindow.ActualHeight, screenHeight * 0.8);
        }

        private void ClosePopup()
        {
            Debug.WriteLine("Closing popup");

            if (_currentPopupView != null)
            {
                // Play exit animation if available
                _currentPopupView.PlayExitAnimation(() => {
                    taskPopup.IsOpen = false;
                });
            }
            else
            {
                // Just close if no view available
                taskPopup.IsOpen = false;
            }
        }

        private void TaskPopup_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("Popup closed event triggered");
            CleanupPopup();
        }

        private void CleanupPopup()
        {
            Debug.WriteLine("Cleaning up popup");

            // Clear popup content
            PopupContentContainer.Content = null;
            _currentPopupView = null;

            // Make sure view model state is synchronized
            if (_viewModel != null && _viewModel.IsPopupOpen)
            {
                // Detach event temporarily to avoid cycle
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.IsPopupOpen = false;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }
    }
}