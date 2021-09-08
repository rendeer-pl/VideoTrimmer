using System;
using Path = System.IO.Path;
using WMPLib;
using System.Diagnostics;
using System.Windows.Forms;

namespace VideoTrimmer
{
    public class VideoProcessing
    {
        String FilePath;
        String FileName;
        String FileRoot;
        String FileExtension;
        String FileDirectory;
        TimeSpan FileDuration;
        WindowsMediaPlayer Player = new WindowsMediaPlayer();
        ResultsWindowContents result;

        public bool LoadFile(string File)
        {
            FilePath = File;
            FileName = Path.GetFileName(FilePath);
            FileRoot = Path.GetPathRoot(FilePath);
            FileExtension = Path.GetExtension(FilePath);
            FileDirectory = Path.GetDirectoryName(FilePath);

            // Get video duration
            var clip = Player.newMedia(FilePath);
            FileDuration = TimeSpan.FromSeconds(clip.duration);

            return false;
        }

        public string GetFilePath()
        {
            return FilePath;
        }

        public string GetFileName() {
            return FileName;
        }

        public string GetFileRoot()
        {
            return FileRoot;
        }

        public TimeSpan GetDuration()
        {
            return FileDuration;
        }

        public bool CheckIfFileIsAccepted(string FileToCheck)
        {
            // TODO: compare against an enum of supported extensions
            if (Path.GetExtension(FileToCheck) == ".mp4")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool DoesTheFileExist() => System.IO.File.Exists(FilePath);

        // Handles output from the transcoding process
        private void ProcessEventHandler(object e, DataReceivedEventArgs outLine)
        {
            System.Console.WriteLine(DateTime.Now + " - " + outLine.Data);
        }


        public struct ResultsWindowContents
        {
            public bool success;
            public String caption;
            public String message;
            public String newFileName;
            public ResultsWindowContents(bool p1, String p2, String p3, String p4)
            {
                success = p1;
                caption = p2;
                message = p3;
                newFileName = p4;
            }
        }

        public ResultsWindowContents Execute(TimeSpan Start, TimeSpan End, bool ShouldRemoveAudio, bool ShouldRecompress)
        {

            TimeSpan Duration = End - Start;

            // find a unique filename
            String NewPartialPath = FileDirectory + "\\" + FileName + "_trim_";
            int UniqueFilenameIndex = 1;
            bool FoundUniqueName = false;
            while (FoundUniqueName == false)
            {
                if (System.IO.File.Exists(NewPartialPath + UniqueFilenameIndex + FileExtension))
                {
                    Console.WriteLine("Name already taken: " + NewPartialPath + UniqueFilenameIndex + FileExtension);
                    UniqueFilenameIndex++;
                }
                else
                {
                    Console.WriteLine("Name available: " + NewPartialPath + UniqueFilenameIndex + FileExtension);
                    FoundUniqueName = true;
                }
            }
            String NewFileName = NewPartialPath + UniqueFilenameIndex + FileExtension;


            // forge the command
            String ConsoleCommand = "/C ffmpeg -ss " + Start.ToString() + " -i \"" + FilePath + "\" -t " + Duration.ToString() + " ";

            // are we recompressing the video?
            if (ShouldRecompress == false) ConsoleCommand += "-c copy ";

            // are we removing audio?
            if (ShouldRemoveAudio == true) ConsoleCommand += "-an ";

            ConsoleCommand += "\"" + NewFileName + "\"";
            // CONSOLE COMMAND END


            // Prepare the process to execute the command
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                Arguments = ConsoleCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // bind methods to receiving output data from pricess
            process.OutputDataReceived += new DataReceivedEventHandler(ProcessEventHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ProcessEventHandler);

            // Launch the process and start listening for output
            process.StartInfo = startInfo;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            // Command executed successfully
            if (process.ExitCode == 0)
            {
                result.success = true;
                result.caption = "Everything should be ok";
                result.message = "Video trimmed using the following command:\n\r\n\r" + ConsoleCommand.Substring(3);
                result.newFileName = NewFileName;
            }
            else
            {
                // Command failed
                result.success = false;
                result.caption = "Something went wrong";
                result.message = "Command that was used:\n\r\n\r" + ConsoleCommand.Substring(3);
            }

            return result;
        }

    }
}
