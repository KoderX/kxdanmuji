using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;

namespace kxdanmuji {
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application {
        App() {
            // 初始化目录
            if (!Directory.Exists(@"avatars")) {
                Directory.CreateDirectory(@"avatars");
            }
            if (!Directory.Exists(@"plugins")) {
                Directory.CreateDirectory(@"plugins");
            }
            if (!Directory.Exists(@"logs")) {
                Directory.CreateDirectory(@"logs");
            }
            // 初始化公共类
            Global.init();
        }
    }
}
