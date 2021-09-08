using System;
using Path = System.IO.Path;
using WMPLib;
using System.Diagnostics;
using System.Management;

namespace VideoTrimmer
{
    public class VideoProcessing
    {
        private String FilePath;
        private String FileName;
        private String FileRoot;
        private String FileExtension;
        private String FileDirectory;
        private TimeSpan FileDuration;
        private float FileFramerate;
        private String NewFileName;
        private float CurrentFrame;
        private String ConsoleCommand;
        private WindowsMediaPlayer Player = new WindowsMediaPlayer();
        Process process = new Process();
        private ResultsWindowContents result;
        private string[] SupportedVideoFormats = new string[] { ".mp4", ".mpg", ".mpeg", ".wmv", ".avi", ".mov", ".mts", ".m2ts", ".vob" };
        private string[] SupportedAudioFormats = new string[] { ".mp3", ".wav", ".aiff" };
        private TimeSpan Start;
        private TimeSpan End;
        private bool ShouldRemoveAudio;
        private bool ShouldRecompress;
        private int DesiredFileSize;
        private ProgressWindow progressWindow;


        public bool LoadFile(string File)
        {
            FilePath = File;
            FileName = Path.GetFileNameWithoutExtension(FilePath);
            FileRoot = Path.GetPathRoot(FilePath);
            FileExtension = Path.GetExtension(FilePath);
            FileDirectory = Path.GetDirectoryName(FilePath);

            try
            {
                // Get video duration
                IWMPMedia clip = Player.newMedia(FilePath);
                FileDuration = TimeSpan.FromSeconds(clip.duration);
                return true;
            } catch
            {
                return false;
            }

        }

        internal void RegisterProgressWindow(ProgressWindow newProgressWindow) => progressWindow = newProgressWindow;

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

        private string GetSupportedFormats(string[] Source)
        {
            string OutputString = "";
            bool FirstLoopElement = true;
            foreach (string SupportedFormat in Source)
            {
                if (FirstLoopElement == true) FirstLoopElement = false;
                else OutputString += ";";
                OutputString += "*" + SupportedFormat;
            }
            return OutputString;
        }


        public string GetSupportedVideoFormats()
        {
            return GetSupportedFormats(SupportedVideoFormats);
        }

        public string GetSupportedAudioFormats()
        {
            return GetSupportedFormats(SupportedAudioFormats);
        }

        public bool CheckIfFileIsAccepted(string FileToCheck)
        {
            // compare against lists of supported extensions          
            if (Array.IndexOf(SupportedVideoFormats, Path.GetExtension(FileToCheck)) >= 0 || Array.IndexOf(SupportedAudioFormats, Path.GetExtension(FileToCheck)) >= 0)
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
            if ((outLine.Data != null) && (ShouldRecompress==true))
            {

                if (FileFramerate == 0.0F)
                {
                    string[] outputArray = outLine.Data.Split(' ');

                    int FPSIndex = Array.IndexOf(outputArray, "fps,");

                    if (FPSIndex > -1)
                    {
                        float.TryParse(outputArray[FPSIndex - 1], out FileFramerate);
                        Console.WriteLine("FileFramerate: " + FileFramerate);
                    }

                }
                else
                {
                    try
                    {
                        if (outLine.Data.Substring(0, 6) == "frame=")
                        {
                            float newCurrentFrame;
                            float.TryParse(outLine.Data.Substring(6), out newCurrentFrame);
                            if (newCurrentFrame > 0) CurrentFrame = newCurrentFrame;
                            int progressValue = Convert.ToInt32(CurrentFrame / ((End - Start).TotalSeconds * FileFramerate) * 100);

                            progressWindow.UpdateProgress(progressValue, false);

                            Console.WriteLine(progressValue + "%");

                        }
                    } catch (Exception)
                    {
                        Console.WriteLine("Unrecognized process output: " + outLine.Data);
                        StopProcess();
                    }
                }
#if DEBUG
                Console.WriteLine(DateTime.Now + " FFMPEG Output: " + outLine.Data);
#endif
            }
        }


