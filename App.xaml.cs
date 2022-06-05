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
        private void Application_Startup(object sender, StartupEventArgs e)
        {

            // Build the window title
            string windowTitle = Globals.appTitle;
#if DEBUG
            windowTitle += " DEBUG";
#endif

            // Check if ffmpeg.exe is present in folder
            if (System.IO.File.Exists(Globals.appOriginPath + "/ffmpeg.exe"))
            {
                Console.WriteLine("FFmpeg has been found");
                MainWindow mainWindow = new MainWindow();
                mainWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                mainWindow.Show();
                mainWindow.Title = windowTitle;
            }
            else
            {
                // FFmpeg not found
                NoFFMPEGWindow noFFMPEGWindow = new NoFFMPEGWindow();
                noFFMPEGWindow.Show();
                noFFMPEGWindow.Title = windowTitle;
            }
        }
    }
}
