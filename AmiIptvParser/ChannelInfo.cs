using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PlaylistsNET.Models;

namespace AmiIptvParser;

public enum ChType
{
    Channel,
    Movie,
    Show
}

public class SeenInfo
{
    public bool Seen { get; set; }
    public double CurrentPosition { get; set; }
    public DateTime HistoryDate { get; set; }
}

public class ChannelInfo
{
    public string Title { get; init; }
    private string TvgId { get; init; }
    public string Group { get; init; }
    public string Logo { get; init; }
    public string URL { get; init; }
    public int ChNumber { get; init; }
    
    [JsonInclude]
    public ChType ChannelType { get;  private set; }

    public SeenInfo SeenInfoCh { get; set; }
    
    public double? TotalDuration { get; set; }



    public ChannelInfo()
    {
        TotalDuration = -1;
    }
    public ChannelInfo(int chNumber, string title, string group, string logoUrl, string url, ChType chType)
    {
        Title = title;
        ChNumber = chNumber;
        Group = group;
        Logo = logoUrl;
        URL = url;
        ChannelType = chType;
    }
    public ChannelInfo(M3uPlaylistEntry channelEntry, int chNumber)
    {
        URL = channelEntry.Path;
        string extraInfoForParser = "";
        foreach (KeyValuePair<string, string> entry in channelEntry.CustomProperties)
        {
            extraInfoForParser += entry.Key + ":" + entry.Value;
        }
        const string regexTvgName = "tvg-name=\"(.*?)\"";
        const string regexTvgLogo = "tvg-logo=\"(.*?)\"";
        const string regexTvgGroup = "group-title=\"(.*?)\"";
        const string regexTvgId = "tvg-id=\"(.*?)\"";
        const string regexTitle = "[,](?!.*[,])(.*?)$";
        Title = Utils.MatchAndResult(extraInfoForParser, regexTvgName);
        if (string.IsNullOrEmpty(Title))
        {
            Title = Utils.DecodeToUtf8(Utils.MatchAndResult(extraInfoForParser, regexTitle));
        }
        Logo = Utils.MatchAndResult(extraInfoForParser, regexTvgLogo);
        Group = Utils.DecodeToUtf8(Utils.MatchAndResult(extraInfoForParser, regexTvgGroup));
        if (string.IsNullOrEmpty(Group))
        {
            Group = Utils.NotGroup;
        }
        TvgId = Utils.MatchAndResult(extraInfoForParser, regexTvgId);
        ChNumber = chNumber;
        CalculateType();
    }
    
    private void CalculateType()
    {
        ChannelType = ChType.Channel;
        if (URL.EndsWith(".mkv") || URL.EndsWith(".avi") || URL.EndsWith(".mp4") || URL.EndsWith(".m3u8"))
        {
            ChannelType = ChType.Movie;
            if (Regex.IsMatch(Title, @"S\d\d\s*?E\d\d$"))
            {
                ChannelType = ChType.Show;
            }
        }
    }
    

    public override bool Equals(object obj)
    {
        if (obj?.GetType() != typeof(ChannelInfo))
            return false;
        var compare = (ChannelInfo) obj;
        return compare.Title == Title
               && compare.Group == Group
               && compare.TvgId == TvgId;
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(Title, TvgId, Group);
    }
}