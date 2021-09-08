using System.Windows;

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

        // Opens browser when user clicks on "Download FFmpeg" button
        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ffmpeg.zeranoe.com/builds/");
        }

    }
}
