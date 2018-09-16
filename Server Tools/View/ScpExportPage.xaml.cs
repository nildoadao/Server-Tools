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
    /// Interação lógica para ScpExportPage.xam
    /// </summary>
    public partial class ScpExportPage : Page
    {
        public ScpExportPage()
        {
            InitializeComponent();
        }

        private async void ExportScpFile(IdracScpTarget target)
        {
            ExportButton.IsEnabled = false;
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            IdracRedfishController idrac = new IdracRedfishController(server);
            OutputTextBox.AppendText("Exportando configurações de " + server.Host + "...\n");
            try
            {
                string file = await idrac.ExportScpFile(target);
                OutputTextBox.AppendText("Arquivo importado com sucesso, salvo em: " + file + "\n");
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText("Falha ao exportar arquivo: " + ex.Message + "\n");
            }
            ExportButton.IsEnabled = true;
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
            else
            {
                return true;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            if (AllRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.ALL);
            }
            else if (SystemRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.System);
            }
            else if (BiosRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.BIOS);
            }
            else if (FcRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.FC);
            }
            else if (IdracRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.IDRAC);
            }
            else if (LifeCycleRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.LidecycleController);
            }
            else if (NicRadioButton.IsChecked.Value)
            {
                ExportScpFile(IdracScpTarget.NIC);
            }
            else
            {
                ExportScpFile(IdracScpTarget.RAID);
            }
        }
    }
}
