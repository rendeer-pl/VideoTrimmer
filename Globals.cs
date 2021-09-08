using System.IO;

public static class Globals
{

    public const string appTitle = "Video Trimmer";
    public const string description = "";
    public const string company = "Rendeer";
    public const string copyright = "Copyright © Rendeer 2020";

    private const string majorVersion = "0";
    private const string minorVersion = "2";
    private const string releaseVersion = "9";
    private const string buildDate = "200301";

    public const string assemblyVersion = majorVersion + "." + minorVersion + "." + releaseVersion + ".0";
    public const string customVersion = majorVersion + "." + minorVersion + "." + releaseVersion + "." + buildDate;

    public static readonly string appOriginPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

}