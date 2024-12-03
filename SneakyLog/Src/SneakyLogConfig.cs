namespace SneakyLog;

public class SneakyLogConfig
{
    public bool LogDebugTrace { get; set; } = false;
    public bool DataTraceEnabled { get; set; } = true;
    public bool LogData { get; set; } = false;
    public bool UseEmojis { get; set; } = true;
}