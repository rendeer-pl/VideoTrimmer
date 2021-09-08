using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for NoFFMPEGWindow.xaml
    /// </summary>
    public partial class NoFFMPEGWindow : Window
    {
        public NoFFMPEGWindow()
        {
            InitializeComponent();
        }

        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ffmpeg.zeranoe.com/builds/");
        }

    }
}
