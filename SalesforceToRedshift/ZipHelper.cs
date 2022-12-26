using System.IO.Compression;

public static class ZipHelper
{
    public static string[] GetFilesZip(string dirPath) => Directory.GetFiles(dirPath,"*.zip",SearchOption.AllDirectories);
    public static string[] ExtractFileInParts(string[] zipPaths, string fileName, string extractPath)
    {
        Console.WriteLine("Extracting files in parts");
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
    public static bool TryExtractFile(string zipFilePath, string filePath_target, string fileName)
    {
        using var zip = ZipFile.OpenRead(zipFilePath);
        foreach (var entry in zip.Entries)
        {
            if (entry.Name == fileName)
            {
                if (File.Exists(filePath_target))
                    File.Delete(filePath_target);

                Console.WriteLine($"Extracting file {fileName} from {zipFilePath} to {filePath_target}");
                entry.ExtractToFile(filePath_target);
                return true;
            }
        }
        return false;
    }
    public static string[] GetDistinctFileNamesFromZips(string searchPath)
    {
        return
            ZipHelper.GetFilesZip(searchPath)
            .Select(x => GetFileNames(x))
            .SelectMany(x => x)
            .Distinct()
            .ToArray();
    }
    public static string[] GetFileNames(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return zip.Entries.Select(x => x.Name).ToArray();
    }
    public static string[] GetDublicateNames(string[] zipPaths)
    {
        return zipPaths
        .Select(x =>
        {
            using var zip = ZipFile.OpenRead(x);
            return zip.Entries.Select(x => x.Name).ToArray();
        })
        .SelectMany(x => x)
        .GroupBy(x => x)
        .Select(x => (x.Key,x.Count()))
        .Where(x => x.Item2 > 1)
        .Select(x => x.Key)
        .ToArray();
    }
}