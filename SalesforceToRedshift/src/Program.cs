
public static class Program
{
    private static void Main(string[] args)
    {
        var config = Config.Get(args);

        Directory.CreateDirectory(config.LocalWorkingDirectory);
        using var redshiftClient = new RedshiftClient(config.ConfigRedshift);
        using var s3Client = new S3Client(config.ConfigS3);
        var sfdxClient = SalesForceClient.CreateFromSFDX(config.SalesforceUserName);
        var fileNamesDone = File.ReadAllLines(config.FileNamesDoneFile);
        var fileNamesTodo = CsvFilesExtracter.GetFileNames(config.LocalZipFilesSearchPath).Except(fileNamesDone);

        foreach (var fileNameTodo in fileNamesTodo)
        {
            Console.WriteLine($"Migrating {fileNameTodo}");

            try
            {
                var csvFile = CsvFilesExtracter.Extract(config.LocalZipFilesSearchPath,config.LocalWorkingDirectory,fileNameTodo);

                if (File.ReadLines(csvFile).Count() <= 1)
                {
                    File.AppendAllLines(config.FileNamesDoneFile, new [] { fileNameTodo } );
                    continue;
                }

                var fileName = Path.GetFileName(csvFile);
                var tableName = Path.GetFileNameWithoutExtension(csvFile);
                var lines = MigrationLogic.CreateSqlCreateLines(tableName, sfdxClient);
                redshiftClient.CreateSchemaIfNotExists(config.ConfigRedshift.SchemaName);
                redshiftClient.DropAndCreateTable(config.ConfigRedshift.SchemaName, tableName, lines);

                var s3file = $"{config.S3WorkingDirectory}/{fileName}";
                s3Client.UploadFile(csvFile,s3file);
                redshiftClient.Copy(config.ConfigRedshift.SchemaName,tableName,s3file,config.ConfigS3);

                File.AppendAllLines(config.FileNamesDoneFile, new [] { fileNameTodo } );
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
            }
        }
    }
}