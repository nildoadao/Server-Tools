using Server_Tools.Idrac.Models;
using System.Windows;


namespace Server_Tools.View
{
    /// <summary>
    /// Lógica interna para StorageWindow.xaml
    /// </summary>
    public partial class StorageWindow : Window
    {
        public StorageWindow(Server server)
        {
            InitializeComponent();
            MainFrame.NavigationService.Navigate(new StorageOverviewPage(server));
        }
    }
}
