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
    /// Interação lógica para FirmwarePage.xam
    /// </summary>
    public partial class FirmwarePage : Page
    {
        public FirmwarePage()
        {
            InitializeComponent();
        }

        private void RepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RepositoryUpdatePage());
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new FileUpdatePage());
        }
    }
}
