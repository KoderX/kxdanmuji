using KxLib.Utilities;
using LitJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using kxdanmuji_plugin_framework;
using System.Diagnostics;
using System.Threading.Tasks;

namespace kxdanmuji {
    /// <summary>
    /// 这个模块大多数取自于Bililive_dm的BiliDMLib的内容
    /// </summary>
    class DanmakuLoader {
        private Dictionary<int, string> typeDict;
        private string[] defaulthosts = new string[] { "livecmt-2.bilibili.com", "livecmt-1.bilibili.com" };
        private bool isConnected = false;
        private Task messageTask, heartbeatTask;
        private int roomID, lastRoomID;
        private TcpClient tcpClient;
        private string CIDInfoUrl = "http://live.bilibili.com/api/player?id=cid:";
        private string chatURL, lastChatURL;
        private int chatPort;
        private NetworkStream networkStream;
        private short protocolversion;
        private bool inDebug = false;
        private Exception Error;

        public event ConnectEvt ConnectEvent;
        public event DisconnectEvt DisconnectEvent;
        public event ReceivedDanmakuEvt ReceivedDanmakuEvent;
        public DanmakuLoader() {
            // 一些初始化
            lastRoomID = 0;
            chatPort = 788;
            protocolversion = 1;
            tcpClient = new TcpClient();
            typeDict = new Dictionary<int, string>() {
                [2] = "更新观众人数",
                [4] = "用户命令"
            };
            
        }

        public void Connect(int RoomID) {
            if (isConnected) {
                throw new InvalidOperationException("已经连接了");
            }
            this.roomID = RoomID;
            new Thread(Connect).Start();
        }
        private void Connect() {
            try {
                if (roomID != lastRoomID) {
                    HttpHelper http = new HttpHelper(Encoding.UTF8);
                    http.GET(CIDInfoUrl + roomID);
                    var xml = http.Response;
                    var serverRegex = Regex.Match(xml, @"<server>(.+?)</server>");
                    if (serverRegex.Success) {
                        chatURL = serverRegex.Groups[1].Value.ToString();
                    } else {
                        chatURL = defaulthosts[new Random().Next(defaulthosts.Length)];
                    }
                } else {
                    chatURL = lastChatURL;
                }
                tcpClient.Connect(chatURL, chatPort);
                Logger.Log("发送进入房间请求");
                networkStream = tcpClient.GetStream();
                // 发送进入房间请求
                Random r = new Random();
                var tmpuid = (long)(1e14 + 2e14 * r.NextDouble());
                var packetModel = new { roomid = roomID, uid = tmpuid };
                var playload = JsonMapper.ToJson(packetModel);
                SendSocketData(7, playload);
                // 开始连接
                isConnected = true;
                // 心跳线程
                Logger.Log("心跳线程开始");
                heartbeatTask = new Task(this.Heartbeat);
                heartbeatTask.Start();
                // 消息循环线程
                Logger.Log("消息循环线程开始");
                messageTask = new Task(this.ReceiveMessageLoop);
                messageTask.Start();
                // 保存上次记录
                lastChatURL = chatURL;
                lastRoomID = roomID;
            }catch (Exception ex) {
                isConnected = false;
                if (DisconnectEvent != null) {
                    DisconnectEvent(this, new ErrorArgs() { Error = ex });
                }
            }
            
        }
        private void Heartbeat() {
            while (isConnected) {
                SendSocketData(2);
                Thread.Sleep(30000);
            }
            Logger.Log("心跳线程结束");
        }
        private void ReceiveMessageLoop() {
            try {
                var stableBuffer = new byte[tcpClient.ReceiveBufferSize];
                if (ConnectEvent != null) {
                    ConnectEvent(this, new EventArgs());
                }
                while (this.isConnected) {

                    networkStream.ReadB(stableBuffer, 0, 4);
                    var packetlength = BitConverter.ToInt32(stableBuffer, 0);
                    packetlength = IPAddress.NetworkToHostOrder(packetlength);

                    if (packetlength < 16) {
                        throw new NotSupportedException("协议失败: (L:" + packetlength + ")");
                    }

                    networkStream.ReadB(stableBuffer, 0, 2);//magic
                    networkStream.ReadB(stableBuffer, 0, 2);//protocol_version 

                    networkStream.ReadB(stableBuffer, 0, 4);
                    var typeId = BitConverter.ToInt32(stableBuffer, 0);
                    typeId = IPAddress.NetworkToHostOrder(typeId);

                    networkStream.ReadB(stableBuffer, 0, 4);//magic, params?
                    var playloadlength = packetlength - 16;
                    if (playloadlength == 0) {
                        continue;//没有内容了

                    }
                    typeId = typeId - 1;//和反编译的代码对应 
                    if (typeDict.ContainsKey(typeId)) {
                        if (inDebug) {
                            Logger.Log("收到新信息 " + typeDict[typeId] + "(" + typeId + ")");
                        }
                    } else {
                        Logger.Log("收到未知新信息 编号: " + typeId);
                    }
                    
                    var buffer = new byte[playloadlength];
                    networkStream.ReadB(buffer, 0, playloadlength);
                    switch (typeId) {
                        case 0:
                        case 1:
                        case 2:
                            {
                                var viewer = BitConverter.ToUInt32(buffer.Take(4).Reverse().ToArray(), 0); //观众人数
                                //Logger.Info("观众人数: " + viewer);
                                if (ReceivedDanmakuEvent != null) {
                                    DmModel dm = new DmModel() {
                                        Time = DateTime.Now,
                                        Type = DmType.UPDATE_VIEWER,
                                        Raw = "",
                                        ViewerCount = viewer
                                    };
                                    ReceivedDanmakuEvent(this, new DanmakuArgs() { Danmaku = dm });
                                }
                                break;
                            }
                        case 3:
                        case 4://playerCommand
                            {

                                var json = Encoding.UTF8.GetString(buffer, 0, playloadlength);
                                // 解析弹幕
                                ParseDanmaku(json);
                                break;
                            }
                        case 5://newScrollMessage
                            {

                                break;
                            }
                        case 7:
                            {

                                break;
                            }
                        case 16:
                            {
                                break;
                            }
                        default:
                            {

                                break;
                            }
                            //                     
                    }
                }
            } catch (NotSupportedException ex) {
                this.Error = ex;
                _disconnect();
            } catch (Exception ex) {
                this.Error = ex;
                _disconnect();

            }
            Logger.Log("消息循环线程结束");
        }

