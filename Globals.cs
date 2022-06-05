using System.IO;

public static class Globals
{

    public const string appTitle = "Video Trimmer";
    public const string description = "";
    public const string company = "Video Trimmer Contributors";
    public const string copyright = "Copyright © Video Trimmer Contributors 2019-2022";

    private const string majorVersion = "1";
    private const string minorVersion = "0";
    private const string releaseVersion = "0";
    private const string buildDate = "211026";

    public const string assemblyVersion = majorVersion + "." + minorVersion + "." + releaseVersion + ".0";
    public const string customVersion = majorVersion + "." + minorVersion + "." + releaseVersion + "." + buildDate;

    public static readonly string appOriginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

}