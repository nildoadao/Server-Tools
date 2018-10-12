using Microsoft.Win32;
using Server_Tools.Idrac;
using Server_Tools.Model;
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
        OpenFileDialog fileDialog;
        OpenFileDialog csvDialog;
        ObservableCollection<ServerJob> jobQueue;
        DispatcherTimer timer;

        public ScpImportPage()
        {
            InitializeComponent();
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
            if(ServersListBox.Items.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um servidor para aplicar o template", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach(Server server in ServersListBox.Items)
            {
                ImportScp(server);
            }
        }

        private async void ImportScp(Server server)
        {
            ScpController idrac = new ScpController(server);
            OutputTextBox.AppendText(string.Format("Importando configurações para {0}...\n", server.Host));
            string target = "";
            if (AllCheckBox.IsChecked.Value)
            {
                target = "ALL";
            }
            else
            {
                bool first = true;
                foreach (CheckBox item in TargetGroup.Children)
                {
                    if (item.IsChecked.Value)
                    {
                        if (first)
                        {
                            target += item.Content.ToString();
                            first = false;
                        }
                        else
                        {
                            target += string.Format(", {0}", item.Content.ToString());
                        }
                    }
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
            try
            {
                IdracJob job = await idrac.ImportScpFile(FileTextBox.Text, target, shutdown, "On");
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

        private bool ValidateForm()
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
            else
            {
                return true;
            }
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
                {
                    item.IsEnabled = true;
                }
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