        public struct ResultsWindowContents
        {
            public bool success;
            public String caption;
            public String message;
            public String newFileName;
            public String command;

            public ResultsWindowContents(bool p1, String p2, String p3, String p4, String p5)
            {
                success = p1;
                caption = p2;
                message = p3;
                newFileName = p4;
                command = p5;
            }
        }

        public void SetParameters(TimeSpan NewStart, TimeSpan NewEnd, bool NewShouldRemoveAudio, bool NewShouldRecompress, int NewDesiredFileSize)
        {
            Start = NewStart;
            End = NewEnd;
            ShouldRemoveAudio = NewShouldRemoveAudio;
            ShouldRecompress = NewShouldRecompress;
            DesiredFileSize = NewDesiredFileSize;
        }

        public void Execute()
        {

            process = new Process();

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
            NewFileName = NewPartialPath + UniqueFilenameIndex + FileExtension;

            int bitrate = 0;
            // is the DesiredFileSize specified?
            if (DesiredFileSize > 0)
            {
                // divide the desired size in Kb by the number of seconds
                bitrate = Convert.ToInt32(DesiredFileSize * 8192 / (End - Start).TotalSeconds);

                // is the remainder smaller than 300 kbps? use 300 kbps
                if (bitrate < 300) bitrate = 300;

                Console.WriteLine("Calculated max bitrate for DesiredFileSize: " + bitrate + "k");
            }


            // forge the command
            ConsoleCommand = "/c \"\"" + Globals.appOriginPath + "\\ffmpeg.exe\" -ss " + Start.ToString() + " -i \"" + FilePath + "\" -t " + Duration.ToString() + " ";

            // are we recompressing the video?
            if (ShouldRecompress == false) ConsoleCommand += "-c copy ";
            // if we are recompressing, is there a DesiredFileSize and a precalculated bitrate?
            else if ((DesiredFileSize > 0) && (bitrate > 0)) ConsoleCommand += "-crf 23 -maxrate " + bitrate + "k -bufsize " + bitrate + "k ";

            // are we removing audio?
            if (ShouldRemoveAudio == true) ConsoleCommand += "-an ";

            ConsoleCommand += "-metadata:s handler_name=\"\" \"" + NewFileName + "\" -progress pipe:1\"";

#if DEBUG
            Console.WriteLine(ConsoleCommand);
#endif
            // CONSOLE COMMAND END


            // Prepare the process to execute the command
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = "cmd.exe",
                Arguments = ConsoleCommand,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            // bind methods to receiving output data from the process
            process.OutputDataReceived += new DataReceivedEventHandler(ProcessEventHandler);
            process.ErrorDataReceived += new DataReceivedEventHandler(ProcessEventHandler);
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(ProcessExited);

            // Launch the process and start listening for output
            process.StartInfo = startInfo;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (ShouldRecompress) progressWindow.UpdateProgress(0, false);

        }

        private void ProcessExited(object sender, EventArgs e)
        {
            Console.WriteLine("Process Exited");

            // Command executed successfully
            if (process.ExitCode == 0)
            {
                result.success = true;
                result.caption = "Trimming complete";
                result.message = "";
                result.newFileName = NewFileName;
                result.command = ConsoleCommand.Substring(3);
            }
            else
            {
                // Command failed
                result.success = false;
                result.caption = "Something went wrong";
                result.message = "";
                result.command = ConsoleCommand.Substring(3);
            }

            progressWindow.TrimmingComplete(result);
        }

        private static void KillProcessAndChildrens(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            // We must kill child processes first!
            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessAndChildrens(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }

            // Then kill parents.
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

        public void StopProcess()
        {
            KillProcessAndChildrens(process.Id);
        }

    }
}
