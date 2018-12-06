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
        bool isUpdating;

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
                Interval = new TimeSpan(0, 0, 5)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
            isUpdating = false;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
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

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            CsvDialog.ShowDialog();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            ImportButton.IsEnabled = false;
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
            ImportScpAsync(path, target, shutdown);                  
        }

        private async void ImportScpAsync(string path, string target, string shutdown)
        {
            isUpdating = true;
            timer.Stop();
            List<Server> servers = new List<Server>();
            foreach (Server item in ServersListBox.Items)
                servers.Add(item);

            foreach (Server server in servers)
            {
                if (!await NetworkHelper.CheckConnectionAsync(server.Host))
                {
                    OutputTextBox.AppendText(string.Format("Servidor {0} inacessivel, verifique a conexão e tente novamente.\n", server.Host));
                    continue;
                }
                try
                {
                    OutputTextBox.AppendText(string.Format("Importando configurações para {0}...\n", server.Host));
                    ScpController idrac = new ScpController(server);
                    IdracJob job = await idrac.ImportScpFileAsync(path, target, shutdown, "On");
                    OutputTextBox.AppendText(string.Format("Job {0} criado para servidor {1}\n", job.Id, server));
                    var chassisIdrac = new ChassisController(server);
                    var chassis = await chassisIdrac.GetChassisAsync();
                    var jobs = (List<ServerJob>)JobsDataGrid.ItemsSource;
                    jobs.Add(new ServerJob { Server = server, Job = job, SerialNumber = chassis.SKU });
                }
                catch (Exception ex)
                {
                    OutputTextBox.AppendText(string.Format("Falha ao importar arquivo para {0} {1}\n", server.Host, ex.Message));
                }
            }
            ImportButton.IsEnabled = true;
            isUpdating = false;
            timer.Start();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileDialog.ShowDialog();
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                foreach (var item in FileHelper.ReadCsvFile(CsvDialog.FileName))
                    ServersListBox.Items.Add(item);
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
            List<ServerJob> updatedJobs = new List<ServerJob>();
            List<ServerJob> jobs = new List<ServerJob>();
            jobs.AddRange((List<ServerJob>)JobsDataGrid.ItemsSource);

            foreach(ServerJob job in jobs)
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
            if(!isUpdating)
                JobsDataGrid.ItemsSource = updatedJobs;

            timer.Start();
        }
    }
}
