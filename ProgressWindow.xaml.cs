using System;
using System.Diagnostics;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using static VideoTrimmer.VideoProcessing;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {

        private readonly MainWindow MainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;
        private bool IsProcessInProgress = false;
        ResultsWindowContents result;

        public ProgressWindow()
        {
            InitializeComponent();

            // if there is no main window, then close this progress window
            if (MainWindow == null) this.Close();
        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            ExecuteProcess();
        }

        private void ExecuteProcess()
        {
            IsProcessInProgress = true;

            MainWindow.videoProcessing.RegisterProgressWindow(this);

            // Execute the trimming logic!
            result = MainWindow.videoProcessing.Execute();

            IsProcessInProgress = false;

            Caption.Content = result.caption;
            Message.Content = result.message;
            Message.Visibility = Visibility.Visible;
            AbortButton.Content = "CLOSE";
            CommandButton.Visibility = Visibility.Visible;

            if (result.success == true)
            {
                SystemSounds.Asterisk.Play();
                ProgressBar.Value = 100;
                ProgressValue.Content = "100%";

                // Open the folder
                string argument = "/select, \"" + result.newFileName + "\"";
                Process.Start("explorer.exe", argument);
            }
            else
            {
                SystemSounds.Hand.Play();
                ProgressBar.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private bool ConfirmAbort()
        {
            MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to abort this operation?", "Confirmation required", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                return true;
            }
            return false;
        }

        private void AbortButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsProcessInProgress == true)
            {
                if (ConfirmAbort()) e.Cancel = false;
                else e.Cancel = true;
            }
        }

        private async void CommandButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (result.command!="")
            {
                System.Windows.Forms.Clipboard.SetText(result.command);
                CommandButton.IsEnabled = false;
                await Task.Delay(200);
                CommandButton.IsEnabled = true;
            }
        }
    }
}
