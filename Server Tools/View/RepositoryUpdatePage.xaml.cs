using Renci.SshNet;
using Server_Tools.Model;
using Server_Tools.Util;
using Server_Tools.Control;
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
using System.Xml.Linq;
using System.Threading;
using System.ComponentModel;
using Microsoft.Win32;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para FirmwareUpdatePage.xam
    /// </summary>
    public partial class RepositoryUpdatePage : Page
    {
        ObservableCollection<IdracUpdateItem> datagridItems;
        bool operationCancelled;
        OpenFileDialog dialog;

        public RepositoryUpdatePage()
        {
            datagridItems = new ObservableCollection<IdracUpdateItem>();
            InitializeComponent();
            operationCancelled = false;
            dialog = new OpenFileDialog();
            dialog.FileOk += Dialog_FileOk;
        }

        private void AddServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ServerTextBox.Text.Trim().Equals(""))
            {               
                if (UserTextBox.Text.Trim().Equals("") | PasswordBox.Password.Trim().Equals(""))
                {
                    MessageBox.Show("Insira usuario e senha da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);                    
                }
                else
                {
                    Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
                    ServersListBox.Items.Add(server);
                    ServerTextBox.Clear();
                    UserTextBox.Clear();
                    PasswordBox.Clear();
                }
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
            OutputTextBox.Clear();
            UpdatesDataGrid.ItemsSource = null;
            SearchUpdates();         
        }

        private async void SearchUpdates()
        {
            string catalogFile = CatalogTextBox.Text;
            RepositoryType repositoryType;

            if (FtpRadioButton.IsChecked.Value)
            {
                repositoryType = RepositoryType.FTP;
            }
            else if (TftpRadioButton.IsChecked.Value)
            {
                repositoryType = RepositoryType.TFTP;
            }
            else
            {
                repositoryType = RepositoryType.NFS;
            }

            Repository repository = new Repository(RepositoryTextBox.Text, repositoryType);

            if (AnonymousCheckBox.IsPressed)
            {
                repository.User = RepositoryUserTextBox.Text;
                repository.Password = RepositoryPasswordBox.Password;
            }

            IEnumerable<IdracFirmware> firmwaresToUpdate = new List<IdracFirmware>();
            OutputTextBox.AppendText("Iniciando busca por updates...\n");

            foreach(Server server in ServersListBox.Items)
            {
                await Task.Run(() =>
                {
                    if (!NetworkHelper.IsConnected(server.Host))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText("Servidor " + server.Host + " inacessivel\n");
                        });
                    }
                    else
                    {
                        try
                        {
                            IdracUpdateController idrac = new IdracUpdateController(server);
                            firmwaresToUpdate = idrac.GetUpdates(catalogFile, repository);
                        }
                        catch (Exception ex)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                OutputTextBox.AppendText("Falha ao buscar por updates para: " + server.Host + " " + ex.Message + "\n");
                            });
                        }

                    }
                });                           
                foreach (IdracFirmware firmwareItem in firmwaresToUpdate)
                {
                    datagridItems.Add(new IdracUpdateItem(server, firmwareItem));
                }
                if (operationCancelled)
                {
                    OutputTextBox.AppendText("Operação cancelada pelo usuario\n");
                    operationCancelled = false;
                    break;
                }
            }
            UpdatesDataGrid.ItemsSource = datagridItems;
            OutputTextBox.AppendText("Finalizada buscas por updates " + datagridItems.Count + " updates encontrados\n");
        }

        private bool ValidateForm()
        {
            if (RepositoryTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o repositorio de armazenamento dos firmwares", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            if (!AnonymousCheckBox.IsChecked.Value)
            {
                if (RepositoryTextBox.Text.Trim().Equals(""))
                {
                    MessageBox.Show("Informe o usuario para acesso ao repositório", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                if (RepositoryPasswordBox.Password.Trim().Equals(""))
                {
                    MessageBox.Show("Informe a senha para acesso ao repositório", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            return true;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if(UpdatesDataGrid.Items.Count > 0)
            {
                UpdateFirmware();
            }
        }

        private async void UpdateFirmware()
        {
            IEnumerable<IdracUpdateItem> firmwares = (IEnumerable<IdracUpdateItem>) UpdatesDataGrid.ItemsSource;
            string catalog = CatalogTextBox.Text;
            bool reboot = RebootCheckBox.IsChecked.Value;

            RepositoryType repositoryType;

            if (FtpRadioButton.IsChecked.Value)
            {
                repositoryType = RepositoryType.FTP;
            }
            else if (TftpRadioButton.IsChecked.Value)
            {
                repositoryType = RepositoryType.TFTP;
            }
            else
            {
                repositoryType = RepositoryType.NFS;
            }

            Repository repository = new Repository(RepositoryTextBox.Text, repositoryType);

            if (AnonymousCheckBox.IsPressed)
            {
                repository.User = RepositoryUserTextBox.Text;
                repository.Password = RepositoryPasswordBox.Password;
            }

            OutputTextBox.AppendText("Iniciando Update de firmwares");

            foreach (Server server in ServersListBox.Items)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        IdracUpdateController idrac = new IdracUpdateController(server);
                        foreach (IdracUpdateItem firmware in firmwares)
                        {
                            idrac.UpdateFirmware(catalog, repository, reboot);
                        }
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText("Falha ao atualizar " + server + " " + ex.Message);
                        });
                    }
                });

                if (operationCancelled)
                {
                    OutputTextBox.AppendText("Operação cancelada pelo usuário");
                    break;
                }
            }
        }

        private void AnonymousCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            RepositoryPasswordBox.IsEnabled = false;
            RepositoryUserTextBox.IsEnabled = false;
        }

        private void AnonymousCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            RepositoryUserTextBox.IsEnabled = true;
            RepositoryPasswordBox.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!operationCancelled)
            {
                operationCancelled = true;
            }
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            dialog.Filter = "Arquivos CSV|*csv";
            dialog.ShowDialog();
        }

        private void Dialog_FileOk(object sender, CancelEventArgs e)
        {
            try
            {
                IEnumerable<Server> servers = FileHelper.ReadCsvFile(dialog.FileName);
                foreach(Server server in servers)
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText("Falha ao carregar CSV " + ex.Message);
            }
        }
    }
}
