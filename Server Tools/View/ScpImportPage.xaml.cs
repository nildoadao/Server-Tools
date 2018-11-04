using Microsoft.Win32;
using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using Server_Tools.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
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
        OpenFileDialog fileDialog;
        OpenFileDialog csvDialog;
        ObservableCollection<ServerJob> jobQueue;
        DispatcherTimer timer;

        private class ServerJob
        {
            public Server Server {get; set;}
            public IdracJob Job { get; set; }
        }

        public ScpImportPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            fileDialog = new OpenFileDialog()
            {
                Filter = "Arquivos SCP (*.xml)|*.xml",
            };
            fileDialog.FileOk += FileDialog_FileOk;
            csvDialog = new OpenFileDialog()
            {
                Filter = "Arquivos CSV|*csv"
            };
            csvDialog.FileOk += CsvDialog_FileOk;
            jobQueue = new ObservableCollection<ServerJob>();
            JobsDataGrid.ItemsSource = jobQueue;
            timer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 10)
            };
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateJobs();
        }

        private void FileDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FileTextBox.Text = fileDialog.FileName;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrEmpty(ServerTextBox.Text))
            {
                if (String.IsNullOrEmpty(UserTextBox.Text) | String.IsNullOrEmpty(PasswordBox.Password))
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

        private void AddCsvButton_Click(object sender, RoutedEventArgs e)
        {
            csvDialog.ShowDialog();
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
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
            foreach (Server server in ServersListBox.Items)
            {
                ImportScp(server, path, target, shutdown);
            }
        }

        private async void ImportScp(Server server, string path, string target, string shutdown)
        {
            if (!NetworkHelper.IsConnected(server.Host))
            {
                MessageBox.Show("Servidor inacessivel", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ScpController idrac = new ScpController(server);           
            try
            {
                OutputTextBox.AppendText(string.Format("Importando configurações para {0}...\n", server.Host));
                IdracJob job = await idrac.ImportScpFile(path, target, shutdown, "On");
                OutputTextBox.AppendText(string.Format("Job {0} criado para servidor {1} \n", job.Id, server));
                jobQueue.Add(new ServerJob { Server = server, Job = job });
            }
            catch(Exception ex)
            {
                OutputTextBox.AppendText("Falha ao importar arquivo: " + ex.Message);
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            fileDialog.ShowDialog();
        }

        private void CsvDialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                IEnumerable<Server> servers = FileHelper.ReadCsvFile(csvDialog.FileName);
                foreach (Server server in servers)
                {
                    ServersListBox.Items.Add(server);
                }
            }
            catch (Exception ex)
            {
                OutputTextBox.AppendText("Falha ao carregar CSV: " + ex.Message + "\n");
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
            else if (String.IsNullOrEmpty(FileTextBox.Text))
            {
                MessageBox.Show("Selecione um arquivo", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else if (ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um servidor para aplicar o template", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            else
            {
                return true;
            }
        }

        private async void UpdateJobs()
        {
            var updatedJobs = new ObservableCollection<ServerJob>();
            foreach(var job in jobQueue)
            {
                var idrac = new JobController(job.Server);
                var updatedJob = await idrac.GetJob(job.Job.Id);
                updatedJobs.Add(new ServerJob { Server = job.Server, Job = updatedJob });
            }
            jobQueue = updatedJobs;
            JobsDataGrid.ItemsSource = jobQueue;
        }
    }
}
