﻿using System;
using System.Diagnostics;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
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

            if (MainWindow.recompressFile.IsChecked == false)
            {
                ProgressBar.IsIndeterminate = true;
                taskBarItemInfoElement.ProgressState = TaskbarItemProgressState.Indeterminate;
            }
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
            MainWindow.videoProcessing.Execute();
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
                if (ConfirmAbort())
                {
                    e.Cancel = false;
                    MainWindow.videoProcessing.StopProcess();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private async void CommandButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (result.command!=null)
            {
                System.Windows.Forms.Clipboard.SetText(result.command);
                CommandButton.IsEnabled = false;
                await Task.Delay(200);
                CommandButton.IsEnabled = true;
            }
        }

        internal void UpdateProgress(int newProgress, bool isCompleted)
        {
            if (newProgress < 0) newProgress = 0;

            if ((newProgress > 99) && (isCompleted == false)) newProgress = 99;

            if (isCompleted == true) newProgress = 100;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressValue.Visibility = Visibility.Visible;
                ProgressBar.Value = newProgress;
                ProgressValue.Content = newProgress.ToString() + "%";
                if (isCompleted == true) taskBarItemInfoElement.ProgressState = TaskbarItemProgressState.Paused;
                else taskBarItemInfoElement.ProgressState = TaskbarItemProgressState.Normal;
                taskBarItemInfoElement.ProgressValue = (double)newProgress / 100;
            }), DispatcherPriority.Background);
        }

        internal void TrimmingComplete(ResultsWindowContents newResult)
        {
            result = newResult;
            IsProcessInProgress = false;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Caption.Content = result.caption;
                Message.Content = result.message;
                Message.Visibility = Visibility.Visible;
                AbortButton.Content = "CLOSE";
                CommandButton.Visibility = Visibility.Visible;
                
            }), DispatcherPriority.Background);

            if (result.success == true)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    SystemSounds.Asterisk.Play();
                    UpdateProgress(100, true);
                }), DispatcherPriority.Background);
                

                // Open the folder
                string argument = "/select, \"" + result.newFileName + "\"";
                Process.Start("explorer.exe", argument);
            }
            else
            {
                SystemSounds.Hand.Play();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ProgressBar.Foreground = new SolidColorBrush(Colors.Red);
                    ProgressBar.Background = new SolidColorBrush(Colors.Red);
                    ProgressBar.BorderBrush = new SolidColorBrush(Colors.Red);
                    taskBarItemInfoElement.ProgressState = TaskbarItemProgressState.Error;
                    taskBarItemInfoElement.ProgressValue = (double)1;
                }), DispatcherPriority.Background);
            }
        }
    }
}
