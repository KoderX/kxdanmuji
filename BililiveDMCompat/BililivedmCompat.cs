using BilibiliDM_PluginFramework;
using kxdanmuji_plugin_framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;

namespace BililivedmCompat {
    public class BililivedmCompat : DmPlugin {
        private readonly ObservableCollection<DMPlugin> Plugins = new ObservableCollection<DMPlugin>();
        private delegate void addDelegate(DmPlugin dm);
        public BililivedmCompat() {
            this.Information.Unique = this.Information.MakeUnique(this.GetType().Name);
            this.Information.Name = "兼容插件";
            this.Information.Version = "1.0";
            this.Information.Description = "老版本弹幕姬兼容插件";
            this.Information.Author = "KoderX";
            this.Information.Contact = "koderx@qq.com";
        }
        public override void ReceiveDanmaku(DmModel dm) {

            var e = new ReceivedDanmakuArgs();
            bool send = false;
            if (!String.IsNullOrEmpty(dm.Raw)) {
                e.Danmaku = new DanmakuModel(dm.Raw, 2);
                //Log("Raw存在 进行二次解析");
                send = true;
            } else {
                e.Danmaku = new DanmakuModel() {
                    RawData = dm.Raw,
                    JSON_Version = 2
                };
                if (dm.Type == DmType.COMMAND) {
                    if (dm.Message.UserID == 1) {
                        foreach (var item in Plugins) {
                            try {
                                if("compat_" + this.Information.MakeUnique(item.GetType().Name) == dm.Message.UserName) {
                                    item.Admin();
                                    break;
                                }
                            } catch (Exception ex) {
                                Log($"ReceiveDanmaku报错 插件:{item.PluginName}\n{ex.ToString()}");
                            }
                        }
                    }
                }else if (dm.Type == DmType.DANMU_MSG) {
                    e.Danmaku.CommentText = dm.Message.Content;
                    e.Danmaku.CommentUser = dm.Message.UserName;
                    e.Danmaku.isAdmin = dm.Message.IsAdmin;
                    e.Danmaku.isVIP = dm.Message.IsVIP || dm.Message.IsSVIP;
                    e.Danmaku.MsgType = MsgTypeEnum.Comment;
                    send = true;
                } else if (dm.Type == DmType.SEND_GIFT) {
                    e.Danmaku.MsgType = MsgTypeEnum.GiftSend;
                    e.Danmaku.GiftName = dm.Gift.Name;
                    e.Danmaku.GiftUser = dm.Gift.UserName;
                    e.Danmaku.Giftrcost = dm.Gift.RCost.ToString();
                    e.Danmaku.GiftNum = dm.Gift.Num.ToString();
                    send = true;
                }
            }

            if (send) {
                foreach (var item in Plugins) {
                    try {
                        item.MainReceivedDanMaku(e);
                    } catch (Exception ex) {
                        Log($"ReceiveDanmaku报错 插件:{item.PluginName}\n{ex.ToString()}");
                    }
                }
            }
        }
        public override void StateChange() {
            foreach (var plugin in Plugins) {
                if (this.State) {
                    plugin.Start();
                } else {
                    plugin.Stop();
                }
            }
            Assembly assembly = Assembly.Load("kxdanmuji");
            if (assembly != null) {
                Type type = assembly.GetType("kxdanmuji.Global");
                if (type != null) {
                    var mi = type.GetMethod("CompatPluginStateChange");
                    if (mi != null) {
                        mi.Invoke(null, new object[] { this.State });
                    }
                }
            }
            
        }

        public override void UnLoad() {
            if (Plugins.Count == 0) {
                return;
            }
            Assembly assembly = Assembly.Load("kxdanmuji");
            if (assembly != null) {
                Type type = assembly.GetType("kxdanmuji.Global");
                if (type != null) {
                    var mi = type.GetMethod("RemoveDmPlugin");
                    if (mi != null) {
                        var p = new DmPlugin();
                        foreach (var item in Plugins) {
                            mi.Invoke(null, new object[] { "compat_" + p.Information.MakeUnique(item.GetType().Name) });
                        }

                    }
                }
            }
        }
        public override void Load() {
            var path = "";
            try {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                path = Path.Combine(path, "弹幕姬", "Plugins");
                Directory.CreateDirectory(path);
            } catch (Exception) {
                return;
            }
            var files = Directory.GetFiles(path);
            foreach (var file in files) {
                try {
                    if (file.ToLower().EndsWith("dll")) {
                        var dll = Assembly.LoadFrom(file);
                        foreach (var exportedType in dll.GetExportedTypes()) {
                            if (exportedType.BaseType == typeof(DMPlugin)) {
                                var plugin = (DMPlugin)Activator.CreateInstance(exportedType);
                                Log("加载兼容插件 " + plugin.PluginName);
                                Plugins.Add(plugin);
                                break;
                            }
                        }
                    }
                } catch (Exception ex) {
                    Log("加载兼容插件错误\n" + ex.ToString());
                }
            }
            if (Plugins.Count == 0) {
                return;
            }
            Assembly assembly = Assembly.Load("kxdanmuji");
            if (assembly != null) {
                Type type = assembly.GetType("kxdanmuji.Global");
                if (type != null) {
                    var execAdd = (addDelegate)Delegate.CreateDelegate(typeof(addDelegate), type, "AddDmPlugin");    //静态类方法
                    var mi = type.GetMethod("AddDmPlugin");
                    var fi_roomid = type.GetField("RoomID");
                    if (mi != null) {
                        var roomid = 0;
                        if (fi_roomid != null) {
                            roomid = Convert.ToInt32(fi_roomid.GetValue(null));
                        }
                        foreach (var item in Plugins) {
                            var p = new DmPlugin();
                            p.Information.Unique = "compat_" + p.Information.MakeUnique(item.GetType().Name);
                            p.Information.Name = item.PluginName;
                            p.Information.ShortName = item.PluginName;
                            p.Information.Author = item.PluginAuth;
                            p.Information.Description = item.PluginDesc;
                            p.Information.Version = item.PluginVer;
                            p.Information.Contact = item.PluginCont;
                            p.Information.IsCompat = true;

                            //mi.Invoke(null, new object[] { p });
                            try {
                                execAdd(p);
                                item.Start();
                                item.MainConnected(roomid);
                                AddMessage("加载兼容插件 " + item.PluginName);
                            } catch(Exception ex) {
                                Log($"加载{item.PluginName}报错\n{ex.ToString()}");
                            }
                        }

                    }
                }
            }
        }
    }
}
