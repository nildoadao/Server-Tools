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

        private void VirtualDisksComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var virtualDisk = VirtualDisksComboBox.SelectedItem as VirtualDisk;
            LoadPhysicalDisks(virtualDisk);
        }

        private void CreateVdButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new CreateRaidPage(server));
        }

        private void DeleteVdButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new DeleteRaidPage(server));
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
                VirtualDisksComboBox.Items.Clear();
                List<VirtualDisk> volumes = await idrac.GetAllVirtualDisks();
                foreach (VirtualDisk item in volumes)
                {
                    VirtualDisksComboBox.Items.Add(item);
                }
                VirtualDiskCountTextBlock.Text = volumes.Count.ToString();
            }
            catch
            {
                MessageBox.Show("Falha ao receber os dados de volumes do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private async void LoadPhysicalDisks(VirtualDisk volume)
        {
            try
            {
                var idrac = new StorageController(server);
                PhysicalDisksItems.Items.Clear();
                List<PhysicalDisk> disks = await idrac.GetPhysicalDisks(volume);
                foreach (PhysicalDisk item in disks)
                {
                    PhysicalDisksItems.Items.Add(item);
                }
            }
            catch
            {
                MessageBox.Show("Falha ao receber os dados dos discos do servidor", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
