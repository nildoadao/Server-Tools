using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para FileUpdatePage.xam
    /// </summary>
    public partial class FileUpdatePage : Page
    {
        OpenFileDialog firmwareDialog;

        public FileUpdatePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            firmwareDialog = new OpenFileDialog()
            {
                Filter = "Idrac Firmware (*.exe)(*.d7)(*.pm)| *.exe;*.d7;*.pm"
            };
            firmwareDialog.FileOk += FirmwareDialog_FileOk;
        }

        private void FirmwareDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FirmwareTextBox.Text = firmwareDialog.FileName;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;
            
            string firmware = FirmwareTextBox.Text;
            string option = "";
            foreach(RadioButton item in InstallOptionGroup.Children)
            {
                if (item.IsChecked.Value)
                {
                    option = item.Content.ToString();
                    break;
                }
            }
            UpdateFirmware(firmware, option);
        }

        private async void UpdateFirmware(string path, string option)
        {
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);

            /*if (!await CheckSupport(server))
                return;*/
            try
            {
                UpdateController idrac = new UpdateController(server);
                OutputTextBox.AppendText(string.Format("Iniciando upload do arquivo {0} para {1} \n", FirmwareTextBox.Text, server.Host));
                IdracJob job = await idrac.UpdateFirmware(path, option);
                OutputTextBox.AppendText(string.Format("Criado Job {0} para update\n",job.Id));
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Erro ao atualizar firmware: {0}\n", ex.Message));
            }           
        }

        private void OpenFirmwareButton_Click(object sender, RoutedEventArgs e)
        {
            firmwareDialog.ShowDialog();
        }

        private bool CheckForm()
        {
            if (ServerTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o hostname do servidor", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            if (UserTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o usuário", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            if (PasswordBox.Password.Trim().Equals(""))
            {
                MessageBox.Show("Informe a senha", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            if (FirmwareTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Selecione um firmware", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private async Task<bool> CheckSupport(Server server)
        {
            var idrac = new UpdateController(server);

            if (!NetworkHelper.IsConnected(server.Host))
            {
                MessageBox.Show(string.Format("O servidor {0} não está acessivel, verifique a conexão e tente novamente", server.Host), "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            try
            {
                if (!await idrac.CheckRedfishSupport(UpdateController.FirmwareInventory))
                {
                    MessageBox.Show("A versão da Idrac desse servidor não possui suporte a função de update de firmware", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
            }
            catch
            {
                MessageBox.Show("A versão da Idrac desse servidor não possui suporte a função de update de firmware", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }
    }
}
