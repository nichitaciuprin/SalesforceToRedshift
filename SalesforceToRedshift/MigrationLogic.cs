public static class MigrationLogic
{
    public static string[] ColumnLines(string schemaName, string tableName, string csvFile, RedshiftClient redshiftClient, SfdxClient sfdxClient)
    {
        Console.WriteLine("Generating SQL. DropAndCreateTable");
        var allLines = GetTableInfo(schemaName,tableName,csvFile,sfdxClient);

        var max = allLines.Select(x => x.Item3).Max();
        if (max > RedshiftClient.VarcharMaxBytesCount)
            throw new Exception($"{max} > RedshiftHelper.VarcharMaxBytesCount");

        allLines = allLines
            .Select(x =>
            {
                x.Item3 = Math.Max(1,x.Item3);
                return x;
            })
            .ToArray();

        var duno = allLines.Select(x =>
        {
            return x.Item2 == "VARCHAR" ?
                $"{x.Item1} {x.Item2}({x.Item3})" :
                $"{x.Item1} {x.Item2}";
        })
        .ToArray();

        return duno;
    }
    private static (string,string,int)[] GetTableInfo(string schemaName, string tableName, string csvFilePath, SfdxClient sfdxClient)
    {
        var lines1 = GetTableInfo1(csvFilePath);
        var lines2 = GetTableInfo2(tableName,sfdxClient);

        return (lines1,lines2) switch
        {
            (null,null) => throw new Exception("Did not get any table info"),
            (_   ,null) => lines1,
            (null,   _) => lines2,
            (_   ,   _) =>
                lines1
                .Select(line1 =>
                {
                    foreach (var line2 in lines2)
                        if (line1.Item1 == line2.Item1)
                        {
                            line1.Item2 = line2.Item2;
                            break;
                        }
                    return line1;
                })
                .ToArray()
        };
    }
    private static (string,string,int)[]? GetTableInfo1(string filePath_csvFile)
    {
        var result = CsvHelper2.GetTableInfo(filePath_csvFile);
        result = CsvHelper2.SplitColumnInfos(result,RedshiftClient.VarcharMaxBytesCount);
        return result
            .Select(x =>
            {
                var name = $"\"{x.Item1}\"";
                var type = $"VARCHAR";
                var byteLength = x.Item2;
                return (name,type,byteLength);
            })
            .ToArray();
    }
    private static (string,string,int)[]? GetTableInfo2(string tableName, SfdxClient sfdxClient)
    {
        var tableInfo = sfdxClient.GetTableInfo(tableName);
        if (tableInfo == null) return null;
        return tableInfo.fields
            .Select(x =>
            {
                var name = $"\"{x.name}\"";
                var type = Convert(x.soapType);
                var byteLength = x.byteLength;
                return (name,type,byteLength);
            })
            .ToArray();
    }
    private static string Convert(string salesforceColumnType)
    {
        return salesforceColumnType switch
        {
            "tns:ID"       => $"VARCHAR",
            "xsd:int"      => $"INTEGER",
            "xsd:double"   => $"FLOAT8",
            "xsd:boolean"  => $"BOOL",
            "xsd:dateTime" => $"TIMESTAMP",
            "xsd:date"     => $"TIMESTAMP",
            "xsd:time"     => $"INTEGER",
            "xsd:string"   => $"VARCHAR",
            "urn:address"  => $"BOOL", // all values will be null
            _              => throw new Exception($"Field {salesforceColumnType} was not handled")
        };
    }
}