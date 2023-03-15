public static class FileHelper
{
    private static readonly object lockObject = new();
    public static bool HasMoreThen1Line(string filePath)
    {
        using var reader = new StreamReader(filePath);
        for (int i = 0; i < 2; i++)
            if (reader.ReadLine() == null)
                return false;
        return true;
    }
    public static void SaveCreate(string saveFilePath, string saveName)
    {
        lock (lockObject)
        {
            File.AppendAllLines(saveFilePath, new[]{saveName});
        }
    }
    public static bool SaveExists(string saveFilePath, string saveName)
    {
        lock (lockObject)
        {
            if (!File.Exists(saveFilePath)) return false;
            var lines = File.ReadAllLines(saveFilePath);
            return lines.Contains(saveName);
        }
    }
    public static int CountLines(string filePath)
    {
        var lineCount = 0;
        using var reader = File.OpenText(filePath);
        while (reader.ReadLine() != null)
            lineCount++;
        return lineCount;
    }
    public static string GetLargestFile(params string[] paths)
    {
        if (paths.Length == 0)
            throw new InvalidOperationException("input is empty");

        return paths
            .Select(x => (x,FileSizeInBytes(x)))
            .MaxBy(x => x.Item2)
            .x;
    }
    public static long FileSizeInBytes(string filePath) => new FileInfo(filePath).Length;
    public static void AppendTo(string sourceFilePath, string targetFilePath, int skipLinesCount)
    {
        var index = 0;
        var lineCount = CountLines(sourceFilePath);
        Console.WriteLine($"Appending file. {sourceFilePath} -> {targetFilePath}");
        var progressBar = new ProgressBar();

        using var streamReader = new StreamReader(sourceFilePath);
        using var streamWriter = new StreamWriter(targetFilePath,true);

        while (true)
        {
            progressBar.ReportIteration(index,lineCount);
            var line = streamReader.ReadLine();
            if (line == null) break;
            if (skipLinesCount > 0)
                skipLinesCount--;
            else
                streamWriter.WriteLine(line);
            index++;
        }

        progressBar.Dispose();
    }
    public static void TryAppendLines(IEnumerable<string> lines, string targetFilePath)
    {
        using var streamWriter = new StreamWriter(targetFilePath);
        foreach (var line in lines)
            streamWriter.WriteLine(line);
    }
}