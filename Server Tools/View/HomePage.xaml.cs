using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para HomePage.xam
    /// </summary>
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();            
        }

        private void UpdateFirmwareButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new FirmwarePage());
        }

        private void StorageButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new StoragePage());
        }

        private void CustomScriptButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CustomScriptPage());
        }

        private void ExportScpButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ScpExportPage());
        }

        private void ImportScpButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ScpImportPage());
        }

        private void TsrButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TsrCollectPage());
        }
    }
}
