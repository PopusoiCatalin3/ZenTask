using System.Windows.Controls;
using ZenTask.ViewModels;

namespace ZenTask.Views
{
    /// <summary>
    /// Interaction logic for TaskView.xaml
    /// </summary>
    public partial class TaskView : UserControl
    {
        private TaskViewModel _viewModel;

        public TaskView()
        {
            InitializeComponent();
            DataContextChanged += TaskView_DataContextChanged;
        }

        private void TaskView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            _viewModel = DataContext as TaskViewModel;

            //if (_viewModel != null)
            //{
            //    // Încarcă datele inițiale
            //    _viewModel.LoadTasksCommand.Execute(null);
            //}
        }
    }
}