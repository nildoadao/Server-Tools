
using Server_Tools.Model;
using Server_Tools.Util;
using Server_Tools.Control;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
