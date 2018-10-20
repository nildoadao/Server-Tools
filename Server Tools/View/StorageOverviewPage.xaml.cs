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
        private List<PhysicalDisk> disks;
        private List<Enclousure> enclousures;
        private List<RaidController> controllers;
        private List<VirtualDisk> virtualDisks;

        public StorageOverviewPage(Server server)
        {
            InitializeComponent();
            this.server = server;
            PageHeader.Text = string.Format("Storage {0}", server.Host);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            GetStorageData();
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
            NavigationService.Navigate(new CreateRaidPage(disks, controllers));
        }

        private void DeleteVdButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void GetStorageData()
        {
            try
            {
                var idrac = new StorageController(server);
                disks = await idrac.GetAllPhysicalDisks();
                enclousures = await idrac.GetAllEnclousures();
                controllers = await idrac.GetAllRaidControllers();
                virtualDisks = await idrac.GetAllVirtualDisks();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Falha ao ler dados de Storage: {0}", ex.Message), "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateForm();
            }
        }
      
        private void UpdateForm()
        {
            PhysicalDiskDatagrid.ItemsSource = disks;
            PopulateDiskGroupBox();
            PopulateControllerGroupBox();
            PopulateVirtualDiskGroupBox();
        }

        private void PopulateDiskGroupBox()
        {
            ServerTextBlock.Text = server.Host;
            PhysicalDiskCountTextBlock.Text = (disks == null) ? "Desconhecido" : disks.Count.ToString();
            UnassignedDiskCountTextBlock.Text = (disks == null) ? "Desconhecido" : UnassignedDisksCount(disks).ToString();
            VirtualDiskCountTextBlock.Text = (disks == null) ? "Desconhecido" : virtualDisks.Count.ToString();
        }

        private void PopulateControllerGroupBox()
        {
            if (controllers == null)
                return;

            ControllersComboBox.Items.Clear();
            foreach(RaidController controller in controllers)
            {
                ControllersComboBox.Items.Add(controller);
            }
        }

        private void PopulateVirtualDiskGroupBox()
        {
            if (virtualDisks == null)
                return;

            VirtualDisksComboBox.Items.Clear();
            foreach(VirtualDisk item in virtualDisks)
            {
                VirtualDisksComboBox.Items.Add(item);
            }
        }
        
        private async void LoadPhysicalDisks(VirtualDisk virtualDisk)
        {
            if (virtualDisk == null)
                return;

            var idrac = new StorageController(server);
            PhysicalDisksItems.Items.Clear();
            foreach (PhysicalDisk item in await idrac.GetPhysicalDisks(virtualDisk))
            {
                PhysicalDisksItems.Items.Add(item);
            }
        }

        private int UnassignedDisksCount(IEnumerable<PhysicalDisk> disks)
        {
            int count = 0;

            if (disks == null)
                return 0;

            foreach(PhysicalDisk item in disks)
            {
                if (item.Links == null)
                    count++;
                else if (item.Links.Volumes.Count == 0)
                    count++;
            }
            return count;
        }

        private int VolumesCount(IEnumerable<PhysicalDisk> disks)
        {
            if (disks == null)
                return 0;

            int count = 0;
            foreach(PhysicalDisk disk in disks)
            {
                if (disk.Links != null)
                    count += disk.Links.Volumes.Count;
            }
            return count;
        }


    }
}
