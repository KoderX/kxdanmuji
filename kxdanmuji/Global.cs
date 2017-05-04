using kxdanmuji_plugin_framework;
using KxLib.Utilities;
using KxLib.Database;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Resources;

namespace kxdanmuji {
    static class Global {
        private static SQLite db;
        private static Dictionary<string, string> config;
        public static SQLite DB {
            get { return db; }
        }
        public static int? RoomID;
        public static int RoomURL;
        public static int RoomLevel;
        public static string RoomUpName;
        public static string RoomTitle;
        public static uint RoomViewer;
        public static bool RoomState;
        public static bool isOfflineMode;
        public static DanmakuLoader DmLoader=new DanmakuLoader();
        public static List<string> randomText;
        public static ObservableCollection<DmPlugin> pluginList = new ObservableCollection<DmPlugin>();
        public static Page PluginPage;
        public static MainWindow mainWindow;

        public static void init() {
            // 初始化数据库
            db = new SQLite("data.db");
            db.Transaction(new Action(() => {
                db.Statement("CREATE TABLE IF NOT EXISTS \"config\"(\"key\" TEXT NOT NULL,\"value\" TEXT,PRIMARY KEY (\"key\"));");
                db.Statement("CREATE TABLE IF NOT EXISTS \"room\" (\"id\" INTEGER NOT NULL,\"url\"  INTEGER NOT NULL,\"name\" TEXT NOT NULL,\"title\"  TEXT,\"avatar_url\"  TEXT,PRIMARY KEY (\"id\"));");
                db.Statement(@"CREATE TABLE IF NOT EXISTS ""danmaku"" (
""id"" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
""room_id"" INTEGER NOT NULL,
""user_id"" INTEGER NOT NULL,
""user_name"" TEXT NOT NULL,
""message"" TEXT NOT NULL,
""gift_name"" TEXT,
""gift_num"" INTEGER,
""type"" INTEGER NOT NULL,
""time"" INTEGER NOT NULL,
""raw"" TEXT);
");
            }));
            LoadAllConfig();
            // 初始化Logger
            Logger.Init($@"logs\{DateTime.Now.ToString("yyyy-MM-dd")}.log");
            Logger.Info("应用启动");
            var u = new Uri("pack://application:,,,/Resources/randomtext.txt");
            StreamResourceInfo info = Application.GetResourceStream(u);
            StreamReader sr = new StreamReader(info.Stream);
            var s = sr.ReadToEnd();
            var sa = s.Split('\n');
            randomText = new List<string>(sa);
        }
        #region 插件系统
        public static void AddDmPlugin(DmPlugin dm) {
            PluginPage.Dispatcher.Invoke(new Action(() => {
                pluginList.Add(dm);
            }));
        }
        public static void RemoveDmPlugin(string unique) {
            PluginPage.Dispatcher.Invoke(new Action(() => {
                pluginList.Remove(pluginList.Where(o => o.Information.Unique == unique && o.Information.IsCompat==true).Single());
            }));
        }
        public static void AddMessage(string name,string msg) {
            mainWindow.AddPluginMessage(name, msg);
        }
        public static void PluginLog(string msg) {
            Logger.Log(msg);
        }
        public static void CompatPluginStateChange(bool state) {
            foreach (var plugin in pluginList) {
                if (plugin.Information.IsCompat) {
                    plugin.State = state;
                }
            }
        }
        #endregion
        private static void LoadAllConfig() {
            config = new Dictionary<string, string>();
            var configResult = db.Builder("config").All();
            foreach(var r in configResult) {
                config.Add(r["key"], r["value"]);
            }
        }
        public static string LoadConfig(string key) {
            if (config.ContainsKey(key)) {
                return config[key];
            }
            return null;
        }
        public static void SaveConfig(string key,string value) {
            if (config.ContainsKey(key)) {
                db.Update("UPDATE config SET value={0} WHERE key={1}", new Object[] { value, key });
                config[key] = value;
            } else {
                db.Insert("INSERT INTO config(key,value) VALUES ({0},{1})", new Object[] { key, value });
                config.Add(key, value);
            }
        }
    }
}
