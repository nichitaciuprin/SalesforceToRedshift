using System.Net.Http.Headers;
using System.Text.Json;
using SFResponse;

public class SalesForceClient
{
    private const int version = 56;
    private readonly string instanceUrl;
    private readonly HttpClient httpClient;

    public static SalesForceClient CreateFromSFDX(string userName)
    {
        var command = $"sfdx force:org:display -u {userName} --json";
        var output = CmdHelper.Run(command);
        var json = JsonDocument.Parse(output).RootElement;
        var result = json.GetProperty("result");
        var accessToken = result.GetProperty("accessToken").GetString()!;
        var instanceUrl = result.GetProperty("instanceUrl").GetString()!;
        return new SalesForceClient(accessToken,instanceUrl);
    }
    public SalesForceClient(string accessToken, string instanceUrl)
    {
        this.instanceUrl = instanceUrl;
        httpClient = new HttpClient
        {
            Timeout = new TimeSpan(1, 0, 0)
        };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.Add("X-SFDC-Session", accessToken);
    }
    public ObjectInfo Describe(string objectName)
    {
        try { return DescribeFromCache(objectName); } catch (Exception) {}
        return DescribeNoCache(objectName);
    }
    public ObjectInfo DescribeNoCache(string objectName)
    {
        var path = GetCatchedDescribe(objectName);
        var url = $"{instanceUrl}/services/data/v{version}.0/sobjects/{objectName}/describe";
        var task1 = httpClient.GetAsync(url); task1.Wait(); var response = task1.Result;
        var task2 = response.Content.ReadAsStringAsync(); task2.Wait(); var json = task2.Result;
        File.WriteAllText(path,json);
        return JsonSerializer.Deserialize<ObjectInfo>(json)!;
    }
    public static ObjectInfo DescribeFromCache(string objectName)
    {
        var path = GetCatchedDescribe(objectName);
        var text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ObjectInfo>(text)!;
    }
    private static string GetCatchedDescribe(string objectName)
    {
        return @$"{Directory.GetCurrentDirectory()}\cache\SF\Describe\{objectName}.json";
    }
}