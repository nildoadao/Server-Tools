using Microsoft.Win32;
using Server_Tools.Control;
using Server_Tools.Model;
using Server_Tools.Util;
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
    /// Interação lógica para ScpImportPage.xam
    /// </summary>
    public partial class ScpImportPage : Page
    {
        OpenFileDialog fileDialog;
        OpenFileDialog csvDialog;

        public ScpImportPage()
        {
            InitializeComponent();
            fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Arquivos SCP (*.xml)|*.xml";
            fileDialog.FileOk += FileDialog_FileOk;
            csvDialog = new OpenFileDialog();
            csvDialog.Filter = "Arquivos CSV|*csv";
            csvDialog.FileOk += CsvDialog_FileOk;
        }

        private void FileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileTextBox.Text = fileDialog.FileName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
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

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            csvDialog.ShowDialog();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportScp();
        }

        private async void ImportScp()
        {
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            IdracRedfishController idrac = new IdracRedfishController(server);
            OutputTextBox.AppendText("Importando configurações para " + server.Host + "...\n");

            IdracScpTarget target = IdracScpTarget.ALL;
            IdracShutdownType shutdown;

            if (GracefulRadioButton.IsChecked.Value)
                shutdown = IdracShutdownType.Graceful;
            else if (ForcedRadioButton.IsChecked.Value)
                shutdown = IdracShutdownType.Forced;
            else
                shutdown = IdracShutdownType.NoReboot;
            try
            {
                IdracJob job = await idrac.ImportScpFile(FileTextBox.Text, target, shutdown, IdracHostPowerStatus.On);
                OutputTextBox.AppendText("Job Status: " + job.JobState + " " + job.Message + "\n");
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText("Falha ao importar arquivo: " + ex.Message);
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            fileDialog.ShowDialog();
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                IEnumerable<Server> servers = FileHelper.ReadCsvFile(csvDialog.FileName);
                foreach (Server server in servers)
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText("Falha ao carregar CSV: " + ex.Message + "\n");
            }
        }

        private bool ValidateForm()
        {
            if (ServerTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o endereço do host", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (UserTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o usuario", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (PasswordBox.Password.Trim().Equals(""))
            {
                MessageBox.Show("Informe a senha", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (FileTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Selecione um arquivo", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
