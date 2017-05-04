using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kxdanmuji_plugin_framework {
    public delegate void DisconnectEvt(object sender, ErrorArgs e);
    public delegate void ReceivedDanmakuEvt(object sender, DanmakuArgs e);
    public delegate void ConnectEvt(object sender, EventArgs e);

    public class ErrorArgs {
        public Exception Error;
    }
    public class DanmakuArgs {
        public DmModel Danmaku;
    }
    public static class DmUtils {
        public static Dictionary<int, string> GiftDict = new Dictionary<int, string>() {
            [1] = "辣条",
            [2] = "王♂者肥皂",
            [3] = "B坷垃",
            [4] = "喵娘",
            [5] = "锅",
            [6] = "亿圆",
            [7] = "666",
            [8] = "233",
            [9] = "爱心便当",
            [10] = "蓝白胖次",

            [13] = "FFF",

            [36] = "刨冰",
            [37] = "团扇",

            [39] = "节奏风暴",
        };
    }
}
