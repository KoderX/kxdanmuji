using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kxdanmuji_plugin_framework {
    /// <summary>
    /// 弹幕类型
    /// </summary>
    public enum DmType {
        UNKNOWN, SYS_MSG, DANMU_MSG, SEND_GIFT, WELCOME, COMMAND, SYS_GIFT, LIVE,UPDATE_VIEWER
    }
    /// <summary>
    /// 弹幕文本信息 DANMU_MSG,WELCOME配套使用
    /// </summary>
    public class DmMessage {
        //// <summary>
        /// 用户ID
        /// </summary>
        public int UserID { get; set; } = 0;
        /// <summary>
        /// 用户昵称
        /// </summary>
        public string UserName { get; set; } = "";
        /// <summary>
        /// 弹幕内容
        /// </summary>
        public string Content { get; set; } = "";
        /// <summary>
        /// 是否管理员
        /// </summary>
        public bool IsAdmin { get; set; } = false;
        /// <summary>
        /// 是否会员老爷
        /// </summary>
        public bool IsVIP { get; set; } = false;
        /// <summary>
        /// 是否年费会员老爷
        /// </summary>
        public bool IsSVIP { get; set; } = false;
        /// <summary>
        /// 是否有勋章
        /// </summary>
        public bool HaveMedal { get; set; } = false;
        /// <summary>
        /// 勋章等级
        /// </summary>
        public int MedalLevel { get; set; } = 0;
        /// <summary>
        /// 勋章名称
        /// </summary>
        public string MedalTitle { get; set; } = "";
        /// <summary>
        /// 勋章UP主姓名
        /// </summary>
        public string MedalUpName { get; set; } = "";
        /// <summary>
        /// 勋章UP主房间URLID
        /// </summary>
        public int MedalUrlID { get; set; } = 0;
        /// <summary>
        /// 用户等级
        /// </summary>
        public int UserLevel { get; set; } = 0;
        /// <summary>
        /// 用户等级排名
        /// </summary>
        public int UserRank { get; set; } = 0;
        /// <summary>
        /// 用户头衔标识
        /// </summary>
        public string UserTitleKey { get; set; }
    }
    /// <summary>
    /// 广播消息 SYS_MSG配套使用
    /// </summary>
    public class DmSysMsg {
        /// <summary>
        /// 消息体 注意:SYS_GIFT最好直接使用Tips不要用这个
        /// </summary>
        public string Message { get; set; } = "";
        //// <summary>
        /// 重复广播次数
        /// </summary>
        public int Repeat { get; set; } = 0;
        /// <summary>
        /// 触发的地址
        /// </summary>
        public string Url { get; set; } = "";
        /// <summary>
        /// 样式类型
        /// </summary>
        public int StyleType { get; set; } = 0;
    }
    /// <summary>
    /// 广播礼物 SYS_GIFT配套使用
    /// </summary>
    public class DmSysGift {
        /// <summary>
        /// 处理后的消息体
        /// </summary>
        public string Message { get; set; } = "";
        //// <summary>
        /// 重复广播次数
        /// </summary>
        public int Repeat { get; set; } = 0;
        /// <summary>
        /// 触发的地址
        /// </summary>
        public string Url { get; set; } = "";
        /// <summary>
        /// 原始信息
        /// </summary>
        public string MessageRaw { get; set; } = "";
        /// <summary>
        /// 房间号
        /// </summary>
        public int RoomID { get; set; } = 0;
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserID { get; set; } = 0;
        /// <summary>
        /// 随机数字
        /// </summary>
        public int Random { get; set; } = 0;
    }
    /// <summary>
    /// 礼物信息 SEND_GIFT配套使用
    /// </summary>
    public class DmGift {
        /// <summary>
        /// 礼物名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 送出礼物的动作
        /// </summary>
        public string Action { get; set; } = "";
        /// <summary>
        /// 礼物数量
        /// </summary>
        public int Num { get; set; } = 0;
        /// <summary>
        /// 用户昵称
        /// </summary>
        public string UserName { get; set; } = "";
        /// <summary>
        /// 直播积分总计 未获取到返回-1 注意是long
        /// </summary>
        public long RCost { get; set; } = 0;
        /// <summary>
        /// 用户ID
        /// </summary>
        public int UserID { get; set; } = 0;
        /// <summary>
        /// 时间戳 注意是long
        /// </summary>
        public long Timestamp { get; set; } = 0;
        /// <summary>
        /// 礼物ID
        /// </summary>
        public int ID { get; set; } = 0;
        /// <summary>
        /// 礼物类型 未获取到返回-1
        /// </summary>
        public int Type { get; set; } = 0;
        /// <summary>
        /// 礼物单价
        /// </summary>
        public int Price { get; set; } = 0;
    }
    /// <summary>
    /// 弹幕模型类
    /// </summary>
    public class DmModel {
        /// <summary>
        /// 弹幕类型
        /// </summary>
        public DmType Type { get; set; }
        /// <summary>
        /// JSON元数据
        /// </summary>
        public string Raw { get; set; } = "";
        /// <summary>
        /// 收到弹幕时间
        /// </summary>
        public DateTime Time { get; set; }
        public DmMessage Message { get; set; }
        public DmGift Gift { get; set; }
        public DmSysMsg SysMessage { get; set; }
        public DmSysGift SysGift { get; set; }
        /// <summary>
        /// 观众人数 UPDATE_VIEWER配套使用
        /// </summary>
        public uint ViewerCount { get; set; }
    }
}
