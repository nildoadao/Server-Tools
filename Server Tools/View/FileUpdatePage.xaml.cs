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
            CheckRedfishSupport();
            //UpdateFirmware();
        }

        private async void UpdateFirmware()
        {
            IdracRedfishController idrac = new IdracRedfishController(new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password));
            try
            {
                string jobId = await idrac.UploadIdracFirmware(FirmwareTextBox.Text, IdracInstallOption.NextReboot);
                OutputTextBox.AppendText(jobId);
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText("Erro: " + ex.Message + "\n");
            }
        }

        private async void CheckRedfishSupport()
        {
            IdracRedfishController idrac = new IdracRedfishController(new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password));
            try
            {
                bool support = await idrac.CheckRedfishSupport(IdracRedfishController.FIRMWARE_INVENTORY);
                {
                    if (support)
                    {
                        OutputTextBox.AppendText("Suporta RedFish\n");
                    }
                    else
                    {
                        OutputTextBox.AppendText("Não suporta Redfish\n");
                    }
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText("Falha ao consultar suporte a API " + ex.Message + "\n");
            }
        }

        private void OpenFirmwareButton_Click(object sender, RoutedEventArgs e)
        {
            firmwareDialog.ShowDialog();
        }
    }
}
