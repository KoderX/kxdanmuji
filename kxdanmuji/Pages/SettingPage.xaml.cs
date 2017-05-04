using System.Windows;
using System.Windows.Controls;

namespace kxdanmuji.Pages {
    /// <summary>
    /// SettingPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingPage : Page {

        private MainWindow mainWindow;

        public SettingPage(MainWindow main) {
            InitializeComponent();
            mainWindow = main;
        }

        private void btnBack_Click(object sender, RoutedEventArgs e) {
            mainWindow.HideSetting();
        }
    }
}
