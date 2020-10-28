using System.IO;
using System.Windows;

namespace Task2
{

    public partial class MainWindow : Window
    {
        public Model CurModel;

        public MainWindow()
        {
            InitializeComponent();
            CurModel = new Model(this.Dispatcher);
            CurModel.FolderPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName + "\\Task1\\Images";
            DataContext = CurModel;
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            CurModel.Start();
        }

        private void Stop(object sender, RoutedEventArgs e)
        {  
            CurModel.Stop();
        }

        private void Open(object sender, RoutedEventArgs e)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (dialog.ShowDialog(this).GetValueOrDefault())
            {
                CurModel.FolderPath = dialog.SelectedPath;
            }
        }
    }
}
