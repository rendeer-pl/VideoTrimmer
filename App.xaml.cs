using System;
using System.Windows;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Checking for updates
            /* 
            try
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                WebClient client = new WebClient();
                string newestVersion = client.DownloadString("https://rendeer.pl/VideoTrimmer/updates.php?v="+ version.Major + "." + version.Minor + "." + version.Build);
                Console.WriteLine("Newest available version: " + newestVersion);
            }
            catch
            {
                // Update server didn't respond but that's ok
                Console.WriteLine("Update server didn't respond.");
            }
            */

            // Check if ffmpeg.exe is present in folder
            if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "/ffmpeg.exe"))
            {
                Console.WriteLine("FFmpeg has been found");
                MainWindow mainWindow = new MainWindow();
                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                mainWindow.Show();
            // FFmpeg not found
            } else
            {
                NoFFMPEGWindow noFFMPEGWindow = new NoFFMPEGWindow();
                noFFMPEGWindow.Show();
            }
        }
    }
}
