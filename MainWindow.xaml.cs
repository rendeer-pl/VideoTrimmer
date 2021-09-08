using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VideoTrimmer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonFileOpen_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    var file = fileDialog.FileName;
                    fileNameLabel.Content = "✅ " + file;
                    fileNameLabel.ToolTip = file;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    fileNameLabel.Content = "No file selected";
                    fileNameLabel.ToolTip = "No file selected";
                    break;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ButtonTrimVideo_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Validate file

            // TODO: Validate timecodes

            // TODO: Put together a console command

            // TODO: Execute console command
            Process p = new Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.Arguments = @"/C dir /p";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            MessageBox.Show(output);
            p.WaitForExit();


        }

        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        public void ButtonShowAbout_Click(object sender, RoutedEventArgs e)
        {
            string Text;
            Version version = Assembly.GetExecutingAssembly().GetName().Version;

            Text = "Rendeer " + version.Major + "." + version.Minor + ".190510";
            MessageBox.Show(Text);
        }
    }
}
