using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void DiskButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new StorageRaidPage());
        }

        private void CustomScriptButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CustomScriptPage());
        }

        private void TemplateFileButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TemplatesPage());
        }
    }
}
