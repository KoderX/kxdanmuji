using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace kxdanmuji_plugin_framework {
    /// <summary>
    /// 插件基类
    /// </summary>
    public class DmPlugin : INotifyPropertyChanged {
        private delegate void addMessageDelegate(string name, string msg);
        private delegate void logDelegate(string msg);
        private addMessageDelegate addMessage;
        private logDelegate logger;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 插件状态 true启用 false停用
        /// </summary>
        public bool State {
            get {
                return _state;
            }
            set {
                _state = value;
                if (PropertyChanged != null) {
                    PropertyChanged(this, new PropertyChangedEventArgs("State"));
                }
            }
        }
        private bool _state = true;
        /// <summary>
        /// 插件信息
        /// </summary>
        public DmPluginInformation Information { get; set; } = new DmPluginInformation();
        /// <summary>
        /// 插件配置信息
        /// </summary>
        private Dictionary<string, DmPluginConfig> Config { get; } = new Dictionary<string, DmPluginConfig>();
        /// <summary>
        /// 插件加载
        /// </summary>
        public virtual void Load() {

        }
        /// <summary>
        /// 插件卸载
        /// </summary>
        public virtual void UnLoad() {

        }
        /// <summary>
        /// 插件状态改变
        /// </summary>
        public virtual void StateChange() {

        }
        /// <summary>
        /// 收到弹幕的异步调用方法
        /// </summary>
        /// <param name="dm">弹幕类实例</param>
        public virtual void ReceiveDanmaku(DmModel dm) {

        }
        /// <summary>
        /// 收到弹幕的同步调用方法
        /// </summary>
        /// <param name="dm">弹幕类实例</param>
        /// <returns></returns>
        public virtual DmModel ReceiveDanmakuSync(DmModel dm) {
            return dm;
        }
        /// <summary>
        /// 手动增加弹幕
        /// </summary>
        /// <param name="dm">弹幕类实例</param>
        protected void AddDanmaku(DmModel dm) {

        }
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息信息</param>
        protected void AddMessage(string message) {
            if (addMessage == null) {
                Assembly assembly = Assembly.Load("kxdanmuji");
                if (assembly != null) {
                    Type type = assembly.GetType("kxdanmuji.Global");
                    if (type != null) {
                        addMessage = (addMessageDelegate)Delegate.CreateDelegate(typeof(addMessageDelegate), type, "AddMessage");
                    }
                }
            }
            if (addMessage != null) {
                addMessage(this.Information.ShortName, message);
            }

        }
        protected void Log(string message) {
            //Console.WriteLine($"[{this.Information.Name}]{message}");
            //return;
            if (logger == null) {
                Assembly assembly = Assembly.Load("kxdanmuji");
                if (assembly != null) {
                    Type type = assembly.GetType("kxdanmuji.Global");
                    if (type != null) {
                        logger = (logDelegate)Delegate.CreateDelegate(typeof(logDelegate), type, "PluginLog");
                    }
                }
            }
            if (logger != null) {
                logger($"[{this.Information.Name}]{message}");
            }
        }
    }
    public class DmPluginConfig {
        public DmPluginConfig(string name) {
            if (!Regex.IsMatch(name, @"^[a-z][a-z]*(_[a-z]+)*$")) {
                throw new FormatException("配置名称格式错误 应该由小写字母,下划线组成,并且字母开头");
            }
            _name = name;
        }
        private string _name = "";
        /// <summary>
        /// 是否手动管理配置信息
        /// 如果是手动 除了名字(Name)其他都没必要设置了
        /// </summary>
        public bool IsManual { get; set; } = false;
        /// <summary>
        /// 配置类型
        /// </summary>
        public DmPluginConfigType Type { get; set; } = DmPluginConfigType.String;
        /// <summary>
        /// 配置名称 应该由小写字母,下划线组成,并且字母开头 如mode,show_sth,is_sth
        /// </summary>
        public string Name {
            get {
                return _name;
            }
        }
        /// <summary>
        /// 配置项标题 会显示在设置界面
        /// </summary>
        public string Title { get; set; } = "";
        /// <summary>
        /// 标题后方提示文本
        /// </summary>
        public string TitleAddition { get; set; } = "";
        /// <summary>
        /// 正则表达式 用于匹配输入是否合法
        /// </summary>
        public string RegexRule { get; set; } = "";
        /// <summary>
        /// 正则匹配失败的提示文本
        /// </summary>
        public string RegexMessage { get; set; } = "";
        /// <summary>
        /// 配置项默认值
        /// </summary>
        public string Default { get; set; } = "";
        /// <summary>
        /// 配置项值
        /// </summary>
        public string Value { get; set; } = "";
        /// <summary>
        /// 附加信息 具体请查看文档
        /// </summary>
        public string Extra { get; set; } = "";
    }
    /// <summary>
    /// 配置选项类型 String-字符串 Integer-数字 Check-复选 Select-下拉菜单 Radio-单选 Button-按钮 _Title-标题
    /// </summary>
    public enum DmPluginConfigType {
        String, Integer, Check, Select, Radio, Button, _Title
    }
    /// <summary>
    /// 插件描述类
    /// </summary>
    public class DmPluginInformation {
        /// <summary>
        /// 插件唯一标识符
        /// 由小写字母,下划线组成,字母开头
        /// * 插件创建后就别改了 否则会丢失配置 推荐使用MakeUnique(this.GetType().Name)生成
        /// </summary>
        public string Unique {
            get {
                return unique;
            }
            set {
                if (!Regex.IsMatch(value, @"^[a-z][a-z]*(_[a-z]+)*$")) {
                    throw new FormatException("唯一标识符格式错误 应该由小写字母,下划线组成,并且字母开头");
                }
                unique = value;
            }
        }
        /// <summary>
        /// 是否是同步插件
        /// 一般如果要拦截弹幕信息才需要同步
        /// 没事闲的别设置同步
        /// </summary>
        public bool IsSync { get; set; } = false;
        /// <summary>
        /// 是否是兼容的老版本插件
        /// * 请不要自己设置为true 否则插件会失效
        /// </summary>
        public bool IsCompat { get; set; } = false;
        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// 插件短名称 用于输出信息时候显示的
        /// </summary>
        public string ShortName { get; set; } = "";
        /// <summary>
        /// 插件描述
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// 作者姓名
        /// </summary>
        public string Author { get; set; } = "";
        /// <summary>
        /// 作者联系方式
        /// </summary>
        public string Contact { get; set; } = "";
        /// <summary>
        /// 插件版本号
        /// 由数字和点组成
        /// </summary>
        public string Version {
            get {
                return version;
            }
            set {
                if (!Regex.IsMatch(value, @"^[0-9]+(\.[0-9]+)*$")) {
                    // 尝试去掉乱七八糟的东西
                    char[] charList = value.ToArray();
                    var sb = new StringBuilder();
                    foreach (var c in charList) {
                        if (Char.IsDigit(c) || c == '.') {
                            sb.Append(c);
                        }
                    }
                    value = sb.ToString();
                    if (!Regex.IsMatch(value, @"^[0-9]+(\.[0-9]+)*$")) {
                        // throw new FormatException("版本号错误 应该由数字和点组成");
                        version = "1.0";
                        return;
                    }
                }
                version = value;
            }
        }
        /// <summary>
        /// 根据名称生成标识符
        /// </summary>
        /// <param name="pluginName">一般情况传入插件类名即可</param>
        /// <returns>格式化后的标识符</returns>
        public string MakeUnique(string pluginName) {
            char[] charList = pluginName.ToArray();
            var sb = new StringBuilder();
            var f = true;
            foreach (var c in charList) {
                if (f && Char.IsLetter(c)) {
                    f = false;
                    sb.Append(Char.ToLower(c));
                    continue;
                }
                if (Char.IsUpper(c)) {
                    sb.Append("_");
                    sb.Append(Char.ToLower(c));
                } else if (Char.IsLower(c)) {
                    sb.Append(c);
                }
            }
            if (f) {
                throw new FormatException("你的类名是什么鬼! 差评! 请使用驼峰命名法");
            }
            return sb.ToString();
        }
        private string version = "1.0";
        private string unique;
    }
}
