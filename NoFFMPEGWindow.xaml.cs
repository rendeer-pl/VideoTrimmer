using System.Windows;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for NoFFMPEGWindow.xaml
    /// </summary>
    public partial class NoFFmpegWindow : Window
    {
        public NoFFmpegWindow()
        {
            InitializeComponent();
        }

        // Opens browser when user clicks on "Download FFmpeg" button
        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://ffmpeg.org/download.html");
        }

    }
}
