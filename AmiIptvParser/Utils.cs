using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace AmiIptvParser;

internal static class Utils
{
    public const string NotGroup = "NOT_GROUP";
    public static string DecodeToUtf8(string strParam)
    {
        byte[] bytes = Encoding.Default.GetBytes(strParam);
        strParam = Encoding.UTF8.GetString(bytes);
        return strParam;
    }


    public static string MatchAndResult(string toSearch, string pattern)
    {
        var result = Regex.Match(toSearch, pattern);
        return result.Success ? result.Groups[1].Value : "";
    }

    public static readonly string DataFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "\\AmiIptvParserData\\";
}

public class SeenResumeChannel
{
    public string Title { get; set; }
    public double Position { get; set; }
    public double TotalDuration { get; set; }
    public bool Seen { get; set; }
    public DateTime Date { get; set; }

}

[DataContract]
public class AccountInfo
{

}
public class UrlObject
{
    public string Url { get; set; }
    public string Name { get; set; }
    public string LogoListUri { get; set; }
}


