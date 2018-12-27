using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using Syroot.Windows.IO;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            string target = "";
            foreach(RadioButton item in TargetGroup.Children)
            {
                if (item.IsChecked.Value)
                {
                    target = item.Content.ToString();
                    break;
                }
            }
            
            string exportUse = "";
            foreach (RadioButton item in ExportUseGroup.Children)
            {
                if (item.IsChecked.Value)
                {
                    exportUse = item.Content.ToString();
                    break;
                }
            }
            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            await ExportScpFileAsync(server, target, exportUse);
        }

        private async Task ExportScpFileAsync(Server server, string target, string exportUse)
        {
            if (!await NetworkHelper.CheckConnectionAsync(server.Host))
            {
                MessageBox.Show("Servidor inacessivel", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }          
            try
            {
                ScpController idrac = new ScpController(server);
                ExportButton.IsEnabled = false;
                OutputTextBox.AppendText(string.Format("Exportando configurações de {0}...\n", server.Host));
                IdracJob job = await idrac.ExportScpFileAsync(target, exportUse);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += async (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow) sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        await SaveFileAsync(server, job);
                };
                load.Show();
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao exportar arquivo: {0}\n", ex.Message));
                ExportButton.IsEnabled = true;
            }           
        }

        private async Task SaveFileAsync(Server server, IdracJob job)
        {
            try
            {
                var idrac = new ScpController(server);
                string file = await idrac.GetScpFileDataAsync(job.Id);
                string currentTime = DateTime.Now.ToString().Replace(":", "").Replace("/", "").Replace(" ", "");
                string downloadsFolder = KnownFolders.Downloads.Path;
                string path = System.IO.Path.Combine(downloadsFolder, "SCP_" + currentTime + ".xml");
                File.WriteAllText(path, file);
                OutputTextBox.AppendText(string.Format("Arquivo exportado com sucesso, salvo em {0}\n", path));
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao salvar arquivo {0}\n", ex.Message));
            }
            finally
            {
                ExportButton.IsEnabled = true;
            }
        }

        private bool CheckForm()
        {
            if (String.IsNullOrEmpty(ServerTextBox.Text))
            {
                MessageBox.Show("Informe o endereço do host", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (String.IsNullOrEmpty(UserTextBox.Text))
            {
                MessageBox.Show("Informe o usuario", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (String.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Informe a senha", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
