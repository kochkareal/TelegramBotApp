// MainWindow.xaml.cs
using System.Windows;

namespace TelegramBotApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new ViewModels.MainWindowViewModel(); // Установка DataContext для привязки к ViewModel
        }
    }
}
