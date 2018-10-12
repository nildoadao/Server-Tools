using Server_Tools.Model;
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
    /// Interação lógica para StorageOverviewPage.xam
    /// </summary>
    public partial class StorageOverviewPage : Page
    {
        private Server server;

        public StorageOverviewPage(Server server)
        {
            InitializeComponent();
            this.server = server;
            PageHeader.Text = string.Format("Storage {0}", server.Host);
        }

        private void OverviewButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewPageContent.SelectedIndex = 0;
        }

        private void ControllerButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewPageContent.SelectedIndex = 1;
        }
    }
}
