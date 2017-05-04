using kxdanmuji.Controls;
using System.Windows.Forms;
using System;
using System.Threading;
using KxLib.Utilities.Control;
using System.Text.RegularExpressions;
using KxLib.Utilities;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Media.Imaging;
using kxdanmuji_plugin_framework;

namespace kxdanmuji {
    /// <summary>
    /// LoginWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoginWindow : KxWindow {
        private NotifyIcon notifyIcon;
        private SmoothProgressBar spbBottom;
        private string avatarUrl;
        private BitmapImage biDefaultAvatar = new BitmapImage(new Uri("pack://application:,,,/Images/default_avatar.png", UriKind.RelativeOrAbsolute));
        private bool existUp;
        private bool isInit=false;
        private Thread connectThread;

        public LoginWindow() {
            InitializeComponent();
        }
        private void Init() {
            InitNotify();
            

            // 初始化控件
            spbBottom = new SmoothProgressBar(pbBottom, 8);
            // 读取配置
            existUp = false;
            tbRoomID.Text = Global.LoadConfig("last_room");
            UpdateInfo();
            isInit = true;

            Global.RoomID = null;
        }

        private void UpdateInfo() {
            bool empty=true;
            if (tbRoomID.Text != "" && Regex.IsMatch(tbRoomID.Text, @"^\d+$")) {
                
                var id = Convert.ToInt32(tbRoomID.Text);
                var room = Global.DB.Builder("room").Where("id", id).OrWhere("url", id).First();
                if (room != null) {
                    // 显示头像
                    var avatarPath = $@"avatars\{room["id"]}.jpg";
                    if (File.Exists(avatarPath)) {
                        image.Source = Utils.LoadImageFromFile(avatarPath);
                    }
                    // 显示姓名
                    tbUp.Text = "UP主: " + room["name"];
                    existUp = true;
                    empty = false;
                }
            }
            if (empty && existUp) { 
                image.Source = biDefaultAvatar;
                tbUp.Text = "UP主: 未知";
                existUp = false;
            }
        }

        private void InitNotify() {
            notifyIcon = new NotifyIcon();
            notifyIcon.Text = "B站弹幕姬 2016";
            Uri iconUri = new Uri("pack://application:,,,/Images/notify.ico", UriKind.RelativeOrAbsolute);

            notifyIcon.Icon = new Icon(System.Windows.Application.GetResourceStream(iconUri).Stream);
            notifyIcon.Visible = true;
            notifyIcon.MouseClick += new MouseEventHandler((o, e) => {
                if (e.Button == MouseButtons.Left) {
                    if (this.WindowState != System.Windows.WindowState.Normal) {
                        this.WindowState = System.Windows.WindowState.Normal;
                    }
                    this.Activate();
                }
            });

        }

