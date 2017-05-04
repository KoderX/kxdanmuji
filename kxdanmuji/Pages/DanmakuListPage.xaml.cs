using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace kxdanmuji.Pages {
    /// <summary>
    /// DanmakuListPage.xaml 的交互逻辑
    /// </summary>
    public partial class DanmakuListPage : Page {
        private MainWindow mainWindow;
        private ObservableCollection<Danmaku> dmList = new ObservableCollection<Danmaku>();
        public DanmakuListPage(MainWindow main) {
            InitializeComponent();
            mainWindow = main;
            // 绑定弹幕源
            lbList.ItemsSource = dmList;
        }

        public void AddDanmaku(Danmaku dm) {
            dmList.Add(dm);
            var maxListCount = 100;
            if (dmList.Count > maxListCount) {
                dmList.RemoveAt(0);
            }
            lbList.ScrollIntoView(lbList.Items[lbList.Items.Count - 1]);
        }
    }
}
