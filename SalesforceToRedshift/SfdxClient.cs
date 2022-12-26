using System.Text.Json;

// TODO. Use sfdx only for geting token.
// TODO. Use RESTAPI for tables info
// TODO. Assert that sfdx is installed and have auth
public class SfdxClient
{
    private readonly string userName;

    public SfdxClient(string userName)
    {
        this.userName = userName;
    }
    public SFTableInfo? GetTableInfo(string tableName)
    {
        Console.WriteLine($"SFDX. Geting table info \"{tableName}\"");
        var command = $"sfdx force:schema:sobject:describe -s {tableName} -u {userName}";
        var output = CmdHelper.Run(command);
        try
        {
            return JsonSerializer.Deserialize<SFTableInfo>(output);
        }
        catch (System.Exception exc)
        {
            if (output.StartsWith("'sfdx' is not recognized"))
                throw new Exception("SFDX is not installed");

            if (output.Contains("No authorization information found"))
                throw new Exception($"SFDX has no auth for {userName}. Auth with sfdx like this: sfdx force:auth:web:login -r https://test.salesforce.com");

            if (output.StartsWith("ERROR running force:schema:sobject:describe:  The requested resource does not exist"))
            {
                ConsoleHelper.WriteLineWarning($"SFDX. Found no info for table \"{tableName}\"");
                return null;
            }

            throw new Exception(output,exc);
        }
    }
    // private void Validate()
    // {
    //     var command = $"sfdx force:org:display --targetusername {userName} --json";
    //     var output = CmdHelper.Run(command);
    //     if (output.Contains(""))
    // }
}
public class SFTableInfo
{
    public string name { get; set; }
    public SFColumnInfo[] fields { get; set; }
    public SFTableInfo(string name, SFColumnInfo[] fields)
    {
        this.name = name;
        this.fields = fields;
    }
}
public class SFColumnInfo
{
    public string name { get; set; }
    public int byteLength { get; set; }
    public string soapType { get; set; }
    public SFColumnInfo(string name, int byteLength, string soapType)
    {
        this.name = name;
        this.byteLength = byteLength;
        this.soapType = soapType;
    }
}