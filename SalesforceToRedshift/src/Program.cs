using System.Text.Json;
using System.IO.Compression;

public static class Program
{
    private static void Main(string[] args)
    {
        var config = GetConfig(args);
        var tableNames = FindTables(config.LocalZipFilesSearchPath);
        foreach (var tableName in tableNames)
        {
            try
            {
                Migrate(config,tableName);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }
        }
    }
    private static void Migrate(Config config, string tableName)
    {
        Directory.CreateDirectory(config.LocalWorkingDirectory);
        var saveFilePath = Path.Combine(config.LocalWorkingDirectory,"Save.txt");
        if (FileHelper.SaveExists(saveFilePath,tableName))
        {
            ConsoleHelper.WriteLineWarning($"{tableName} is already migrated");
            return;
        }
        Console.WriteLine("Starting migration task");
        var fileName = tableName+".csv";
        var csvFile = ExtractCsvFile(config.LocalZipFilesSearchPath,fileName,config.LocalZipFilesSearchPath);
        CsvHelper2.Validate(csvFile);
        if (FileHelper.HasMoreThen1Line(csvFile))
            MigrateCsvFile(config,csvFile);
        else
            Console.WriteLine($"SKIPING");
        Console.WriteLine($"DONE");
        FileHelper.SaveCreate(saveFilePath,tableName);
    }
    private static void MigrateCsvFile(Config config, string csvFile)
    {
        using var redshiftClient = new RedshiftClient(config.ConfigRedshift);
        using var s3Client = new S3Client(config.ConfigS3);
        var sfdxClient = SalesForceClient.CreateFromSFDX(config.SalesforceUserName);

        var fileName = Path.GetFileName(csvFile);
        var s3filePath = $"{config.S3WorkingDirectory}/{fileName}";
        var tableName = Path.GetFileNameWithoutExtension(csvFile);

        var lines = MigrationLogic.CreateSqlCreateLines(tableName, sfdxClient);
        redshiftClient.CreateSchemaIfNotExists(config.ConfigRedshift.SchemaName);
        redshiftClient.DropAndCreateTable(config.ConfigRedshift.SchemaName, tableName, lines);
        s3Client.UploadFile(csvFile,s3filePath);
        redshiftClient.Copy(config.ConfigRedshift.SchemaName,tableName,s3filePath,config.ConfigS3);
    }
    private static string ExtractCsvFile(string searchPath, string fileName, string extractPath)
    {
        var zipFiles = Directory.GetFiles(searchPath,"*.zip",SearchOption.AllDirectories);
        var fileParts = ExtractFileInParts(zipFiles,fileName,extractPath);
        var csvFile = Path.Combine(extractPath,fileName);
        CsvHelper2.Merge(fileParts,csvFile);
        return csvFile;
    }
    private static string[] FindTables(string searchPath)
    {
        return
            Directory.GetFiles(searchPath,"*.zip",SearchOption.AllDirectories)
            .Select(x =>
            {
                using var zip = ZipFile.OpenRead(x);
                return zip.Entries.Select(x => x.Name).ToArray();
            })
            .SelectMany(x => x)
            .Distinct()
            .Where(x => Path.GetExtension(x) == ".csv")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .ToArray();
    }
    private static Config GetConfig(string[] args)
    {
        var filePath = args[0];
        var reader = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<Config>(reader);
        return config!;
    }
    private static string[] ExtractFileInParts(string[] zipPaths, string fileName, string extractPath)
    {
        Console.WriteLine("Extracting file parts");
        var list = new List<string>();
        for (int i = 0; i < zipPaths.Length; i++)
        {
            var zipPath = zipPaths[i];
            var filePath_target = Path.Combine(extractPath,fileName)+$".{i}";
            if (TryExtractFile(zipPath,filePath_target,fileName))
                list.Add(filePath_target);
        }
        return list.ToArray();
    }
    private static bool TryExtractFile(string zipFilePath, string targetFilePath, string fileName)
    {
        using var zip = ZipFile.OpenRead(zipFilePath);
        foreach (var entry in zip.Entries)
        {
            if (entry.Name != fileName) continue;

            if (File.Exists(targetFilePath))
                File.Delete(targetFilePath);

            Console.WriteLine($"Extracting file {fileName} from {zipFilePath} to {targetFilePath}");
            entry.ExtractToFile(targetFilePath);
            return true;
        }
        return false;
    }
}