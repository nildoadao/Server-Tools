using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para CustomScriptPage.xaml
    /// </summary>
    public partial class CustomScriptPage : Page
    {
        OpenFileDialog CsvDialog;
        OpenFileDialog ScriptDialog;

        public CustomScriptPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CsvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            CsvDialog.FileOk += CsvDialog_FileOk;
            ScriptDialog = new OpenFileDialog()
            {
                Filter = "Arquivos Txt|*txt"
            };
            ScriptDialog.FileOk += ScriptDialog_FileOk;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(UserTextBox.Text) | String.IsNullOrEmpty(PasswordBox.Password) | String.IsNullOrEmpty(ServerTextBox.Text))
            {
                MessageBox.Show("Preencha os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            ServersListBox.Items.Add(server);
            ServerTextBox.Clear();

            if (!KeepCheckbox.IsChecked.Value)
            {
                UserTextBox.Clear();
                PasswordBox.Clear();
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Clear();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach(Server server in FileHelper.ReadCsvFile(CsvDialog.FileName))
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch
            {
                MessageBox.Show("Falha ao carregar arquivo CSV, certifique que o arquivo está no formato correto e tente novamente\n\nFormato esperado:\n<hostname>;<usuario>;<senha>", 
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            ApplyButton.IsEnabled = false;
            ClearButton.IsEnabled = false;
            string path = ScriptTextBox.Text;
            await RunScriptAsync(path);
        }

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao menos um servidor para aplicar o script", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (String.IsNullOrEmpty(ScriptDialog.FileName))
            {
                MessageBox.Show("Selecione um script a ser aplicado", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private async Task RunScriptAsync(string path)
        {
            foreach(Server server in ServersListBox.Items)
            {
                if (!await NetworkHelper.CheckConnectionAsync(server.Host))
                {
                    OutputTextBox.AppendText(string.Format("Servidor {0} inacessivel.\n", server.Host));
                    continue;
                }
                try
                {
                    OutputTextBox.AppendText(string.Format("Aplicando script para {0}\n", server.Host));
                    var idrac = new IdracSshController(server);
                    string[] script = File.ReadAllLines(path);
                    foreach (var command in script)
                    {
                        idrac.RunCommand(command);
                    }
                }
                catch (Exception ex)
                {
                    OutputTextBox.AppendText(string.Format("Falha ao executar o script para {0} : {1}\n", server.Host, ex.Message));
                }
            }
            ApplyButton.IsEnabled = true;
            ClearButton.IsEnabled = true;
        }

        private void ScriptDialogButton_Click(object sender, RoutedEventArgs e)
        {
            ScriptDialog.ShowDialog();
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            CsvDialog.ShowDialog();
        }

        private void ScriptDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ScriptTextBox.Text = ScriptDialog.FileName;
        }

    }
}
