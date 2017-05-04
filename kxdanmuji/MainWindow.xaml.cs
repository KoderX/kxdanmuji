using kxdanmuji.Controls;
using KxLib.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using kxdanmuji_plugin_framework;
using kxdanmuji.Pages;
using System.Windows.Controls;
using System.Reflection;
using System.Threading.Tasks;

namespace kxdanmuji {
    /// <summary>
    /// 主界面窗口
    /// </summary>
    public partial class MainWindow : KxWindow {
        private const string ViewerFormat = @"当前观众: {0}人";
        private const string RecDMFormat = "收到弹幕: {0} 参与用户: {1}";
        private HashSet<int> validUserIDList = new HashSet<int>();
        private int totalDanmaku = 0;
        private DanmakuWindow danmakuWindow;
        private DanmakuListPage danmakuListPage;
        private PluginPage pluginPage;
        private SettingPage settingPage;


        public MainWindow() {
            // 修正最大化问题
            WindowHelper.RepairWindowBehavior(this, 8,600,400);
            InitializeComponent();

            // 加载Page
            danmakuListPage = new DanmakuListPage(this);
            danmakuListPage.Resources = this.Resources;
            pluginPage = new PluginPage(this);
            pluginPage.Resources = this.Resources;
            settingPage = new SettingPage(this);
            settingPage.Resources = this.Resources;

            Global.mainWindow = this;

            frmPage.Content = danmakuListPage;
            frmSetting.Content = settingPage;

            // 加载皮肤
            string skinStr = Global.LoadConfig("skin");
            if (skinStr != null) {
                if (skinStr.Equals("Dark")) this.SkinType = SkinTypeEnum.Dark;
                else if (skinStr.Equals("Blue")) this.SkinType = SkinTypeEnum.Blue;
            }
        }
        public override void SkinButton_Click(object sender, RoutedEventArgs e) {
            if (this.SkinType == SkinTypeEnum.Dark) {
                this.SkinType = SkinTypeEnum.Blue;
            } else if (this.SkinType == SkinTypeEnum.Blue) {
                this.SkinType = SkinTypeEnum.Light;
            } else {
                this.SkinType = SkinTypeEnum.Dark;
            }
            Global.SaveConfig("skin", this.SkinType.ToString());

        }

        public void HideSetting() {
            frmSetting.Visibility = Visibility.Collapsed;
        }

        private void KxWindow_Closed(object sender, System.EventArgs e) {
            danmakuWindow.Close();

            if (!Global.isOfflineMode) {
                Global.DmLoader.Disconnect();
                Global.DmLoader.ReceivedDanmakuEvent -= ReceivedDanmaku;
            }
        }

        private void SetLiveOn(bool on) {
            this.Dispatcher.Invoke(new Action(() => {
                //Global.RoomState
                if (on) {
                    tbLiveState.Text = "直播中";
                    tbLiveState.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fffd9ccc"));
                } else {
                    tbLiveState.Text = "准备中";
                    tbLiveState.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#cc999999"));
                }
            }));
        }

        private void KxWindow_Loaded(object sender, RoutedEventArgs e) {
            var assembly = Assembly.GetExecutingAssembly();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var dt = new DateTime(2000, 1, 1);
            dt = dt.AddDays(version.Build);
            dt = dt.AddSeconds(version.Revision);
            this.Title += $"{version.Major}.{version.Minor}.{dt.ToLocalTime().ToString("yyMMdd")}";
            if (Global.isOfflineMode) {
                this.Title += " - 离线模式";
            }
            if (Global.RoomID==null) {
                if (Global.isOfflineMode) {
                    Global.RoomTitle = "欢迎使用弹幕姬";
                    Global.RoomViewer = 0;
                    Global.RoomUpName = "喵喵喵 ฅ՞•ﻌ•՞ฅ";
                    Global.RoomLevel = 0;
                } else {
                    Global.RoomID = 55054;
                    Global.RoomTitle = "不知道你咋进来的, 反正是出错了~";
                    Global.RoomViewer = 666;
                    Global.RoomUpName = "KoderX";
                    Global.RoomLevel = 30;
                }
            }
            if(Global.RoomID != null) {
                var avatarPath = $@"avatars\{Global.RoomID}.jpg";
                if (File.Exists(avatarPath)) {
                    imgAvatar.Source = Utils.LoadImageFromFile(avatarPath);
                }
            }
            
            tbTitle.Text = Global.RoomTitle;
            tbUpName.Text = Global.RoomUpName;
            tbViewer.Text = String.Format(ViewerFormat, Global.RoomViewer);
            UpdateStatistics(0, 0);
            SetLiveOn(Global.RoomState);
            if (Global.RoomLevel > 0 && Global.RoomLevel <= 50) {
                try {
                    // 定义切割图片
                    var cut = new Int32Rect(0, Global.RoomLevel * 17 - 17, 39, 17);
                    var uri = new Uri("pack://application:,,,/Images/level-up.png", UriKind.RelativeOrAbsolute);
                    imgLevel.Source = new CroppedBitmap(BitmapFrame.Create(uri), cut);
                } catch (Exception ex) {
                    imgLevel.Visibility = Visibility.Collapsed;
                    Logger.Error("切割图片错误", ex);
                }
            } else {
                imgLevel.Visibility = Visibility.Collapsed;
            }
            // 右侧弹幕条
            danmakuWindow = new DanmakuWindow();
            danmakuWindow.Show();

            if (!Global.isOfflineMode) {
                Global.DmLoader.ReceivedDanmakuEvent += ReceivedDanmaku;
            }
        }
        
