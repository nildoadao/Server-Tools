using Microsoft.Win32;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Xml;
using System.Xml.Linq;

namespace Server_Tools
{
    /// <summary>
    /// Interação lógica para MainWindow.xam
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<IdracFirmware> firmwareList;
        SshClient client;

        public MainWindow()
        {
            InitializeComponent();
            firmwareList = new ObservableCollection<IdracFirmware>();
        }
       
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordTextBox.Password);
            client = new SshClient(server.Host, server.User, server.Password);
            IdracUpdateController idracController = new IdracUpdateController(client);
            //IEnumerable<string> reportResult = idracController.GetUpdateReport(server, "Catalog.xml", NfsServerTextBox.Text);
            IEnumerable<string> reportResult = idracController.ReadReportFile(@"C:\Users\nildo\Desktop\result.txt");
            var catalogItems = idracController.GetCatalogItems(@"C:\Users\nildo\Desktop\Catalog.xml");
            var serverItems = idracController.ReadReportFile(reportResult);

            IEnumerable<IdracFirmware> list = idracController.CompareServerToCatalog(catalogItems, serverItems);

            foreach(IdracFirmware item in list)
            {
                firmwareList.Add(item);
            }
            FirmwareDataGrid.ItemsSource = firmwareList;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ObservableCollection<IdracFirmware> updateList = (ObservableCollection<IdracFirmware>)FirmwareDataGrid.ItemsSource;
            foreach(IdracFirmware item in updateList)
            {
                if (item.Update)
                {
                    Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordTextBox.Password);
                    client = new SshClient(server.Host, server.User, server.Password);
                    IdracUpdateController idrac = new IdracUpdateController(client);
                    idrac.UpdateFirmware(item.FirmwarePath, NfsServerTextBox.Text);
                }
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {        
            if(client != null)
            {
                if (client.IsConnected)
                {
                    return;
                }
            }

            client = new SshClient(ServerTextBox.Text, UserTextBox.Text, PasswordTextBox.Password);
            try
            {
                client.Connect();
                StatusLabel.Content = "Online";
                StatusLabel.Foreground = Brushes.Green;
                MessageBox.Show("Conexão estabelecida !");
            }
            catch (SshAuthenticationException)
            {
                MessageBox.Show("Usuario ou senha invalidos");
            }
            catch (SshConnectionException)
            {
                MessageBox.Show("Falha na conexão SSH");
            }
            catch (Exception)
            {
                MessageBox.Show("Falha ao tentar conectar com: " + ServerTextBox.Text);
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if(client != null)
            {
                if (!client.IsConnected)
                {
                    return;
                }
                else
                {
                    client.Disconnect();
                    StatusLabel.Content = "Offline";
                    StatusLabel.Foreground = Brushes.Red;
                    MessageBox.Show("Client desconectado !");
                }
            }
        }
    }
}
