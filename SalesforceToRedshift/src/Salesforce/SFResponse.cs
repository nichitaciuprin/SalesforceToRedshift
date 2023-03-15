#nullable disable warnings

namespace SFResponse;

public class ObjectInfo
{
    public readonly string Name;
    public readonly string Label;
    public readonly bool Queryable;
    public readonly FieldInfo[] Fields;
}
public class FieldInfo
{
    public readonly string Name;
    public readonly int ByteLength;
    public readonly string SoapType;
    public readonly string Type;
}