using System.IO.Compression;

namespace AmiIptvParser;

public class LocalLogger : Logger
{
    private const string LogFolderName = "Logs";
    private const string LogFileName = "amiiptvparser.log";
    private const int LogChunkSize = 1000000;
    private const int LogChunkMaxCount = 5;
    private const int LogArchiveMaxCount = 20;
    private const int LogCleanupPeriod = 7;

    private string _basePath;
    public void SetBasePath(string value)
    {
        _basePath = value;
    }

    protected override void CreateLog(string message)
    {
        var logFolderPath = Path.Combine((string.IsNullOrEmpty(_basePath)) ? Path.GetTempPath(): _basePath, LogFolderName);
        if (!Directory.Exists(logFolderPath))
            Directory.CreateDirectory(logFolderPath);

        var logFilePath = Path.Combine(logFolderPath, LogFileName);

        Rotate(logFilePath);

        using var sw = File.AppendText(logFilePath);
        sw.WriteLine(message);
    }
    private void Rotate(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length < LogChunkSize)
            return;

        var fileTime = DateTime.Now.ToString("dd_MM_yy_h_m_s");
        var rotatedPath = filePath.Replace(".log", $".{fileTime}");
        File.Move(filePath, rotatedPath);

        var folderPath = Path.GetDirectoryName(rotatedPath) ?? throw new AmiIptvException("Path log not found", 580);
        var logFolderContent = new DirectoryInfo(folderPath).GetFileSystemInfos();

        var chunks = logFolderContent.Where(x => !x.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)).ToList();

        if (chunks.Count <= LogChunkMaxCount)
            return;

        var archiveFolderInfo = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(rotatedPath) ?? throw new AmiIptvException("Path log not found", 580), $"{LogFolderName}_{fileTime}"));

        foreach (var chunk in chunks)
        {
            Directory.Move(chunk.FullName, Path.Combine(archiveFolderInfo.FullName, chunk.Name));
        }

        ZipFile.CreateFromDirectory(archiveFolderInfo.FullName, Path.Combine(folderPath, $"{LogFolderName}_{fileTime}.zip"));
        Directory.Delete(archiveFolderInfo.FullName, true);

        var archives = logFolderContent.Where(x => x.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)).ToArray();

        if (archives.Length <= LogArchiveMaxCount)
            return;

        var oldestArchive = archives.OrderBy(x => x.CreationTime).First();
        var cleanupDate = oldestArchive.CreationTime.AddDays(LogCleanupPeriod);
        if (DateTime.Compare(cleanupDate, DateTime.Now) <= 0)
        {
            foreach (var file in logFolderContent)
            {
                file.Delete();
            }
        }
        else
        {
            File.Delete(oldestArchive.FullName);
        }
    }
}