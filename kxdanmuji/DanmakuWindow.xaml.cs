using KxLib.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace kxdanmuji {
    /// <summary>
    /// 弹幕侧边栏窗口
    /// </summary>
    public partial class DanmakuWindow : Window {
        private ObservableCollection<Danmaku> dmList = new ObservableCollection<Danmaku>();
        private Task clearListTask;
        private bool closed=false;
        public DanmakuWindow() {
            WindowHelper.TransparentWindow(this);
            InitializeComponent();

        }

        public void SendDanmaku(Danmaku dm) {
            // 弹幕最长存活时间8秒
            dm.DeadTime = DateTime.Now.AddSeconds(8);
            dmList.Add(dm);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            this.Width = 240;
            this.Height = SystemParameters.WorkArea.Height;
            this.Topmost = true;
            this.Top = 0;
            this.Left = SystemParameters.WorkArea.Right - this.Width;
            // 绑定数据源
            lbDanmaku.ItemsSource = dmList;
            clearListTask = new Task(clearList);
            clearListTask.Start();
        }

        private void clearList() {
            // 清理冗余弹幕
            while (!closed) {
                if (dmList.Count>0 && DateTime.Compare(DateTime.Now, dmList[0].DeadTime) > 0) {
                    this.Dispatcher.Invoke(new Action(() => {
                        dmList.RemoveAt(0);
                    }));
                } else {
                    Thread.Sleep(1000);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            closed = true;
        }
    }

}
