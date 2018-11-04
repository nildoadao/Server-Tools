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
    /// Interação lógica para StorageOverviewPage.xam
    /// </summary>
    public partial class StorageOverviewPage : Page
    {
        // Classe interna para os dados do datagrid

        private class VolumeItem
        {
            public bool IsSelected { get; set; }
            public VirtualDisk Volume { get; set; }
        }

        private Server server;

        public StorageOverviewPage(Server server)
        {
            InitializeComponent();
            this.server = server;
            PageHeader.Text = string.Format("Storage {0}", server.Host);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateForm();
        }

        private void OverviewButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewPageContent.SelectedIndex = 0;
        }

        private void ControllerButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewPageContent.SelectedIndex = 1;
        }

        private void PhysicalDiskButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewPageContent.SelectedIndex = 2;
        }

        private void VirtualDiskButton_Click(object sender, RoutedEventArgs e)
        {
            OverviewPageContent.SelectedIndex = 3;
        }

        private void ControllersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var controller = ControllersComboBox.SelectedItem as RaidController;
            FirmwareVersionTextBlock.Text = controller.FirmwareVersion;
            ControllerModelTextBlock.Text = controller.Model;
        }

        private void CreateVdButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateRaidPage(server));
        }

        private void DeleteVdButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForm())
                return;

            var selectedVolumes = from VolumeItem item in VdDataGrid.ItemsSource
                                  where item.IsSelected
                                  select item.Volume;

            if(selectedVolumes.Count() > 1)
            {
                MessageBox.Show("Selecione apenas um Volume para ser excluido", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("Deseja mesmo excluir o volume selecionado ?", "Aviso", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                DeleteVd(selectedVolumes.FirstOrDefault());
            }
        }
      
        private void UpdateForm()
        {
            UpdateDisks();
            UpdateControllers();
            UpdateVirtualDisks();
        }

        private async void UpdateDisks()
        {
            try
            {
                var idrac = new StorageController(server);
                List<PhysicalDisk> disks = await idrac.GetAllPhysicalDisks();
                PhysicalDiskDatagrid.ItemsSource = disks;
                ServerTextBlock.Text = server.Host;
                PhysicalDiskCountTextBlock.Text = disks.Count.ToString();
                UnassignedDiskCountTextBlock.Text = UnassignedDisksCount(disks).ToString();               
            }
            catch
            {
                MessageBox.Show("Falha ao receber os dados de disco do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateControllers()
        {
            try
            {
                var idrac = new StorageController(server);
                ControllersComboBox.Items.Clear();
                foreach (RaidController controller in await idrac.GetAllRaidControllers())
                {
                    ControllersComboBox.Items.Add(controller);
                }
            }
            catch
            {
                MessageBox.Show("Falha ao receber os dados de controladoras do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UpdateVirtualDisks()
        {
            try
            {
                var idrac = new StorageController(server);
                List<VirtualDisk> volumes = await idrac.GetAllVirtualDisks();
                var datagridItems = new ObservableCollection<VolumeItem>();
                foreach (var item in volumes)
                {
                    datagridItems.Add(new VolumeItem { IsSelected = false, Volume = item });
                }
                VirtualDiskCountTextBlock.Text = volumes.Count().ToString();
                VdDataGrid.ItemsSource = datagridItems;
            }
            catch
            {
                MessageBox.Show("Falha ao receber os Volumes do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int UnassignedDisksCount(IEnumerable<PhysicalDisk> disks)
        {
            if (disks == null)
                return 0;

            var unassigned = from item in disks
                             where item.Links.Volumes.Count == 0
                             select item;

            return unassigned.Count();
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
            UpdateForm();
        }

        private bool CheckForm()
        {
            if (VdDataGrid.Items == null || VdDataGrid.Items.Count == 0)
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
