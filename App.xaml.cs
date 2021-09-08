using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
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



            if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "/ffmpeg.exe"))
            {
                Console.WriteLine("FFMPEG has been found");
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();

            } else
            {
                NoFFMPEGWindow noFFMPEGWindow = new NoFFMPEGWindow();
                noFFMPEGWindow.Show();
            }



        }
    }
}
