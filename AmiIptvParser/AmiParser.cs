
using System.Text.Json;

namespace AmiIptvParser;

public delegate void StartLoadAndProcessChannels();
public delegate void EndLoadAndProcessChannels(ChStatus status);
public class AmiParser
{

    protected AmiParser()
    {}


    internal static Channels Instance { get; private set; }

    private static void CheckInstanceInitialize()
    {
        if (Instance == null)
        {
            throw new AmiIptvException("Parser not initialized", 5001);
        }
    }
    
    public static void SubscribeStartEventProcessList(StartLoadAndProcessChannels delegateFunction)
    {
        CheckInstanceInitialize();
        Instance.OnStartLoadAndProcessChannels += delegateFunction;
    }
    public static void SubscribeEndEventProcessList(EndLoadAndProcessChannels delegateFunction)
    {
        CheckInstanceInitialize();
        Instance.OnEndLoadAndProcessChannels += delegateFunction;
    }

    public static void UnsubscribeStartEventProcessList(StartLoadAndProcessChannels delegateFunction)
    {
        CheckInstanceInitialize();
        Instance.OnStartLoadAndProcessChannels -= delegateFunction;
    }

    public static void UnsubscribeEndEventProcessList(EndLoadAndProcessChannels delegateFunction)
    {
        CheckInstanceInitialize();
        Instance.OnEndLoadAndProcessChannels -= delegateFunction;
    }

    private static void SaveCacheData(string name, List<ChannelInfo> channels)
    {
        if (!Directory.Exists(Utils.DataFolder + "\\lists\\"))
        {
            Directory.CreateDirectory(Utils.DataFolder + "\\lists\\");
        }
        if (File.Exists(Utils.DataFolder + "\\lists\\" + name + "_cache.json"))
        {
            File.Delete(Utils.DataFolder + "\\lists\\" + name + "_cache.json");
        }

        using var file = File.CreateText(Utils.DataFolder + "\\lists\\" + name + "_cache.json");
        file.Write(JsonSerializer.Serialize(channels));
    }


    public static void Initializer(UrlObject playListUrl, LoggerType logType = LoggerType.Local, bool loadCache=true)
    {
        if (Instance == null)
        {
            Instance = new Channels(playListUrl.Url, logType);
            SubscribeEndEventProcessList((status) =>
            {
                if (status == ChStatus.Initialize)
                {
                    SaveCacheData(playListUrl.Name, Instance.ChannelsList);
                }
            });
        }
        else
        {
            Instance.SetPlayList(playListUrl.Url);
        }
        if (loadCache)
        {
            CheckAndLoadCache(playListUrl.Name);
        }
    }


    private static bool CheckAndLoadCache(string name)
    {
        if (!File.Exists(Utils.DataFolder + "\\lists\\" + name + "_cache.json")) return false;
        var readerCache = new StreamReader(Utils.DataFolder + "\\lists\\" + name + "_cache.json");
        Instance.LoadCache(readerCache);
        return true;
    }

    public static IEnumerable<ChannelInfo> GetAllChannels()
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList;
    }

    public static void ProcessListSync()
    {
        Instance.RefreshList().Wait();
    }
    
    public static async Task ProcessListAsync()
    {
        await Instance.RefreshList();
    }

    public static ChStatus GetStatus()
    {
        CheckInstanceInitialize();
        return Instance.Status;
    }

    public static IEnumerable<ChannelInfo> GetLiveChannels()
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList.Where(chInfo => chInfo.ChannelType == ChType.Channel);
    }

    public static IEnumerable<ChannelInfo> GetVodChannels()
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList.Where(chInfo => chInfo.ChannelType != ChType.Channel);
    }
    public static IEnumerable<ChannelInfo> GetMovies()
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList.Where(chInfo => chInfo.ChannelType == ChType.Movie);
    }

    public static IEnumerable<ChannelInfo> GetChannelsByGroup(string groupTitle)
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList.Where(chInfo => chInfo.Group == groupTitle);
    }

    public static List<SeenResumeChannel> GetHistorySeen()
    {
        CheckInstanceInitialize();
        return Instance.SeenResumeCache;
    }

    public static IEnumerable<string> GetAllGroups()
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList.Select(chInfo => chInfo.Group).Distinct();
    }

    public static IEnumerable<string> GetAllShowsGroups()
    {
        CheckInstanceInitialize();
        return Instance.ChannelsList.Where(chInfo => chInfo.ChannelType == ChType.Show).Select(chInfo => chInfo.Group).Distinct();
    }

    public static void UpdateOrSetSeenResumeByNumber(int chNumber, bool seen, double currentPosition)
    {
        CheckInstanceInitialize();
        Instance.UpdateOrSetSeenResume(GetChannelNumber(chNumber), seen, currentPosition);
    }

    public static ChannelInfo GetChannelNumber(int chNumber)
    {
        CheckInstanceInitialize();
        var channelInfo = Instance.ChannelsList.FirstOrDefault(chInfo => chInfo.ChNumber == chNumber);
        return channelInfo ?? throw new AmiIptvException("Channel not exist", 5002);  
        
    }
}