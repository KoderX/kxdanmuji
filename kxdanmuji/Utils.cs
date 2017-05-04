using KxLib.Utilities;
using LitJson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace kxdanmuji {
    static class Utils {
        public static BitmapImage LoadImageFromFile(String path) {
            using (BinaryReader loader = new BinaryReader(File.Open(path, FileMode.Open))) {
                FileInfo fd = new FileInfo(path);
                int Length = (int)fd.Length;
                byte[] buf = new byte[Length];
                buf = loader.ReadBytes((int)fd.Length);


                //开始加载图像  
                BitmapImage bim = new BitmapImage();
                bim.BeginInit();
                bim.StreamSource = new MemoryStream(buf);
                bim.EndInit();
                return bim;
            }
        }


        public static byte[] ToBE(this byte[] b) {
            if (BitConverter.IsLittleEndian) {
                return b.Reverse().ToArray();
            } else {
                return b;
            }
        }

        public static void ReadB(this NetworkStream stream, byte[] buffer, int offset, int count) {
            if (offset + count > buffer.Length)
                throw new ArgumentException();
            int read = 0;
            while (read < count) {
                var available = stream.Read(buffer, offset, count - read);
                if (available == 0) {
                    throw new ObjectDisposedException(null);
                }
                read += available;
                offset += available;

            }
        }
        public static int ToInt32(this string s) {
            return Convert.ToInt32(s);
        }
        public static int ToInt32(this JsonData jd) {
            return Convert.ToInt32(jd.ToString());
        }
        public static long ToInt64(this string s) {
            return Convert.ToInt64(s);
        }
        public static long ToInt64(this JsonData jd) {
            return Convert.ToInt64(jd.ToString());
        }
    }
    public class Danmaku {
        public DateTime DeadTime { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public string TypeColor {
            get {
                if (Type.Equals("礼物")) {
                    return "#5cb85c";
                } else if (Type.Equals("系统")) {
                    return "#d9534f";
                }else if (Type.Equals("插件")) {
                    return "#f0ad4e";
                } else { 
                    return "#5bc0de";
                }
            }
        }
        public string Str1 { get; set; }
        public string Str1Color { get; set; }
        public string Str2 { get; set; }
        public string ToXaml {
            get {
                var sb = new StringBuilder();
                sb.Append(@"<FlowDocument xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""><Paragraph>");
                if ("礼物|系统|插件".IndexOf(Type)>0) {
                    sb.Append($@"<Run Foreground=""White"" Background=""{TypeColor}"">{Type}</Run>");
                }
                if (Str1.Length > 0) {
                    sb.Append($@"<Run Foreground=""{Str1Color}"">{Str1.Replace(" ", "&#160;")}</Run>");
                }
                sb.Append($@"<Run Foreground=""White"">{Str2}</Run>");
                sb.Append(@"</Paragraph></FlowDocument>");
                return sb.ToString() ;
            }
        }
    }

    public class RichTextBoxHelper : DependencyObject {
        private static HashSet<Thread> _recursionProtection = new HashSet<Thread>();

        public static string GetDocumentXaml(DependencyObject obj) {
            return (string)obj.GetValue(DocumentXamlProperty);
        }

        public static void SetDocumentXaml(DependencyObject obj, string value) {
            _recursionProtection.Add(Thread.CurrentThread);
            obj.SetValue(DocumentXamlProperty, value);
            _recursionProtection.Remove(Thread.CurrentThread);
        }

        public static readonly DependencyProperty DocumentXamlProperty = DependencyProperty.RegisterAttached(
            "DocumentXaml",
            typeof(string),
            typeof(RichTextBoxHelper),
            new FrameworkPropertyMetadata(
                "",
                FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (obj, e) => {
                    if (_recursionProtection.Contains(Thread.CurrentThread))
                        return;

                    var richTextBox = (RichTextBox)obj;

                    // Parse the XAML to a document (or use XamlReader.Parse())

                    try {
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(GetDocumentXaml(richTextBox)));
                        var doc = (FlowDocument)XamlReader.Load(stream);

                        // Set the document
                        richTextBox.Document = doc;
                    } catch (Exception) {
                        richTextBox.Document = new FlowDocument();
                    }

                    // When the document changes update the source
                    richTextBox.TextChanged += (obj2, e2) => {
                        RichTextBox richTextBox2 = obj2 as RichTextBox;
                        if (richTextBox2 != null) {
                            SetDocumentXaml(richTextBox, XamlWriter.Save(richTextBox2.Document));
                        }
                    };
                }
            )
        );
    }
}
