using System.Windows;


namespace Server_Tools.View
{
    /// <summary>
    /// Lógica interna para HomePageWindow.xaml
    /// </summary>
    public partial class HomeWindow : Window
    {
        public HomeWindow()
        {
            InitializeComponent();
            MainFrame.NavigationService.Navigate(new HomePage());
        }
    }
}
