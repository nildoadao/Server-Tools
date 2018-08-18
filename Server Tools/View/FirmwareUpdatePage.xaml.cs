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

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para FirmwareUpdatePage.xam
    /// </summary>
    public partial class FirmwareUpdatePage : Page
    {
        ObservableCollection<IdracUpdateItem> datagridItems;
        private bool reboot;
        private string repositorySource;

        public bool Reboot { get => reboot; set => reboot = value; }
        public string RepositorySource { get => repositorySource; set => repositorySource = value; }

        public FirmwareUpdatePage()
        {
            InitializeComponent();
            datagridItems = new ObservableCollection<IdracUpdateItem>();
            reboot = false;
            repositorySource = "FTP";
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
            foreach (string server in ServersListBox.Items)
            {
                if (!NetworkHelper.IsConnected(server))
                {
                    OutputTextBox.AppendText("Servidor " + server + " inacessivel\n");
                    continue;
                }
            }
        }

        public bool ValidateForm()
        {
            if(ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao menos um servidor para atualização", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (UserTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Insira o nome do usuario para atualização", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (PasswordBox.Password.Trim().Equals(""))
            {
                MessageBox.Show("Insira a senha para atualização", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (RepositoryTextBox.Text.Trim().Equals(""))
            {
                MessageBox.Show("Informe o repositorio de armazenamento dos firmwares", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void FtpRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            repositorySource = "FTP";
        }

        private void TftpRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            repositorySource = "TFTP";
        }

        private void NfsRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            repositorySource = "NFS";
        }

        private void RebootCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            reboot = true;
        }

        private void RebootCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            reboot = false;
        }
    }
}