        private void UpdateStatistics(int total, int validTotal) {
            this.Dispatcher.Invoke(new Action(() => {
                tbRcvDM.Text = String.Format(RecDMFormat, total, validTotal);
            }));
        }

        private string SolveTime(DateTime dt) {
            return dt.ToLocalTime().ToLongTimeString();
        }
        private void ReceivedDanmaku(object sender, DanmakuArgs e) {
            var dmModel = e.Danmaku;
            foreach (var item in Global.pluginList) {
                if (item.State == false) {
                    continue;
                }
                if (item.Information.IsSync) {
                    dmModel = item.ReceiveDanmakuSync(dmModel);
                } else {
                    new Task(()=> { item.ReceiveDanmaku(dmModel); }).Start();
                }
            }
            var ts = Convert.ToInt64((dmModel.Time - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
            Danmaku dm = new Danmaku();
            switch (dmModel.Type) {
                case DmType.UNKNOWN:
                    break;
                case DmType.UPDATE_VIEWER:
                    this.Dispatcher.Invoke(new Action(() => {
                        tbViewer.Text = String.Format(ViewerFormat, dmModel.ViewerCount);
                    }));
                    break;
                case DmType.SYS_MSG:
                    var sysmsg = dmModel.SysMessage;
                    dm.Time = SolveTime(dmModel.Time);
                    dm.Type = "系统";
                    dm.Str1 = " ";
                    dm.Str1Color = "#000";
                    dm.Str2 = sysmsg.Message;
                    if (true) {
                        Global.DB.Insert("INSERT INTO danmaku(room_id,user_id,user_name,message,gift_name,gift_num,type,time,raw) VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8})",
                        new Object[] { 0,0,"", sysmsg.Message, "",0,0, ts,dmModel.Raw });
                    }
                    break;
                case DmType.DANMU_MSG:
                    var msg = dmModel.Message;
                    dm.Time = SolveTime(dmModel.Time);
                    dm.Type = "弹幕";
                    dm.Str1 = msg.UserName + "：";
                    dm.Str1Color = "#4fc1e9";
                    dm.Str2 = msg.Content;
                    // 更新统计
                    if (!validUserIDList.Contains(msg.UserID)) {
                        validUserIDList.Add(msg.UserID);
                    }
                    if (msg.UserID > 0) {
                        UpdateStatistics(++totalDanmaku, validUserIDList.Count);
                    }
                    if (Global.RoomID.HasValue) {
                        Global.DB.Insert("INSERT INTO danmaku(room_id,user_id,user_name,message,gift_name,gift_num,type,time,raw) VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8})",
                        new Object[] { Global.RoomID, msg.UserID, msg.UserName, msg.Content, "", 0, 1, ts, dmModel.Raw });
                    }
                    break;
                case DmType.SEND_GIFT:
                    var gift = dmModel.Gift;
                    dm.Time = SolveTime(dmModel.Time);
                    dm.Type = "礼物";
                    dm.Str1 = gift.UserName + " ";
                    dm.Str1Color = "#ff8f34";
                    dm.Str2 = gift.Action + gift.Name + "×" + gift.Num;
                    if (Global.RoomID.HasValue) {
                        Global.DB.Insert("INSERT INTO danmaku(room_id,user_id,user_name,message,gift_name,gift_num,type,time,raw) VALUES ({0},{1},{2},{3},{4},{5},{6},{7},{8})",
                        new Object[] { Global.RoomID, gift.UserID, gift.UserName, dm.Str2, gift.Name, gift.Num, 2, ts, dmModel.Raw });
                    }
                    break;
                default:
                    break;
            }
            if (dm.Time != null) {
                this.Dispatcher.Invoke(new Action(() => {
                    // 中间列表弹幕
                    danmakuListPage.AddDanmaku(dm);
                    // 侧边弹幕
                    if(danmakuWindow.Visibility== Visibility.Visible) {
                        danmakuWindow.SendDanmaku(dm);
                    }
                }));
            }

        }
        private void btnSetting_Click(object sender, RoutedEventArgs e) {
            frmSetting.Content = settingPage;
            frmSetting.Visibility = Visibility.Visible;
        }
        private void btnTest_Click(object sender, RoutedEventArgs e) {
            DmModel dm;
            if (new Random().Next(5) == 3) {
                dm = new DmModel() {
                    Type = DmType.SEND_GIFT,
                    Time = DateTime.Now,
                    Gift = new DmGift() {
                        UserName = "喵",
                        UserID = -1,
                        Action = "赠送",
                        Name = "亿元",
                        RCost=1000,
                        Num = new Random().Next(1, 10)
                    }
                };
            } else {
                dm = new DmModel() {
                    Type = DmType.DANMU_MSG,
                    Time = DateTime.Now,
                    Message = new DmMessage() {
                        UserName = "喵",
                        UserID = -1,
                        Content = Global.randomText[new Random().Next(Global.randomText.Count)]
                    }
                };
            }
            ReceivedDanmaku(null, new DanmakuArgs() { Danmaku = dm });
        }
        private void SendSystemMessage(string text) {
            var dm = new DmModel() {
                Type = DmType.SYS_MSG,
                Time = DateTime.Now,
                SysMessage = new DmSysMsg() {
                    Message = text,
                    Repeat = 1
                }
            };
            ReceivedDanmaku(null, new DanmakuArgs() { Danmaku = dm });
        }
        public void AddPluginMessage(string name,string msg) {
            Danmaku dm = new Danmaku() {
                Time=SolveTime(DateTime.Now),
                Type = "插件",
                Str1 = String.IsNullOrEmpty(name) ? "" : name + "：",
                Str1Color = "#9999",
                Str2 = msg
            };
            this.Dispatcher.Invoke(new Action(() => {
                // 中间列表弹幕
                danmakuListPage.AddDanmaku(dm);
                // 侧边弹幕
                if (danmakuWindow.Visibility == Visibility.Visible) {
                    danmakuWindow.SendDanmaku(dm);
                }
            }));
        }
        private void btnDanmakuShow_Click(object sender, RoutedEventArgs e) {
            var state = btnDanmakuShow.ToolTip.Equals("隐藏弹幕侧边框");
            if (state) {
                danmakuWindow.Hide();
                btnDanmakuShow.SetResourceReference(Button.StyleProperty, "ToolbarButton");
                btnDanmakuShow.ToolTip = "显示弹幕侧边框";
            } else {
                danmakuWindow.Show();
                btnDanmakuShow.SetResourceReference(Button.StyleProperty, "ToolbarButtonSelect");
                btnDanmakuShow.ToolTip = "隐藏弹幕侧边框";
            }
            SendSystemMessage("弹幕侧边框已" + (state ? "隐藏" : "显示"));
        }
        private void btnPlugin_Click(object sender, RoutedEventArgs e) {
            if (btnPlugin.ToolTip.Equals("隐藏插件列表")) {
                frmPage.Content = danmakuListPage;
                btnPlugin.SetResourceReference(Button.StyleProperty, "ToolbarButton");
                btnPlugin.ToolTip = "显示插件列表";
            } else {
                frmPage.Content = pluginPage;
                btnPlugin.SetResourceReference(Button.StyleProperty, "ToolbarButtonSelect");
                btnPlugin.ToolTip = "隐藏插件列表";
            }
        }

        private void btnTest_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            return;
            DmModel dm;
            if (new Random().Next(5) == 3) {
                dm = new DmModel() {
                    Type = DmType.SEND_GIFT,
                    Time = DateTime.Now,
                    Gift = new DmGift() {
                        UserName = "喵",
                        UserID = -1,
                        Action = "赠送",
                        Name = "亿元",
                        RCost = 1000,
                        Num = new Random().Next(1, 10)
                    }
                };
            } else {
                dm = new DmModel() {
                    Type = DmType.DANMU_MSG,
                    Time = DateTime.Now,
                    Message = new DmMessage() {
                        UserName = "喵",
                        UserID = -1,
                        Content = Global.randomText[new Random().Next(Global.randomText.Count)]
                    }
                };
            }
            ReceivedDanmaku(null, new DanmakuArgs() { Danmaku = dm });
        }
    }
}
