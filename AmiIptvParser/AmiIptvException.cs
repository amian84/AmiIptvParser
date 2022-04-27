namespace AmiIptvParser;


public class AmiIptvException : Exception
{
    public int Code { get; set; }
    public AmiIptvException(){}

    public AmiIptvException(string message, int code) :base(message)
    {
        Code = code;
    }
}