        private void ParseDanmaku(string json) {
            if (inDebug) {
                Logger.Log(json);
            }
            try {
                DmModel dm = new DmModel() {
                    Time = DateTime.Now,
                    Type = DmType.UNKNOWN,
                    Raw = json
                };
                JsonData jsonData = JsonMapper.ToObject(json);
                if (jsonData["cmd"].Equals("DANMU_MSG")) {
                    dm.Type = DmType.DANMU_MSG;
                    var msg = new DmMessage();
                    msg.Content = jsonData["info"][1].ToString();
                    if (jsonData["info"][3].IsArray && jsonData["info"][3].Count > 2) {
                        msg.HaveMedal = true;
                        msg.MedalLevel = jsonData["info"][3][0].ToInt32();
                        msg.MedalTitle = jsonData["info"][3][1].ToString();
                        msg.MedalUpName = jsonData["info"][3][2].ToString();
                        msg.MedalUrlID = jsonData["info"][3][3].ToInt32();
                    } else {
                        msg.HaveMedal = false;
                    }
                    msg.UserID = jsonData["info"][2][0].ToInt32();
                    msg.UserName = jsonData["info"][2][1].ToString();
                    msg.IsAdmin = jsonData["info"][2][2].ToString().Equals("1");
                    msg.IsSVIP = jsonData["info"][2][3].ToString().Equals("1");
                    msg.IsVIP = jsonData["info"][2][4].ToString().Equals("1");
                    msg.UserLevel = jsonData["info"][4][0].ToInt32();
                    var rank = jsonData["info"][4][1].ToString();
                    if (rank.StartsWith(">")) {
                        msg.UserRank = 1000000;
                    } else {
                        if (Regex.IsMatch(rank, @"^\d+$")) {
                            msg.UserRank = rank.ToInt32();
                        } else {
                            msg.UserRank = 0;
                            Logger.Log("未知排名数据: " + rank);
                        }
                    }
                    if (jsonData["info"][5].IsArray && jsonData["info"][5].Count > 0) {
                        msg.UserTitleKey = jsonData["info"][5][0].ToString();
                    } else {
                        msg.UserTitleKey = "";
                    }
                    dm.Message = msg;
                } else if (jsonData["cmd"].Equals("SEND_GIFT")) {
                    dm.Type = DmType.SEND_GIFT;
                    var gift = new DmGift();
                    var d = jsonData["data"];
                    gift.Action = d["action"].ToString();
                    gift.ID = d["giftId"].ToInt32();
                    gift.Name = d["giftName"].ToString();
                    gift.Num = d["num"].ToInt32();
                    gift.Price = d["price"].ToInt32();
                    if (d.Keys.Contains("rcost")) {
                        gift.RCost = d["rcost"].ToInt32();
                    } else {
                        gift.RCost = -1;
                    }

                    gift.Timestamp = d["timestamp"].ToInt64();
                    if (d.Keys.Contains("giftType")) {
                        gift.Type = d["giftType"].ToInt32();
                    } else {
                        gift.Type = -1;
                    }
                    gift.UserID = d["uid"].ToInt32();
                    gift.UserName = d["uname"].ToString();
                    dm.Gift = gift;
                } else if (jsonData["cmd"].Equals("WELCOME")) {
                    dm.Type = DmType.WELCOME;
                    var msg = new DmMessage();
                    msg.UserID = jsonData["data"]["uid"].ToInt32();
                    msg.UserName = jsonData["data"]["uname"].ToString();
                    msg.Content = msg.UserName + " 老爷 进入直播间";
                    msg.IsAdmin = jsonData["data"]["isadmin"].ToString().Equals("1");
                    if (jsonData["data"].Keys.Contains("svip")) {
                        msg.IsSVIP = jsonData["data"]["svip"].ToString().Equals("1");
                    }
                    if (jsonData["data"].Keys.Contains("vip")) {
                        msg.IsVIP = jsonData["data"]["vip"].ToString().Equals("1");
                    }
                    dm.Message = msg;
                } else if (jsonData["cmd"].Equals("SYS_MSG")) {
                    dm.Type = DmType.SYS_MSG;
                    var msg = new DmSysMsg();
                    msg.Message = jsonData["msg"].ToString();
                    msg.Repeat = jsonData["rep"].ToInt32();
                    if (jsonData.Keys.Contains("styleType")) {
                        msg.StyleType = jsonData["styleType"].ToInt32();
                    }
                    msg.Url = jsonData["url"].ToString();
                    dm.SysMessage = msg;

                } else if (jsonData["cmd"].Equals("SYS_GIFT")) {
                    dm.Type = DmType.SYS_GIFT;
                    var gift = new DmSysGift();
                    if (jsonData.Keys.Contains("msgTips")) {
                        gift.Message = jsonData["tips"].ToString();
                        gift.MessageRaw = jsonData["msg"].ToString();
                        gift.Repeat = jsonData["rep"].ToInt32();
                        gift.Url = jsonData["url"].ToString();
                        gift.RoomID = jsonData["roomid"].ToInt32();
                    } else {
                        gift.MessageRaw = jsonData["msg"].ToString();
                        var list = Regex.Split(gift.MessageRaw, ":?");
                        // 夏子耐耐酱:? 在小绝的:?直播间5367:?内赠送:?36:?共100个，触发1次刨冰雨抽奖，快去前往抽奖吧！
                        // DHVa:?  在杆菌无敌的:?直播间26057:?内赠送:?1:?共4500个
                        gift.RoomID = Regex.Match(list[2],"[0-9]+").Value.ToInt32();
                        var giftid = list[4].ToInt32();
                        string giftname = "未知礼物";
                        if (DmUtils.GiftDict.ContainsKey(giftid)) {
                            giftname = DmUtils.GiftDict[giftid];
                        }
                        gift.Message = $"【{list[0]}】{list[1]}直播间【{gift.RoomID}】内 赠送 {giftname}{list[5]}";
                    }
                    gift.Random = jsonData["rnd"].ToInt32();


                    dm.SysGift = gift;
                } else if (jsonData["cmd"].Equals("LIVE")) {
                    dm.Type = DmType.LIVE;
                } else {
                    Logger.Log("未知指令 " + jsonData["cmd"]);
                    if (!inDebug) {
                        Logger.Log(json);
                    }
                }


                if (ReceivedDanmakuEvent != null) {
                    ReceivedDanmakuEvent(this, new DanmakuArgs() { Danmaku = dm });
                }
            } catch(Exception ex) {
                if (!inDebug) {
                    // 上面输出过了就不用输出2次了= =
                    Logger.Error("解析错误 JSON 如下: ");
                    Logger.Error(json);
                }
                Logger.Error("异常:",ex);
            }
            
        }

