using Microsoft.Win32;
using Renci.SshNet;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
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

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                IEnumerable<Server> servers = FileHelper.ReadCsvFile(csvDialog.FileName);
                foreach(Server server in servers)
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao carregar CSV: {0}\n", ex.Message));
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Insira ao menos um servidor para aplicar o script", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else if (scriptDialog.FileName.Trim().Equals(""))
            {
                MessageBox.Show("Selecione um script a ser aplicado", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            ApplyButton.IsEnabled = false;
            try
            {
                ApplyScript();
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao aplicar o script: {0}\n", ex.Message));
            }
        }

        private async void ApplyScript()
        {
            foreach (Server server in ServersListBox.Items)
            {
                var script = FileHelper.ReadTxtFile(ScriptTextBox.Text);
                await Task.Run(() =>
                {
                    try
                    {
                        var idrac = new IdracSshCommand(server);
                        foreach (var command in script)
                        {
                            idrac.RunCommand(command);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(string.Format("Script com sucesso para {0}\n", server));
                        });
                    }
                    catch(Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OutputTextBox.AppendText(string.Format("Falha ao aplicar script para {0}: {1}\n", server, ex.Message));
                        });
                    }
                });

                if (operationCancelled)
                {
                    OutputTextBox.AppendText("Operação cancelada pelo usuário\n");
                    operationCancelled = false;
                    ApplyButton.IsEnabled = true;
                    break;
                }
            }
            ApplyButton.IsEnabled = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!operationCancelled)
            {
                operationCancelled = true;
            }
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
