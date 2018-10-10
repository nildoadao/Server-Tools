using Server_Tools.Idrac;
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
    /// Interação lógica para StoragePage.xaml
    /// </summary>
    public partial class StoragePage : Page
    {
        public StoragePage()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var storageWindow = new StorageWindow();
            GetInfo();
            storageWindow.Show();
        }

        private async void GetInfo()
        {
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            var idrac = new StorageController(server);
            var controllers = await idrac.GetControllersLocation();
            var disks = new List<IdracPhysicalDisk>();
            foreach(var item in controllers)
            {
                var storage = await idrac.GetRaidController(item);
                disks = await idrac.GetPhysicalDisks(storage);
            }
        }
    }
}
