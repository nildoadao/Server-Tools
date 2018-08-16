using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Server_Tools
{
    /// <summary>
    /// Interação lógica para FirmwareUpdatePage.xam
    /// </summary>
    public partial class FirmwareUpdatePage : Page
    {
        ObservableCollection<IdracUpdateItem> datagridItems;

        public FirmwareUpdatePage()
        {
            InitializeComponent();
            datagridItems = new ObservableCollection<IdracUpdateItem>();
        }

        private void AddServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ServerTextBox.Text.Trim().Equals(""))
            {
                ServersListBox.Items.Add(ServerTextBox.Text);
            }
        }

        private void RemoveServerButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void SearchUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }
            List<IdracUpdateItem> serversToUpdate = new List<IdracUpdateItem>();

            foreach (string item in ServersListBox.Items)
            {
                if (!NetworkHelper.IsConnected(item))
                {
                    continue;
                }
                Server server = new Server(item, UserTextBox.Text, PasswordBox.Password);
                SshClient client = new SshClient(server.Host, server.User, server.Password);
                try
                {
                    client.Connect();
                    IdracUpdateController idrac = new IdracUpdateController(client);
                    IEnumerable<IdracFirmware> firmwaresToUpdate = idrac.GetUpdates(RepositoryTextBox.Text, "Catalog.xml");

                    foreach(IdracFirmware firmware in firmwaresToUpdate)
                    {
                        serversToUpdate.Add(new IdracUpdateItem(server, firmware));
                    }

                    foreach(IdracUpdateItem idracItem in serversToUpdate)
                    {
                        datagridItems.Add(idracItem);
                    }
                    UpdatesDataGrid.ItemsSource = datagridItems;
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Falha ao checar por updates: " + ex.Message, "Erro");
                }
                finally
                {
                    if(client != null)
                    {
                        client.Dispose();
                    }
                }
            }
        }

        public bool ValidateForm()
        {
            if(ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao meno um servidor para atualização", "Aviso");
                return false;
            }
            else if (UserTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Insira o nome do usuario para atualização", "Aviso");
                return false;
            }
            else if (PasswordBox.Password.Trim().Equals(""))
            {
                MessageBox.Show("Insira a senha para atualização", "Aviso");
                return false;
            }
            else if (RepositoryTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o repositorio de armazenamento dos firmwares", "Aviso");
                return false;
            }

            return true;
        }
    }
}
