using Microsoft.Win32;
using Renci.SshNet;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interação lógica para CustomScriptPage.xam
    /// </summary>
    public partial class CustomScriptPage : Page
    {
        OpenFileDialog csvDialog;
        OpenFileDialog scriptDialog;
        bool operationCancelled;

        public CustomScriptPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            csvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            csvDialog.FileOk += CsvDialog_FileOk;
            scriptDialog = new OpenFileDialog()
            {
                Filter = "Arquivos Txt|*txt"
            };
            scriptDialog.FileOk += ScriptDialog_FileOk;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(UserTextBox.Text) | String.IsNullOrEmpty(PasswordBox.Password) | String.IsNullOrEmpty(ServerTextBox.Text))
                MessageBox.Show("Preencha os dados da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);

            else
            {
                Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
                ServersListBox.Items.Add(server);
                ServerTextBox.Clear();
                UserTextBox.Clear();
                PasswordBox.Clear();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach(Server server in FileHelper.ReadCsvFile(csvDialog.FileName))
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch(Exception)
            {
                MessageBox.Show("Falha ao ler arquivo CSV, cheque o arquivo e tente novamente", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            ApplyButton.IsEnabled = false;
            ApplyScript();
        }

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao menos um servidor para aplicar o script", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (String.IsNullOrEmpty(scriptDialog.FileName))
            {
                MessageBox.Show("Selecione um script a ser aplicado", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            return true;
        }

        private async void ApplyScript()
        {
            foreach (Server server in ServersListBox.Items)
            {
                try
                {
                    var script = File.ReadAllLines(ScriptTextBox.Text);
                    await RunScriptAsync(server, script);

                    if (operationCancelled)
                    {
                        OutputTextBox.AppendText("Operação cancelada pelo usuário\n");
                        operationCancelled = false;
                        break;
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(string.Format("Falha ao executar o script : {0}", ex.Message), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }

            }
            ApplyButton.IsEnabled = true;
        }

        private async Task<string> RunScriptAsync(Server server, IEnumerable<string> script)
        {
            string result = "";
            await Task.Run(() =>
            {
                try
                {
                    if (!NetworkHelper.IsConnected(server.Host))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(string.Format("Servidor {0} ínacessivel.\n", server));
                        });
                        return;
                    }
                    var idrac = new IdracSshController(server);
                    foreach(var command in script)
                    {
                        result = idrac.RunCommand(command);
                    }
                }
                catch(Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        OutputTextBox.AppendText(string.Format("Falha ao aplicar script para {0}: {1}\n", server, ex.Message));
                    });
                }
            });
            return result;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!operationCancelled)
                operationCancelled = true;
        }

        private void ScriptDialogButton_Click(object sender, RoutedEventArgs e)
        {
            scriptDialog.ShowDialog();
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            csvDialog.ShowDialog();
        }

        private void ScriptDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ScriptTextBox.Text = scriptDialog.FileName;
        }

    }
}
