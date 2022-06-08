using System;
using System.IO;
using System.Net;
using System.Windows;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Build the window title
        string windowTitle = Globals.appTitle;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
#if DEBUG
            windowTitle += " DEBUG";
#endif

            LookForFFmpeg();
        }

        private void LookForFFmpeg()
        {
            // Check if ffmpeg.exe is present in the app's folder or in the user folder
            string appPath = Globals.appOriginPath;
            string localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\VideoTrimmer";

            if (File.Exists(appPath + "\\ffmpeg.exe") || File.Exists(localPath + "\\ffmpeg.exe"))
            {
                Console.WriteLine("FFmpeg has been found");
                CreateMainWindow();
            }
            else
            {
                // FFmpeg not found

                // Show dialog asking if the FFmpeg should be downloaded automatically
                MessageBoxResult messageBoxResult = MessageBox.Show("In order to work, Video Trimmer needs the FFmpeg library. Click OK to download it automatically.", "FFmpeg required", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (messageBoxResult.ToString() == "OK")
                {
                    // Download FFmpeg to a user folder
                    DownloadFile(localPath, "https://github.com/rendeer-pl/ffmpeg/releases/latest/download/ffmpeg.exe");
                }
                else
                {
                    // There is no FFmpeg and uset declined an automatic download
                    ShowNoFFmpegWindow();
                }
            }
        }

        private void CreateMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            mainWindow.Show();
            mainWindow.Title = windowTitle;
        }

        private void DownloadFile(string localPath, string remoteFilename)
        {
            // This will do nothing if the path already exists
            Directory.CreateDirectory(localPath);

            string localFilename = localPath + "\\ffmpeg.exe";

            Console.WriteLine("remoteFilename: " + remoteFilename);
            Console.WriteLine("localFilename: " + localFilename);

            WebClient webClient = new WebClient();
            webClient.DownloadFileAsync(new Uri(remoteFilename), localFilename);

            DownloadProgressWindow downloadProgressWindow = new DownloadProgressWindow();
            downloadProgressWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            downloadProgressWindow.Show();

            webClient.DownloadProgressChanged += (s, e2) =>
            {
                downloadProgressWindow.DownloadProgressBar.Value = e2.ProgressPercentage;
                downloadProgressWindow.DownloadProgressValue.Content = e2.ProgressPercentage+"%";
            };

            webClient.DownloadFileCompleted += (s, e2) =>
            {
                downloadProgressWindow.DownloadProgressBar.Visibility = Visibility.Hidden;
                Console.WriteLine("Download complete");
                LookForFFmpeg();
                downloadProgressWindow.Close();
            };
        }

        private void ShowNoFFmpegWindow()
        {
            NoFFmpegWindow noFFmpegWindow = new NoFFmpegWindow();
            noFFmpegWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            noFFmpegWindow.Show();
            noFFmpegWindow.Title = windowTitle;
        }
    }
}
