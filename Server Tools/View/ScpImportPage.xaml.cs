using Microsoft.Win32;
using Server_Tools.Control;
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
    /// Interação lógica para ScpImportPage.xam
    /// </summary>
    public partial class ScpImportPage : Page
    {
        OpenFileDialog fileDialog;

        public ScpImportPage()
        {
            InitializeComponent();
            fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Arquivos SCP (*.xml)|*.xml";
            fileDialog.FileOk += FileDialog_FileOk;
        }

        private void FileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileTextBox.Text = fileDialog.FileName;
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

            IdracScpTarget target;
            IdracShutdownType shutdown;

            if (AllRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.ALL;
            }
            else if (SystemRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.System;
            }
            else if (BiosRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.BIOS;
            }
            else if (FcRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.FC;
            }
            else if (IdracRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.IDRAC;
            }
            else if (LifeCycleRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.LidecycleController;
            }
            else if (NicRadioButton.IsChecked.Value)
            {
                target = IdracScpTarget.NIC;
            }
            else
            {
                target = IdracScpTarget.RAID;
            }

            if (GracefulRadioButton.IsChecked.Value)
            {
                shutdown = IdracShutdownType.Graceful;
            }
            else if (ForcedRadioButton.IsChecked.Value)
            {
                shutdown = IdracShutdownType.Forced;
            }
            else
            {
                shutdown = IdracShutdownType.NoReboot;
            }
            try
            {
                IdracJob job = await idrac.ImportScpFile(FileTextBox.Text, target, shutdown, IdracHostPowerStatus.On);
                OutputTextBox.AppendText("Job Status: " + job.JobState + " " + job.Message);
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
