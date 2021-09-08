using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using static VideoTrimmer.VideoProcessing;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {

        private readonly MainWindow MainWindow = (MainWindow)System.Windows.Application.Current.MainWindow;

        public ProgressWindow()
        {
            InitializeComponent();

            if (MainWindow == null) this.Close();

        }

        private void OnContentRendered(object sender, EventArgs e)
        {
            ExecuteProcess();
        }

        private void ExecuteProcess()
        {
            // Execute the trimming logic!
            ResultsWindowContents result = MainWindow.videoProcessing.Execute();

            if (result.success == true)
            {
                // Displays the MessageBox
                // TODO: Replace with a custom window
                 _ = System.Windows.Forms.MessageBox.Show(result.message, result.caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Open the folder
                string argument = "/select, \"" + result.newFileName + "\"";
                Process.Start("explorer.exe", argument);
            }
            else
            {
                // Displays the MessageBox
                // TODO: Replace with a custom window
                _ = System.Windows.Forms.MessageBox.Show(result.message, result.caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (ConfirmAbort()) e.Cancel = false;
            else e.Cancel = true;
        }
    }
}
