using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace kxdanmuji {
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow : Window {
        public TestWindow() {
            InitializeComponent();
            /*
            object key = aaa.GetValue(FrameworkElement.DefaultStyleKeyProperty);
            Style style = (Style)Application.Current.FindResource(key);
            string styleString = XamlWriter.Save(style);*/
        }
        private class c {
            public string Name { get; set; }
            public string Author { get; set; } = "未知";
            public string Description { get; set; }
        }
        private ObservableCollection<c> a = new ObservableCollection<c>() {
            new c() { Name="项1" ,Description="介绍1介绍1介绍1介绍1介绍1介绍1"},
            new c() { Name="项2" ,Description="介绍2"},
            new c() { Name="项3" ,Description="介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3介绍3"}
        };
        
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            dataGrid.ItemsSource = a;
            
            object key = item1.GetValue(ListViewItem.TemplateProperty);
            //Style style = (Style)Application.Current.FindResource("");
            string styleString = XamlWriter.Save(lv.ItemContainerStyle);
            Console.WriteLine(styleString);
        }
    }
}
