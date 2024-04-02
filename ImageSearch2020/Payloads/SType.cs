namespace ImageSearch2020.Payloads;
public class SType
{
    private static readonly SType _handle = new("handle");
    private static readonly SType _class = new("class");
    private static readonly SType _title = new("title");

    public static SType Handle => _handle;
    public static SType Class => _class;
    public static SType Title => _title;

    private readonly string _value;
    public string Value => _value;

    private SType(string value)
    {
        _value = value;
    }

    public override string ToString()
    {
        return _value;
    }
}
