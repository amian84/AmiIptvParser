# AmiIptvParser

[![CI](https://github.com/amian84/AmiIptvParser/actions/workflows/ci.yml/badge.svg)](https://github.com/amian84/AmiIptvParser/actions/workflows/ci.yml)

Parse IPTV playlists in M3U and M3U8 format. The library targets `net10.0` and does not write a disk cache. The only NuGet dependency is `Microsoft.Extensions.Logging.Abstractions`. The logger argument on `PlaylistClient` is optional.

License: [MIT](https://github.com/amian84/AmiIptvParser/blob/main/LICENSE).

## Install

```bash
dotnet add package AmiIptvParser
```

Requires the .NET 10 SDK. Package version `1.0.0` is published from the git tag `v1.0.0`. Until that tag is pushed, nuget.org only has `0.0.1` and `0.0.2`, which use a different API. See [docs/publishing.md](https://github.com/amian84/AmiIptvParser/blob/main/docs/publishing.md).

## Usage

```csharp
using AmiIptvParser;

var playlist = M3uParser.Parse(File.ReadAllText("channels.m3u"));
foreach (var channel in playlist.Entries.Live())
{
    Console.WriteLine($"{channel.Number}. {channel.Title}");
}

using var http = new HttpClient();
var remote = await new PlaylistClient(http).LoadAsync(new Uri("https://example.com/playlist.m3u"));
```

`PlaylistClient` uses the `HttpClient` you pass in. Set timeouts and handlers on that client. A non-success HTTP status throws `HttpRequestException` and the body is not parsed.

`Movies()`, `Episodes()`, `Groups()`, and `InGroup("News")` filter the same list. An HLS `.m3u8` entry is `ContentKind.Live`. Xtream `/movie/` and `/series/` paths are movies and episodes. Titles keep commas that sit inside the name.

## What changed from 0.0.x

1.0 is a new API. `AmiParser`, the disk cache, and the PlaylistsNET dependency are gone. Details are in [CHANGELOG.md](https://github.com/amian84/AmiIptvParser/blob/main/CHANGELOG.md).
