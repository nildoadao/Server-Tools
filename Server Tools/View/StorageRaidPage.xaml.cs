using Server_Tools.Control;
using Server_Tools.Model;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para StorageRaidPage.xam
    /// </summary>
    public partial class StorageRaidPage : Page
    {

        private ObservableCollection<IdracDiskItem> datagridItems;

        public StorageRaidPage()
        {
            InitializeComponent();
            datagridItems = new ObservableCollection<IdracDiskItem>();
        }

        private void CheckPhysicalDiskButton_Click(object sender, RoutedEventArgs e)
        {
            if(ServerTextBox.Text.Trim().Equals("") | UserTextBox.Text.Trim().Equals("") | PasswordBox.Password.Trim().Equals(""))
            {
                MessageBox.Show("Preencha os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            CheckPhysicalDisks();
        }

        private async void CheckPhysicalDisks()
        {
            string host = ServerTextBox.Text;
            string user = UserTextBox.Text;
            string password = PasswordBox.Password;

            OutputTextBox.AppendText("Consultando discos de " + host + " ..." + "\n");

            await Task.Run(() =>
            {
                if (!NetworkHelper.IsConnected(host))
                {
                    Dispatcher.Invoke(() =>
                    {
                        OutputTextBox.AppendText("Servidor: " + host + " inacessivel\n");
                    });
                }
                else
                {
                    try
                    {
                        Server server = new Server(host, user, password);
                        IdracStorageController idrac = new IdracStorageController(server);
                        IEnumerable<IdracPhysicalDisk> physicalDisks = idrac.GetPhysicalDisks();
                        foreach (IdracPhysicalDisk disk in physicalDisks)
                        {
                            datagridItems.Add(new IdracDiskItem(disk, false));
                        }                       
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText("Falha ao consultar discos dos servidor: " + host + " " + ex.Message + "\n");
                        });
                    }
                }
            });
            DisksDataGrid.ItemsSource = datagridItems;
            OutputTextBox.AppendText("Busca concluida, encontrados: " + datagridItems.Count + " discos\n");
        }
    }
}
