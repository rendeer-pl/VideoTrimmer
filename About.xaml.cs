using System;
using System.Net;
using System.Windows;
using System.Windows.Forms;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            VersionNumber.Content = Globals.customVersion;

        }

        private void ButtonUpdates_Click(object sender, RoutedEventArgs e)
        {
            // Checking for updates
            string newestVersion = "";
            try
            {
                WebClient client = new WebClient();
                newestVersion = client.DownloadString("http://soft.rendeer.pl/VideoTrimmer/current_version.php");
                Console.WriteLine("Newest available version: " + newestVersion);
            }
            catch
            {
                // Update server didn't respond but that's ok
                Console.WriteLine("Update server didn't respond.");
            }

            try
            {
                Version localVersion = new Version(Globals.customVersion);
                Version remoteVersion = new Version(newestVersion);

                int result = localVersion.CompareTo(remoteVersion);
                if (result < 0)
                {
                    MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("An update is available!\nPress OK to download the newest version of Video Trimmer.", "Update available", MessageBoxButton.OKCancel, MessageBoxImage.None);
                    if (messageBoxResult.ToString() == "OK") { System.Diagnostics.Process.Start("https://github.com/rendeer-pl/VideoTrimmer/releases/latest/download/VideoTrimmer.exe"); }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("You are already using the newest version.", "Congrats!");
                }

            } catch
            {
                // If the automatic check went wrong, let's just open the Releases page where users will be able to check for themselves.
                System.Diagnostics.Process.Start("https://soft.rendeer.pl/VideoTrimmer/releases/");
            }

        }

        private void ButtonChangelist_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://soft.rendeer.pl/VideoTrimmer/releases/");
        }

        private void ButtonWebpage_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://soft.rendeer.pl/VideoTrimmer/");
        }

        private void ButtonLincense_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://soft.rendeer.pl/VideoTrimmer/license/");
        }

        private void ButtonContribtion_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://soft.rendeer.pl/VideoTrimmer/contributing/");
        }
    }
}
