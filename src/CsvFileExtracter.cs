using System.IO.Compression;

public static class CsvFilesExtracter
{
    public static string Extract(string searchPath, string extractDirectory, string fileName)
    {
        Directory.CreateDirectory(extractDirectory);
        var filesWithSameName = ExtractFilesWithSameName(searchPath,fileName,extractDirectory);
        var csvFile = Path.Combine(extractDirectory,fileName);
        CsvHelper2.Merge(filesWithSameName,csvFile);
        CsvHelper2.Validate(csvFile);
        return csvFile;
    }
    public static string[] GetFileNames(string searchPath)
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
            .Select(x => Path.GetFileName(x))
            .ToArray();
    }
    private static string[] ExtractFilesWithSameName(string searchPath, string fileName, string extractDirectory)
    {
        var zipPaths = Directory.GetFiles(searchPath,"*.zip",SearchOption.AllDirectories);
        var list = new List<string>();
        for (int i = 0; i < zipPaths.Length; i++)
        {
            var zipFile = zipPaths[i];
            var targetFile = Path.Combine(extractDirectory,fileName)+$".{i}";
            using var zip = ZipFile.OpenRead(zipFile);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name != fileName) continue;
                entry.ExtractToFile(targetFile,true);
                list.Add(targetFile);
                i++;
            }
        }
        return list.ToArray();
    }
}