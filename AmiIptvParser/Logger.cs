using System.Globalization;

namespace AmiIptvParser;

public abstract class Logger:  IDisposable
{
    private readonly Queue<Action> _queue = new Queue<Action>();
    private readonly ManualResetEvent _hasNewItems = new ManualResetEvent(false);
    private readonly ManualResetEvent _terminate = new ManualResetEvent(false);
    private readonly ManualResetEvent _waiting = new ManualResetEvent(false);
    private readonly Thread _loggingThread;

    private static readonly Dictionary<LoggerType, Lazy<Logger>> _instances = new Dictionary<LoggerType, Lazy<Logger>>();

    public static Logger GetLogger(LoggerType type)
    {
        if (_instances.ContainsKey(type))
        {
            return _instances[type].Value;
        }

        Lazy<Logger> logger;
        switch (type)
        {
            case LoggerType.Local:
            default:
                logger = new Lazy<Logger>(() => new LocalLogger());
                break;
            
        }

        return logger.Value;
    }
    protected Logger()
    {
        _loggingThread = new Thread(new ThreadStart(ProcessQueue)) { IsBackground = true };
        _loggingThread.Start();
    }

    private void ProcessQueue()
    {
        while (true)
        {
            _waiting.Set();
            int i = WaitHandle.WaitAny(new WaitHandle[] { _hasNewItems, _terminate });
            if (i == 1) return;
            _hasNewItems.Reset();
            _waiting.Reset();

            Queue<Action> queueCopy;
            lock (_queue)
            {
                queueCopy = new Queue<Action>(_queue);
                _queue.Clear();
            }

            foreach (var log in queueCopy)
            {
                log();
            }
        }
    }
    
    
    private void Log(string message, LogType logType)
    {
        if (string.IsNullOrEmpty(message))
            return;

        var logRow = ComposeLogRow(message, logType);
        System.Diagnostics.Debug.WriteLine(logRow);

        lock (_queue)
            _queue.Enqueue(() => CreateLog(logRow));

        _hasNewItems.Set();
			
    }
    
    public void Info(string message)
    {
        Log(message, LogType.Info);
    }

    public void Debug(string message)
    {
        Log(message, LogType.Debug);
    }

    public void Error(string message)
    {
        Log(message, LogType.Error);
    }

    public void Error(Exception e)
    {
        Log(UnwrapExceptionMessages(e), LogType.Error);
			
    }

    public override string ToString() => $"Logger settings: [Type: {this.GetType().Name}";

    protected abstract void CreateLog(string message);

    public void Flush() => _waiting.WaitOne();

    public void Dispose()
    {
        _terminate.Set();
        _loggingThread.Join();
    }

    protected virtual string ComposeLogRow(string message, LogType logType) =>
        $"[{DateTime.Now.ToString(CultureInfo.InvariantCulture)} {logType}] - {message}";

    protected virtual string UnwrapExceptionMessages(Exception ex)
    {
        return ex == null ? string.Empty : $"{ex}, Inner exception: {UnwrapExceptionMessages(ex.InnerException)} ";
    }

}

public enum LogType
{
    Info,
    Debug,
    Error
}

public enum LoggerType
{
    Local,
    
}