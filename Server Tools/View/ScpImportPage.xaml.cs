using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para ScpImportPage.xam
    /// </summary>
    public partial class ScpImportPage : Page
    {
        OpenFileDialog FileDialog;
        OpenFileDialog CsvDialog;
        DispatcherTimer timer;

        private class ServerJob
        {
            public Server Server {get; set;}
            public IdracJob Job { get; set; }
            public string SerialNumber { get; set; }
        }

        public ScpImportPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            FileDialog = new OpenFileDialog()
            {
                Filter = "Arquivos SCP (*.xml)|*.xml",
            };
            FileDialog.FileOk += FileDialog_FileOk;
            CsvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            CsvDialog.FileOk += CsvDialog_FileOk;
            JobsDataGrid.ItemsSource = new List<ServerJob>();
            timer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 10)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateJobsAsync();
        }

        private void FileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileTextBox.Text = FileDialog.FileName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(ServerTextBox.Text) || String.IsNullOrEmpty(UserTextBox.Text) || String.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Insira as informações da Idrac", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Server server = new Server(ServerTextBox.Text, UserTextBox.Text, PasswordBox.Password);
            ServersListBox.Items.Add(server);
            ServerTextBox.Clear();
            UserTextBox.Clear();
            PasswordBox.Clear();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            ServersListBox.Items.Remove(ServersListBox.SelectedItem);
        }

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            CsvDialog.ShowDialog();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
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
            string shutdown = "";
            foreach (RadioButton item in ShutdownGroup.Children)
            {
                if (item.IsChecked.Value)
                {
                    shutdown = item.Content.ToString();
                    break;
                }
            }
            string path = FileTextBox.Text;
            foreach(Server server in ServersListBox.Items)
            {
                OutputTextBox.AppendText(string.Format("Importando configurações para {0}...\n", server.Host));
                ImportScpAsync(server, path, target, shutdown);
            }                    
        }

        private async void ImportScpAsync(Server server, string path, string target, string shutdown)
        {
            if(!await NetworkHelper.CheckConnectionAsync(server.Host))
            {
                OutputTextBox.AppendText(string.Format("Servidor {0} inacessivel\n", server.Host));
                return;
            }
                           
            try
            {
                ScpController idrac = new ScpController(server);               
                IdracJob job = await idrac.ImportScpFileAsync(path, target, shutdown, "On");
                OutputTextBox.AppendText(string.Format("Job {0} criado para servidor {1}\n", job.Id, server));
                var chassisIdrac = new ChassisController(server);
                var chassis = await chassisIdrac.GetChassisAsync();
                var jobs = (List<ServerJob>) JobsDataGrid.ItemsSource;
                jobs.Add(new ServerJob { Server = server, Job = job, SerialNumber = chassis.SKU });
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText(string.Format("Falha ao importar arquivo {0}\n", ex.Message));
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.ShowDialog();
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                IEnumerable<Server> servers = FileHelper.ReadCsvFile(CsvDialog.FileName);
                foreach (Server server in servers)
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

        private bool CheckForm()
        {
            if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um servidor para aplicar o template", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (String.IsNullOrEmpty(FileTextBox.Text))
            {
                MessageBox.Show("Selecione um arquivo", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
                return true;
        }

        private async void UpdateJobsAsync()
        {
            var updatedJobs = new List<ServerJob>();
            foreach(ServerJob job in JobsDataGrid.ItemsSource)
            {
                try
                {
                    var idrac = new JobController(job.Server);
                    var updatedJob = await idrac.GetJobAsync(job.Job.Id);
                    var chassisIdrac = new ChassisController(job.Server);
                    var chassis = await chassisIdrac.GetChassisAsync();
                    updatedJobs.Add(new ServerJob { Server = job.Server, Job = updatedJob, SerialNumber = chassis.SKU });
                }
                catch
                {
                    OutputTextBox.AppendText("Falha ao atualizar status dos Jobs\n");
                }
            }
            JobsDataGrid.ItemsSource = updatedJobs;
        }
    }
}
