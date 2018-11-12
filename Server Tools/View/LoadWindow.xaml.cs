using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
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
using System.Windows.Shapes;

namespace Server_Tools.View
{
    /// <summary>
    /// Lógica interna para LoadWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        Server server;
        IdracJob job;

        public IdracJob Job { get { return job; } }

        public LoadWindow(Server server, IdracJob job)
        {
            this.server = server;
            this.job = job;
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = "Server Tools";
            TaskTextBlock.Text = job.Name;
            LoopJob();
        }

        private void UpdateProgressBar(IdracJob job)
        {
            TaskProgressBar.Value = job.PercentComplete;
            StatusTextBlock.Text = string.Format("Status {0}%: {1}", job.PercentComplete.ToString(), job.Message);
        }

        private async void LoopJob()
        {
            var idrac = new JobController(server);
            var time = DateTime.Now;
            while (true)
            {
                try
                {
                    job = await idrac.GetJob(job.Id);
                    UpdateProgressBar(job);

                    if (job.JobState.Contains("Completed"))
                        break;

                    else if (job.JobState.Contains("Failed"))
                        throw new Exception(string.Format("Falha ao executar o Job: {0}", job.Message));

                    else if (DateTime.Now >= time.AddMinutes(JobController.JobTimeout))
                        throw new TimeoutException("Excedido tempo limite do Job");
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            Close();
        }
    }
}
