using Microsoft.Win32;
using Server_Tools.Control;
using Server_Tools.Model;
using System;
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
            firmwareDialog = new OpenFileDialog();
            firmwareDialog.Filter = "Idrac Firmware (*.exe)(*.d7)(*.pm)| *.exe;*.d7;*.pm";
            firmwareDialog.FileOk += FirmwareDialog_FileOk;
        }

        private void FirmwareDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FirmwareTextBox.Text = firmwareDialog.FileName;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
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
            IdracRedfishController idrac = new IdracRedfishController(server);
            OutputTextBox.AppendText("Iniciando upload do arquivo " + FirmwareTextBox.Text + " para " + server.Host + "\n");
            try
            {
                IdracJob job = await idrac.UpdateFirmware(path, option);
                OutputTextBox.AppendText("JOb Status: " + job.Message + "\n");
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText("Erro: " + ex.Message + "\n");
            }
            
        }

        private void OpenFirmwareButton_Click(object sender, RoutedEventArgs e)
        {
            firmwareDialog.ShowDialog();
        }
    }
}
