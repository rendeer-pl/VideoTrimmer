using System.IO;

public static class Globals
{

    public const string appTitle = "Video Trimmer";
    public const string description = "";
    public const string company = "Video Trimmer Contributors";
    public const string copyright = "Copyright © Video Trimmer Contributors 2019-2022";

    private const string majorVersion = "1";
    private const string minorVersion = "1";
    private const string releaseVersion = "2";
    private const string buildDate = "220724";

    public const string assemblyVersion = majorVersion + "." + minorVersion + "." + releaseVersion + ".0";
    public const string customVersion = majorVersion + "." + minorVersion + "." + releaseVersion + "." + buildDate;

    public static readonly string appOriginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    public static string FFmpegDownloadLink = "https://github.com/rendeer-pl/ffmpeg/releases/latest/download/ffmpeg.exe";
    public static string FFmpegPath;

}