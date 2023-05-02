using SFResponse;

public static class MigrationLogic
{
    public static string[] CreateSqlCreateLines(string tableName, SalesForceClient salesForceClient)
    {
        Console.WriteLine("Generating SQL. DropAndCreateTable");
        return salesForceClient
            .Describe(tableName)
            .Fields.Select(x => Convert(x))
            .ToArray();
    }
    private static string Convert(FieldInfo fieldInfo)
    {
        return fieldInfo.SoapType switch
        {
            "tns:ID"           => $"\"{fieldInfo.Name}\" VARCHAR",
            "xsd:int"          => $"\"{fieldInfo.Name}\" INTEGER",
            "xsd:time"         => $"\"{fieldInfo.Name}\" INTEGER",
            "xsd:double"       => $"\"{fieldInfo.Name}\" FLOAT8",
            "xsd:boolean"      => $"\"{fieldInfo.Name}\" BOOL",
            "xsd:dateTime"     => $"\"{fieldInfo.Name}\" TIMESTAMP",
            "xsd:date"         => $"\"{fieldInfo.Name}\" TIMESTAMP",
            "xsd:string"       => $"\"{fieldInfo.Name}\" VARCHAR",
            "urn:address"      => $"\"{fieldInfo.Name}\" BOOL", // all values will be null
            "xsd:base64Binary" => $"\"{fieldInfo.Name}\" VARCHAR({fieldInfo.ByteLength})",
            "xsd:anyType"      => $"\"{fieldInfo.Name}\" VARCHAR({fieldInfo.ByteLength})",
            _                  => throw new Exception($"Field was not handled")
        };
    }
}