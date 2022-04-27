using PlaylistsNET.Content;
using PlaylistsNET.Models;
using System.Text.Json;
using System.Timers;

namespace AmiIptvParser;


public enum ChStatus
{
    NotInit,
    Initializing,
    Initialize,
    Timeout,
    Unknown
}

internal class Channels
{
    #region events
    public event StartLoadAndProcessChannels OnStartLoadAndProcessChannels;
    public event EndLoadAndProcessChannels OnEndLoadAndProcessChannels;
    #endregion
    private System.Timers.Timer TimeOutTimer { get; set; }
    private const int TimeOutParser = 300000; //Set tiemout parser to 5 minutes
    private string PlayListUrl { get; set; }
    private Logger Logger { get; init; }
    public List<ChannelInfo> ChannelsList { get; private set; }
    public List<SeenResumeChannel> SeenResumeCache { get; private set; }
    public ChStatus Status { get; private set; }
    
    public Channels(string playlistUrl, LoggerType logType = LoggerType.Local)
    {
        TimeOutTimer = new System.Timers.Timer(TimeOutParser);
        TimeOutTimer.Elapsed += new ElapsedEventHandler(DisplayTimeEvent);
        SeenResumeCache =  new List<SeenResumeChannel>();
        ChannelsList = new List<ChannelInfo>();
        SetPlayList(playlistUrl);
        Logger = Logger.GetLogger(logType);
    }

    public void SetPlayList(string playlistUrl)
    {
        PlayListUrl = playlistUrl;
        ChannelsList.Clear();
        Status = ChStatus.NotInit;
    }

    

    public void UpdateOrSetSeenResume(ChannelInfo channel, bool seen, double currentPosition)
    {
        channel.SeenInfoCh ??= new SeenInfo();
        channel.SeenInfoCh.CurrentPosition = currentPosition;
        channel.SeenInfoCh.Seen = channel.SeenInfoCh.Seen;
        channel.SeenInfoCh.HistoryDate = DateTime.Now;
        var srCache = SeenResumeCache.Where(sr => sr.Title == channel.Title).ToList();
        if (srCache.Any())
        {
            foreach(var sr in srCache)
            {
                sr.Position = currentPosition;
                sr.Seen = seen;
                sr.Date = DateTime.Now;
            }
        }
        else
        {
            SeenResumeCache.Add(new SeenResumeChannel()
            {
                Date = DateTime.Now,
                Position = currentPosition,
                Seen = seen,
                Title = channel.Title,
                TotalDuration = channel.TotalDuration ?? -1
            });
        }        
    }
    public void LoadCache(StreamReader readerCache)
    {
        InitializeProcess();
        List<ChannelInfo> items;
        using (readerCache)
        {
            string json = readerCache.ReadToEnd();
            items = JsonSerializer.Deserialize<List<ChannelInfo>>(json);
        }
        ChannelsList = items;
        FinishProcess(ChStatus.Initialize);
    }

    private void FillSeenAndResume()
    {
        SeenResumeCache.Clear();
        foreach (var ch in ChannelsList.Where(c => c.SeenInfoCh is not null ))
        {
            SeenResumeCache.Add(new SeenResumeChannel()
            {
                Seen = ch.SeenInfoCh.Seen,
                Date = ch.SeenInfoCh.HistoryDate,
                Title = ch.Title,
                Position = ch.SeenInfoCh.CurrentPosition,
                TotalDuration = ch.TotalDuration ?? -1
            });
        }
    }

    private void InitializeProcess()
    {
        TimeOutTimer.Enabled = true;
        TimeOutTimer.Start();
        Status = ChStatus.Initializing;
        OnStartLoadAndProcessChannels?.Invoke();
    }
    public async Task RefreshList()
    {
        InitializeProcess();
        var parser = PlaylistParserFactory.GetPlaylistParser(".m3u");
        CancellationToken cancellationToken = default;
        M3uPlaylist m3UList;
        using (var client = new HttpClient())
        {
            var response = await client.GetStreamAsync(PlayListUrl, cancellationToken);
            m3UList = (M3uPlaylist)parser.GetFromStream(response);
        }

        if (m3UList == null)
        {
            FinishProcess(ChStatus.Unknown);
            throw new AmiIptvException("Can not parser the url " + PlayListUrl, 5005);
        }
        int channelNumber = 0;
        foreach (var entry in m3UList.PlaylistEntries.Where(e => e.CustomProperties.Count > 0))
        {
            var channelInfo = new ChannelInfo(entry, channelNumber);
            if (channelInfo.ChannelType != ChType.Channel && !string.IsNullOrEmpty(channelInfo.Title))
            {
                var seenInfo = SeenResumeCache.FirstOrDefault(sr => sr.Title == channelInfo.Title);
                if (seenInfo != null)
                {
                    channelInfo.SeenInfoCh = new SeenInfo()
                    {
                        Seen = seenInfo.Seen,
                        CurrentPosition = seenInfo.Position,
                        HistoryDate = seenInfo.Date
                    };
                }
                
            }
            ChannelsList.Add(channelInfo);
            channelNumber++;
        }
        FinishProcess(ChStatus.Initialize);

    }

    private void FinishProcess(ChStatus status)
    {
        Status = status;
        TimeOutTimer.Enabled = false;
        TimeOutTimer.Stop();
        OnEndLoadAndProcessChannels?.Invoke(Status);
        FillSeenAndResume();
    }

    
    private void DisplayTimeEvent(object source, ElapsedEventArgs e) => FinishProcess(ChStatus.Timeout);
}