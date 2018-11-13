using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Server_Tools.View
{
    /// <summary>
    /// Interação lógica para FirmwarePage.xam
    /// </summary>
    public partial class FirmwarePage : Page
    {
        public FirmwarePage()
        {
            InitializeComponent();
        }

        private void RepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RepositoryUpdatePage());
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new FileUpdatePage());
        }
    }
}
