using System.Linq;
using NUnit.Framework;
using AmiIptvParser;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text;
using System.IO;

namespace UnitTest;

public class Tests
{
    
    [SetUp]
    public void Setup()
    {
        var testListUrl = new UrlObject()
        {
            Url = "https://run.mocky.io/v3/e68af937-3b8f-4c8a-91d6-ebe27ed1c17b",
            Name = "Test"
        };

        AmiParser.Initializer(testListUrl, loadCache:false);
    }

    [Test]
    public void ExistAnyChannel()
    {
        AmiParser.ProcessList();
        Assert.True(AmiParser.GetAllChannels().Any());
    }
    
    [Test]
    public void GetChannelByNumber()
    {
        AmiParser.ProcessList();
        Assert.AreEqual(AmiParser.GetChannelNumber(3).Title, "Canal4");
    }

    [Test]
    public void CheckGroups()
    {
        AmiParser.ProcessList();
        Assert.AreEqual(AmiParser.GetAllGroups().Count(), 4);
    }

    [Test]
    public void CheckShows()
    {
        AmiParser.ProcessList();
        Assert.AreEqual(AmiParser.GetAllShowsGroups().Count(), 2);
    }


    [Test]
    public void LoadCache()
    {
        var channels = new List<ChannelInfo>
        {
            new ChannelInfo(1, "Channel1", "Group1", "", "", ChType.Channel),
            new ChannelInfo(2, "Channel2", "Group1", "", "", ChType.Channel),
            new ChannelInfo(3, "Channel3", "Group2", "", "", ChType.Channel),
            new ChannelInfo(4, "Channel4", "Group2", "", "", ChType.Channel),
            new ChannelInfo(5, "Movies", "MOVIES", "", "", ChType.Movie)
        };

        byte[] byteArray = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(channels));
        var stream = new MemoryStream(byteArray);

        // convert stream to string
        var reader = new StreamReader(stream);

        AmiParserToTest.CheckCacheTest(reader);
        Assert.AreEqual(AmiParser.GetAllGroups().Count(), 3);
        Assert.AreEqual(AmiParser.GetMovies().Count(), 1);
    }
    
    [Test]
    public void SeenMovies()
    {
        var channels = new List<ChannelInfo>
        {
            new ChannelInfo(1, "Channel1", "Group1", "", "", ChType.Channel),
            new ChannelInfo(2, "Channel2", "Group1", "", "", ChType.Channel),
            new ChannelInfo(3, "Channel3", "Group2", "", "", ChType.Channel),
            new ChannelInfo(4, "Mov1", "MOVIES", "", "", ChType.Movie),
            new ChannelInfo(5, "Mov2", "MOVIES", "", "", ChType.Movie)
        };

        channels[4].SeenInfoCh = new SeenInfo()
        {
            Seen = true,
            HistoryDate = new DateTime(2022, 01, 01)
        };

        byte[] byteArray = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(channels));
        var stream = new MemoryStream(byteArray);

        // convert stream to string
        var reader = new StreamReader(stream);

        AmiParserToTest.CheckCacheTest(reader);
        Assert.AreEqual(AmiParser.GetHistorySeen().Count, 1);
        Assert.True(AmiParser.GetHistorySeen()[0].Seen);
        Assert.AreEqual(AmiParser.GetHistorySeen()[0].Date, new DateTime(2022, 01, 01));
        Assert.AreEqual(AmiParser.GetHistorySeen()[0].Title, "Mov2");
    }

    [Test]
    public void TestSignals()
    {
        
        var are = new AutoResetEvent(false);
        AmiParser.SubscribeStartEventProcessList(() =>
        {
            Assert.AreEqual(AmiParser.GetStatus(), ChStatus.Initializing);
        });

        AmiParser.SubscribeEndEventProcessList((status) =>
        {
            Assert.AreEqual(status, ChStatus.Initialize);
            are.Set();
        });
        AmiParser.ProcessList();
        var wasSignaled = are.WaitOne(timeout: TimeSpan.FromSeconds(5));
        Assert.True(wasSignaled);
    }
}