        public void Disconnect() {
            Logger.Log("断开连接");
            if (!isConnected) {
                return;
            }
            isConnected = false;
            try {
                tcpClient.Close();
            } catch (Exception) {

            }
            networkStream = null;
        }

        private void _disconnect() {
            if (isConnected) {
                Logger.Error("异常断开连接", Error);

                isConnected = false;

                tcpClient.Close();

                networkStream = null;
                if (DisconnectEvent != null) {
                    DisconnectEvent(this, new ErrorArgs() { Error = Error });
                }
            }

        }
        void SendSocketData(int action, string body = "") {
            SendSocketData(0, 16, protocolversion, action, 1, body);
        }
        void SendSocketData(int packetlength, short magic, short ver, int action, int param = 1, string body = "") {
            var playload = Encoding.UTF8.GetBytes(body);
            if (packetlength == 0) {
                packetlength = playload.Length + 16;
            }
            var buffer = new byte[packetlength];
            using (var ms = new MemoryStream(buffer)) {


                var b = BitConverter.GetBytes(buffer.Length).ToBE();

                ms.Write(b, 0, 4);
                b = BitConverter.GetBytes(magic).ToBE();
                ms.Write(b, 0, 2);
                b = BitConverter.GetBytes(ver).ToBE();
                ms.Write(b, 0, 2);
                b = BitConverter.GetBytes(action).ToBE();
                ms.Write(b, 0, 4);
                b = BitConverter.GetBytes(param).ToBE();
                ms.Write(b, 0, 4);
                if (playload.Length > 0) {
                    ms.Write(playload, 0, playload.Length);
                }
                networkStream.Write(buffer, 0, buffer.Length);
                networkStream.Flush();
            }
        }
    }
}
