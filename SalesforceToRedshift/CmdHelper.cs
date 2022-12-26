using System.Diagnostics;

public static class CmdHelper
{
    public static string Run(string command)
    {
        var processInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
        processInfo.CreateNoWindow = false;
        processInfo.UseShellExecute = false;
        processInfo.RedirectStandardError = true;
        processInfo.RedirectStandardOutput = true;
        var process = Process.Start(processInfo)!;
        var result = ReadToEnd(process.StandardOutput);
        if (string.IsNullOrEmpty(result))
            result = ReadToEnd(process.StandardError);
        process.Close();
        return result;
    }
    private static string ReadToEnd(StreamReader streamReader)
    {
        var result = streamReader.ReadToEnd();
        streamReader.Dispose();
        return result;
    }
}