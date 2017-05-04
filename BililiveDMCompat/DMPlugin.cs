using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace BilibiliDM_PluginFramework {


    public class CompatDMPlugin : DispatcherObject, INotifyPropertyChanged {
        private bool _status = false;
        public event ReceivedDanmakuEvt ReceivedDanmaku;
        public event DisconnectEvt Disconnected;
        public event ReceivedRoomCountEvt ReceivedRoomCount;
        public event ConnectedEvt Connected;

        public void MainConnected(int roomid) {
            this.RoomID = roomid;
            Connected?.Invoke(null, new ConnectedEvtArgs() { roomid = roomid });
        }

        public void MainReceivedDanMaku(ReceivedDanmakuArgs e) {
            ReceivedDanmaku?.Invoke(null, e);
        }

        public void MainReceivedRoomCount(ReceivedRoomCountArgs e) {
            ReceivedRoomCount?.Invoke(null, e);
        }

        public void MainDisconnected() {
            this.RoomID = null;
            Disconnected?.Invoke(null, null);
        }

        /// <summary>
        /// 插件名稱
        /// </summary>
        public string PluginName { get; set; } = "這是插件";

        /// <summary>
        /// 插件作者
        /// </summary>
        public string PluginAuth { get; set; } = "這是作者";

        /// <summary>
        /// 插件作者聯繫方式
        /// </summary>
        public string PluginCont { get; set; } = "這是聯繫方式";

        /// <summary>
        /// 插件版本號
        /// </summary>
        public string PluginVer { get; set; } = "這是版本號";
        /// <summary>
        /// 插件描述
        /// </summary>
        public string PluginDesc { get; set; } = "描述還沒填";

        /// <summary>
        /// 插件描述, 已過期, 請使用PluginDesc
        /// </summary>
        [Obsolete("手滑產品, 請使用PluginDesc")]
        public string PlubinDesc {
            get { return this.PluginDesc; }
            set { this.PluginDesc = value; }
        }

        /// <summary>
        /// 插件狀態
        /// </summary>
        public bool Status {
            get { return _status; }
            private set {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        /// <summary>
        /// 當前連接中的房間
        /// </summary>
        public int? RoomId => RoomID;

        private int? RoomID;

        public CompatDMPlugin() {

        }
        /// <summary>
        /// 啟用插件方法 請重寫此方法
        /// </summary>
        public virtual void Start() {
            this.Status = true;
            Console.WriteLine(this.PluginName + " Start!");
        }
        /// <summary>
        /// 禁用插件方法 請重寫此方法
        /// </summary>
        public virtual void Stop() {

            this.Status = false;
            Console.WriteLine(this.PluginName + " Stop!");
        }
        /// <summary>
        /// 管理插件方法 請重寫此方法
        /// </summary>
        public virtual void Admin() {

        }
        /// <summary>
        /// 反初始化方法, 在弹幕姬主程序退出时调用, 若有需要请重写,
        /// </summary>
        public virtual void DeInit() {

        }
        /// <summary>
        /// 打日志
        /// </summary>
        /// <param name="text"></param>
        public void Log(string text) {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                //dynamic mw = Application.Current.MainWindow;
                //mw.logging(this.PluginName + " " + text);

            }));

        }

        private delegate void addMessageDelegate(string name, string msg);
        /// <summary>
        /// 打彈幕
        /// </summary>
        /// <param name="text"></param>
        /// <param name="fullscreen"></param>
        public void AddDM(string text, bool fullscreen = false) {
            Assembly assembly = Assembly.Load("kxdanmuji");
            if (assembly != null) {
                Type type = assembly.GetType("kxdanmuji.Global");
                if (type != null) {
                    var execAdd = (addMessageDelegate)Delegate.CreateDelegate(typeof(addMessageDelegate), type, "AddMessage");    //静态类方法
                    execAdd(this.PluginName, text);
                }
            }
            /*this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                dynamic mw = Application.Current.MainWindow;
                mw.AddDMText(this.PluginName, text, true, fullscreen);

            }));*/

        }
        /// <summary>
        /// 发送伪春菜脚本, 前提是用户有打开伪春菜并允许弹幕姬和伪春菜联动(默认允许)
        /// </summary>
        /// <param name="text">Sakura Script脚本</param>
        public void SendSSPMsg(string text) {
            this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => {
                //dynamic mw = Application.Current.MainWindow;
                //mw.SendSSP(text);

            }));

        }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }


}
