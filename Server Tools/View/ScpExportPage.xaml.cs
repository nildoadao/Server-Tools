using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using Syroot.Windows.IO;
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
    /// Interação lógica para ScpExportPage.xam
    /// </summary>
    public partial class ScpExportPage : Page
    {
        public ScpExportPage()
        {
            InitializeComponent();
        }

        private void AllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox item in TargetGroup.Children)
            {
                if (item.Content.ToString() != "ALL")
                {
                    item.IsChecked = false;
                    item.IsEnabled = false;
                }
            }
        }

        private void AllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox item in TargetGroup.Children)
            {
                if (item.Content.ToString() != "ALL")
                    item.IsEnabled = true;
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            string target = "";

            if (AllCheckBox.IsChecked.Value)
                target = "ALL";
            else
            {
                bool first = true;
                foreach (CheckBox item in TargetGroup.Children)
                {
                    if (item.IsChecked.Value && first)
                    {
                        target += item.Content.ToString();
                        first = false;
                    }
                    else if(item.IsChecked.Value)
                        target += string.Format(", {0}", item.Content.ToString());                   
                }
            }

            if (String.IsNullOrEmpty(target))
            {
                MessageBox.Show("Selecione uma opção de Export", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
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
            ExportScpFile(server, target, exportUse);
        }

        private async void ExportScpFile(Server server, string target, string exportUse)
        {
            if (!NetworkHelper.IsConnected(server.Host))
            {
                MessageBox.Show("Servidor inacessivel", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ScpController idrac = new ScpController(server);
            try
            {
                ExportButton.IsEnabled = false;
                OutputTextBox.AppendText(string.Format("Exportando configurações de {0}...\n", server.Host));
                IdracJob job = await idrac.ExportScpFile(target, exportUse);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow) sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        SaveFile(server, job);
                };
                load.Show();
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao exportar arquivo: {0}\n", ex.Message));
                ExportButton.IsEnabled = true;
            }           
        }

        private async void SaveFile(Server server, IdracJob job)
        {
            try
            {
                var idrac = new ScpController(server);
                string file = await idrac.GetScpFileData(job.Id);
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
    }
}
