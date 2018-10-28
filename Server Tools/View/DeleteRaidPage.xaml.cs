using Server_Tools.Idrac.Controllers;
using Server_Tools.Idrac.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interação lógica para DeleteRaidPage.xam
    /// </summary>
    public partial class DeleteRaidPage : Page
    {
        private Server server;

        public DeleteRaidPage(Server server)
        {
            this.server = server;
            InitializeComponent();
        }

        private class VolumeItem
        {
            public bool IsSelected { get; set; }
            public VirtualDisk Volume { get; set; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDataGrid();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            var selectedVolumes = from VolumeItem item in VdDataGrid.ItemsSource
                                  where item.IsSelected
                                  select item.Volume;

            if (MessageBox.Show("Deseja mesmo excluir o(s) volume(s) selecionado(s) ?", "Aviso", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                foreach(var item in selectedVolumes)
                {
                    DeleteVd(item);
                }
            }
        }

        private async void LoadDataGrid()
        {
            try
            {
                var idrac = new StorageController(server);
                List<VirtualDisk> volumes = await idrac.GetAllVirtualDisks();
                ObservableCollection<VolumeItem> datagridItems = new ObservableCollection<VolumeItem>();
                foreach (var item in volumes)
                {
                    datagridItems.Add(new VolumeItem { IsSelected = false, Volume = item });
                }
                VdDataGrid.ItemsSource = datagridItems;
            }
            catch
            {
                MessageBox.Show("Falha ao receber os Volumes do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private async void DeleteVd(VirtualDisk volume)
        {
            try
            {
                var idrac = new StorageController(server);
                IdracJob job = await idrac.DeleteVirtualDisk(volume);
                var load = new LoadWindow(server, job) { Title = server.Host };
                load.Closed += (object sender, EventArgs e) =>
                {
                    var window = (LoadWindow)sender;
                    job = window.Job;
                    if (job.JobState.Contains("Completed"))
                        MessageBox.Show("Volume excluido com sucesso !", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                };
                load.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Falha ao criar o Job {0}", ex.Message), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LoadDataGrid();
        }

        private bool CheckForm()
        {
            if(VdDataGrid.Items == null || VdDataGrid.Items.Count == 0)
            {
                MessageBox.Show("Não há volumes para serem excluidos", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            var selectedVolumes = from VolumeItem item in VdDataGrid.ItemsSource
                                  where item.IsSelected
                                  select item.Volume;

            if (selectedVolumes.Count() == 0)
            {
                MessageBox.Show("Selecione ao menos um VD", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            return true;
        }
    }
}