        private void KxWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            notifyIcon.Visible = false;
        }

        private void KxWindow_Loaded(object sender, System.Windows.RoutedEventArgs e) {
            Init();
            //MessageBox.Show("当前版本为测试版 "+ this.GetType().Name, "提示");
        }
        private void btnConnect_Click(object sender, System.Windows.RoutedEventArgs e) {
            String roomid_str = tbRoomID.Text;
            if (!Regex.IsMatch(roomid_str, @"^\d+$")) {
                ShowMessage("请输入正确的房间号!", "提示");
                return;
            }
            spbBottom.SetValueNow(0);
            Global.RoomURL = Convert.ToInt32(roomid_str);
            connectThread = new Thread(Connect);
            connectThread.Start();
        }
        private void ShowMessage(String message, String title) {
            Logger.Info(message);
            this.Dispatcher.Invoke(new Action(() => {
                MessageBox.Show(message, title);
            }));
        }

        private void tbRoomID_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            if (isInit) { UpdateInfo(); }
        }

        private void ChangeState(int state) {
            if (state == 0) {
                this.Dispatcher.Invoke(new Action(() => {
                    tbRoomID.IsEnabled = true;
                    btnConnect.IsEnabled = true;
                    btnOfflineMode.IsEnabled = true;
                }));
            } else {
                this.Dispatcher.Invoke(new Action(() => {
                    tbRoomID.IsEnabled = false;
                    btnConnect.IsEnabled = false;
                    btnOfflineMode.IsEnabled = false;
                }));
            }
        }

        private void Connect() {
            HttpHelper http = new HttpHelper(Encoding.UTF8);
            ChangeState(1);
            spbBottom.SetValue(1);
            try {
                http.SetTimeOut(5000);
                http.GET(@"http://live.bilibili.com/" + Global.RoomURL);
            } catch(Exception ex) {
                Logger.Error("读取房间信息错误", ex);
                ShowMessage("弹幕姬怀疑您的网络或者B站的网络崩了~", "网络错误");
                ChangeState(0);
                return;
            }
            
            if (http.StatusCode == 302) {
                Logger.Log("302跳转 " + http.ResponseHeaders["location"]);
                http.GET(http.ResponseHeaders["location"]);
                spbBottom.SetValue(2);
            }
            if (http.StatusCode != 200) {
                if (http.StatusCode == 404) {
                    ShowMessage("亲~该房间号不存在啊~", "房间号不存在");
                } else {
                    ShowMessage("亲~请检查一下网络连接", "网络错误");
                }
                ChangeState(0);
                return;
            }
            string s = http.Response;
            if (s.IndexOf("<title>出错啦") > 0) {
                ShowMessage("亲~该房间号不存在啊~", "房间号不存在");
                ChangeState(0);
                return;
            }
            var mID = Regex.Match(s, @"var ROOMID = (\d+);");
            var mUrl = Regex.Match(s, @"var ROOMURL = (\d+);");
            var mName = Regex.Match(s, "<span class=\"info-text\" title=\"(.+)\">(.+)</span>");
            var mTitle = Regex.Match(s, "<h2 class=\"dp-inline-block room-title\" title=\"(.+)\">.+</h2>");
            var mAvatar = Regex.Match(s, "UID\" style=\"background-image: url\\((.+)\\)\"></a>");
            var mLevel = Regex.Match(s, "<i class=\"up-level-icon\" ms-class=\"lv-(\\d+)\"");
            var mLiveState = Regex.Match(s, "<div class=\"live-switcher(| pointer) on\"");

            try {
                Global.RoomURL = Convert.ToInt32(mUrl.Groups[1].Value);
                Global.RoomID = Convert.ToInt32(mID.Groups[1].Value);
                Global.RoomUpName = mName.Groups[1].Value;
                Global.RoomTitle = mTitle.Groups[1].Value;
                Global.RoomLevel = Convert.ToInt32(mLevel.Groups[1].Value);
                Global.RoomState = mLiveState.Success;
                avatarUrl = mAvatar.Groups[1].Value;
            } catch(Exception ex) {
                // 到达这里说明网络有问题
                ShowMessage("发生了奇怪的错误, 可能b站改版导致获取信息错误, 请关注新版本", "未知错误");
                spbBottom.SetValueNow(0);
                ChangeState(0);
                Logger.Error("正则匹配失败导致获取房间信息错误", ex);
                return;
            }


            Logger.Log("房间ID=" + Global.RoomID);
            Logger.Log("房间URL=" + Global.RoomURL);
            Logger.Log("UP主=" + Global.RoomUpName);
            Logger.Log("标题=" + Global.RoomTitle);
            Logger.Log("头像地址=" + avatarUrl);
            Logger.Log("直播状态=" + (Global.RoomState?"直播中":"准备中"));

            spbBottom.SetValue(3);

            Global.SaveConfig("last_room", Global.RoomURL.ToString());
            int roomid = Global.RoomID.Value;
            var room=Global.DB.Builder("room").Find(roomid);
            bool downAvatar=false;
            if (room != null) {
                Global.DB.Update("UPDATE room SET url={1},name={2},title={3},avatar_url={4} WHERE id={0}",
                 new Object[] { roomid, Global.RoomURL, Global.RoomUpName, Global.RoomTitle, avatarUrl });
                if (!room["avatar_url"].Equals(avatarUrl)) {
                    downAvatar = true;
                }
            } else {
                Global.DB.Insert("INSERT INTO room(id,url,name,title,avatar_url) VALUES ({0},{1},{2},{3},{4})",
                new Object[] { roomid, Global.RoomURL, Global.RoomUpName, Global.RoomTitle, avatarUrl });
                downAvatar = true;
            }
            spbBottom.SetValue(4);
            
            var avatarPath = $@"avatars\{roomid}.jpg";
            if (!downAvatar && !File.Exists(avatarPath)) {
                downAvatar = true;
            }
            if (downAvatar) {
                http.DownloadFile(avatarUrl, avatarPath);
                Logger.Log("下载头像完毕");
            }
            spbBottom.SetValue(5);
            // 显示头像
            if (File.Exists(avatarPath)) {
                try {
                    this.Dispatcher.Invoke(new Action(() => {
                        image.Source = Utils.LoadImageFromFile(avatarPath);
                    }));
                } catch {
                    // 图像错了就不显示呗 干嘛报错 对不对
                }
                
            }
            // 显示姓名
            this.Dispatcher.Invoke(new Action(() => {
                tbUp.Text = "UP主: " + Global.RoomUpName;
            }));

            existUp = true;
            // FileHelper.SaveTxt("1.txt", s);
            Global.DmLoader.ConnectEvent += DmLoader_Connect;
            Global.DmLoader.DisconnectEvent += DmLoader_Disconnect;
            Global.DmLoader.Connect(roomid);
            spbBottom.SetValue(7);
        }
        private void DmLoader_Disconnect(object sender, ErrorArgs e) {
            Global.DmLoader.DisconnectEvent -= DmLoader_Disconnect;
            ShowMessage("连接到弹幕服务器发生错误\n原因: "+e.Error.Message, "连接错误");
            spbBottom.SetValueNow(0);
            ChangeState(0);
        }
        private void DmLoader_Connect(object sender, EventArgs e) {
            try {
                Logger.Log("连接成功");
                Global.DmLoader.ConnectEvent -= DmLoader_Connect;
                Global.DmLoader.DisconnectEvent -= DmLoader_Disconnect;
                spbBottom.SetValue(8);
                Thread.Sleep(1000);
                this.Dispatcher.Invoke(new Action(() => {
                    Global.isOfflineMode = false;
                    new MainWindow().Show();
                    this.Close();
                }));

            } catch(Exception ex) {
                ShowMessage(ex.Message,"错误");
                Logger.Error("连接错误", ex);
            }
            
        }

        private void btnOfflineMode_Click(object sender, System.Windows.RoutedEventArgs e) {
            string roomid_str = tbRoomID.Text;
            if (Regex.IsMatch(roomid_str, @"^\d+$")) {
                // 尝试获取房间信息
                var room = Global.DB.Builder("room").Where("id", roomid_str.ToInt32()).OrWhere("url", roomid_str.ToInt32()).First();
                if (room != null) {
                    Global.RoomURL = room["url"].ToInt32();
                    Global.RoomID = room["id"].ToInt32();
                    Global.RoomUpName = room["name"].ToString();
                    Global.RoomTitle = room["title"].ToString();
                    Global.RoomLevel = 0;
                    Global.RoomState = false;
                } else {
                    Global.RoomID = null;
                }
            }
            Global.isOfflineMode = true;
            new MainWindow().Show();
            this.Close();
        }
    }
}
