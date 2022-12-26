using System.Text.Json;

public static class Program
{
    private static void Main(string[] args)
    {
        var filePath = args[0];
        var reader = File.ReadAllText(filePath);
        var config = JsonSerializer.Deserialize<Config>(reader);
        if (config == null)
            throw new Exception("Could not read config file");
        Helper.PrintProperties(config);
        Migrate(config);
    }
    private static void Migrate(Config config)
    {
        if (!Directory.Exists(config.DirPath))
            Directory.CreateDirectory(config.DirPath);

        var tableNames =
            ZipHelper.GetFilesZip(config.ZipFilesSearchPath)
            .Select(x => ZipHelper.GetFileNames(x))
            .SelectMany(x => x)
            .Distinct()
            .Where(x => Path.GetExtension(x) == ".csv")
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .ToArray();

        foreach (var tableName in tableNames)
        {
            try
            {
                Migrate(config,tableName);
            }
            catch (System.Exception exc)
            {
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }
        }
    }
    private static void Migrate(Config config, string tableName)
    {
        Console.WriteLine();
        var saveFilePath = Path.Combine(config.DirPath,"Save.txt");
        if (FileHelper.SaveExists(saveFilePath,tableName))
        {
            ConsoleHelper.WriteLineWarning($"{tableName} is already migrated");
            return;
        }

        Console.WriteLine("Starting migration task");
        var fileName = tableName+".csv";
        var csvFile = ExtractCsvFile(config.ZipFilesSearchPath,fileName,config.DirPath);
        FileHelper.PrintFileSize(csvFile);
        using var redshiftClient = new RedshiftClient(config.RedshiftConfig);
        using var s3Client = new S3Client(config.S3Config);
        var sfdxClient = new SfdxClient(config.SalesforceUserName);
        CsvHelper2.Validate(csvFile);
        var oldCsvFile = csvFile;
        var newCsvFile = CsvHelper2.SplitByColumnSizeIfLargeValueExists(csvFile,RedshiftClient.VarcharMaxSizeInBytes);
        csvFile = newCsvFile == null ? oldCsvFile : newCsvFile;
        redshiftClient.CreateSchemaIfNotExists(config.RedsfhitTargetSchemaName);
        var lines = MigrationLogic.ColumnLines(tableName, csvFile, sfdxClient);
        redshiftClient.DropAndCreateTable(config.RedsfhitTargetSchemaName, tableName, lines);
        s3Client.UploadFile(fileName,csvFile);
        var s3filePath = $"{config.S3Config.BucketName}/{fileName}";
        redshiftClient.Copy(config.RedsfhitTargetSchemaName,tableName,s3filePath,config.S3Config);
        if (newCsvFile != null)
            File.Delete(newCsvFile);
        File.Delete(oldCsvFile);
        FileHelper.SaveCreate(saveFilePath,tableName);
        Console.WriteLine($"DONE");
    }
    private static string ExtractCsvFile(string searchPath, string fileName, string extractPath)
    {
        var zipFiles = ZipHelper.GetFilesZip(searchPath);
        var fileParts = ZipHelper.ExtractFileInParts(zipFiles,fileName,extractPath);
        var csvFile = Path.Combine(extractPath,fileName);
        CsvHelper2.Merge(fileParts,csvFile);
        return csvFile;
    }
}