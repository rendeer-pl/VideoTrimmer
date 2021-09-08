using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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
