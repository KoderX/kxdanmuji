using kxdanmuji_plugin_framework;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace kxdanmuji.Pages {
    /// <summary>
    /// PluginPage.xaml 的交互逻辑
    /// </summary>
    public partial class PluginPage : Page {
        private MainWindow mainWindow;

        public PluginPage(MainWindow main) {
            InitializeComponent();
            Global.PluginPage = this;
            mainWindow = main;

            dgList.ItemsSource = Global.pluginList;
            dgList.LoadingRow += (sender, e) => { e.Row.Header = e.Row.GetIndex(); };

            var ds = Directory.GetDirectories("plugins");

            new Task(() => {
                foreach (var d in ds) {
                    var dllpath = d + Regex.Match(d, @"\\[a-zA-Z]+$").Groups[0] + ".dll";

                    Console.WriteLine(dllpath);
                    if (File.Exists(dllpath)) {
                        try {
                            var dll = Assembly.LoadFrom(dllpath);
                            foreach (var exportedType in dll.GetExportedTypes()) {
                                if (exportedType.BaseType == typeof(DmPlugin)) {
                                    var plugin = (DmPlugin)Activator.CreateInstance(exportedType);
                                    this.Dispatcher.Invoke(new Action(() => {
                                        Global.pluginList.Add(plugin);
                                    }));
                                    Console.WriteLine("add plugin " + plugin.Information.Name);
                                    break;
                                }
                            }
                        } catch (Exception) {
                        }
                    }
                }
                foreach (var plugin in Global.pluginList) {
                    try {
                        plugin.Load();
                    } catch (Exception) {
                    }
                }
            }).Start();



        }
        private void MenuItem_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(dgList.SelectedItem.GetType().Name);
        }

        private void dgList_LoadingRow(object sender, DataGridRowEventArgs e) {
            e.Row.MouseRightButtonDown += (s, a) => {
                var menu = new ContextMenu();
                a.Handled = true;
                var plugin = (e.Row.Item as DmPlugin);
                menu.Items.Add(new MenuItem() {
                    Header = plugin.Information.Name,
                    IsEnabled = false
                });
                menu.Items.Add(new MenuItem() {
                    Header = "-"
                });
                var menuState = new MenuItem() {
                    Header = plugin.State ? "禁用插件" : "启用插件",
                    IsEnabled = !plugin.Information.IsCompat
                };
                menuState.PreviewMouseLeftButtonDown += (s1, a1) => {
                    if (!plugin.Information.IsCompat) {
                        plugin.State = !plugin.State;
                        plugin.StateChange();
                    }
                };
                menu.Items.Add(menuState);
                var menuConfig = new MenuItem() {
                    Header = "配置"
                };
                menuConfig.PreviewMouseLeftButtonDown += (s1, a1) => {
                    if (plugin.Information.IsCompat) {
                        var compat = Global.pluginList.Where(o => o.Information.Unique == "bililivedm_compat").Single();
                        compat.ReceiveDanmaku(new DmModel() {
                            Type = DmType.COMMAND,
                            Message = new DmMessage() {
                                UserID = 1,
                                UserName = plugin.Information.Unique
                            }
                        });
                    } else {

                    }
                };
                menu.Items.Add(menuConfig);
                DataGrid row = sender as DataGrid;
                row.ContextMenu = menu;
            };
        }
    }
    public class IndexConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            var item = (DmPlugin)values[0];
            var list = (ObservableCollection<DmPlugin>)values[1];
            return String.Format("{0:00}", list.IndexOf(item) + 1);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class TestConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value.GetType() == typeof(bool)) {
                if ((bool)value == true) {
                    return "[兼容]";
                }
            } else if (value.GetType() == typeof(string)) {
                return "当前版本: " + value;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

    }
    public class StateConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value.GetType() == typeof(bool)) {
                if ((bool)value == true) {
                    return "已启用";
                } else {
                    return "未启用";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

    }
}