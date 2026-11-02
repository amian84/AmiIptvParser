# Changelog

All notable changes to AmiIptvParser are documented here.

The package version comes from a git tag: `v1.0.0` produces `1.0.0`. This section is the 1.0 surface. It is not on nuget.org until that tag is pushed. Builds before the first tag are `1.0.0-alpha.0` plus the commit height. See [docs/publishing.md](docs/publishing.md).

Packages `0.0.1` and `0.0.2` targeted `net6.0` and are not compatible with this API. There is no compatibility shim.

## 1.0.0

### Breaking

- Removed the static `AmiParser` facade and its process-wide playlist: `Initializer`, `ProcessListSync`, `ProcessListAsync`, `GetAllChannels`, `GetLiveChannels`, `GetVodChannels`, `GetMovies`, `GetChannelsByGroup`, `GetAllGroups`, `GetAllShowsGroups`, `GetChannelNumber`, `GetHistorySeen`, `UpdateOrSetSeenResumeByNumber`, `GetStatus`, and the subscribe/unsubscribe methods.
- Removed disk cache, the Windows path layout under the entry assembly, and the logger types `Logger`, `LocalLogger`, `LogType`, and `LoggerType`.
- Removed `ChannelInfo`, `SeenInfo`, `SeenResumeChannel`, `UrlObject`, `AccountInfo`, `ChType`, `ChStatus`, `AmiIptvException`, `AmiParserToTest`, and the public event delegates.
- Removed the PlaylistsNET dependency. Parsing lives in this package.
- Invalid playlist text throws `PlaylistFormatException`. The integer error codes (`5001`, `5002`, `5005`, `580`) are gone.
- Target framework is `net10.0` only.
- License is MIT (`PackageLicenseExpression`). The 0.0.x packages did not declare a license.

Replace `AmiParser.ProcessList*` with `M3uParser` for text you already have, or with `PlaylistClient` when the playlist is downloaded. Watch history and a disk cache, if you still want them, belong in the application.

### Added

- `M3uParser.Parse`, `ParseAsync`, and `ParseEntriesAsync`, including overloads that take an `IContentKindClassifier`.
- Immutable `M3uPlaylist` and `PlaylistEntry` (`Number`, `Title`, `Url`, `TvgId`, `TvgName`, `LogoUrl`, `Group`, `Duration`, `Kind`, `Attributes`).
- `ContentKind` (`Live`, `Movie`, `Episode`) and `DefaultContentKindClassifier`.
- `PlaylistClient` over a caller-owned `HttpClient`, with `PlaylistClientOptions` for the User-Agent. The default User-Agent is `AmiIptvParser/1.0`. An optional `ILogger<PlaylistClient>` may be passed; the playlist URI is not written to logs.
- `PlaylistEntryExtensions`: `Live`, `Movies`, `Episodes`, `Groups`, and `InGroup`.
- Parser behavior that 0.0.x got wrong: an `#EXTINF` line with no attributes is kept; the title is the text after the first comma that sits outside quotes; a leading BOM is accepted; an HLS `.m3u8` manifest (query string ignored) is live; Xtream `/movie/` and `/series/` paths are movie and episode; an `SxxEyy` title on a video file is an episode.

### Tests

- The suite is xUnit v3 with AwesomeAssertions and NSubstitute.
- Playlist cases are embedded fixtures (no attributes, comma in the title, BOM, CRLF, live `.m3u8`, Xtream movie and series, corrupt lines). Tests do not call the network.
- `PlaylistClient` is exercised with a fake `HttpMessageHandler`.

### Packaging

- Version is calculated by MinVer from `v*` tags (`MinVerMinimumMajorMinor` is `1.0`).
- The README is packed as the NuGet readme. Symbols ship as a `.snupkg` with SourceLink.
- GitHub Actions restores, builds, tests, and packs on `main` and pull requests. A `v*` tag push publishes to nuget.org with trusted publishing (OIDC). See [docs/publishing.md](docs/publishing.md).
