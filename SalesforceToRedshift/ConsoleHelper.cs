using System.Text;

public static class ConsoleHelper
{
    public static void WriteLineWarning(string msg)
    {
        var rem = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(msg);
        Console.ForegroundColor = rem;
    }
    public static void ReadYes()
    {
        while (true)
        {
            Console.WriteLine("OK?");
            var response = Console.ReadLine();
            if (response == null) continue;
            if (response.ToLower() != "yes") continue;
            break;
        }
    }
}
public class ProgressBar : IDisposable, IProgress<double>
{
	private const int blockCount = 10;
	private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
	private const string animation = @"|/-\";
	private readonly Timer timer;
	private double currentProgress = 0;
	private string currentText = string.Empty;
	private bool disposed = false;
	private int animationIndex = 0;

	public ProgressBar()
    {
		timer = new Timer(TimerHandler);
		// Avoid writing to file
		if (Console.IsOutputRedirected) return;
        ResetTimer();
	}
    public void ReportFull()
    {
        if (disposed) return;
        var value = 1f;
        value = Math.Clamp(value,0,1);
		Interlocked.Exchange(ref currentProgress, value);
	}
    public void ReportInteration(int index, int length)
    {
        if (disposed) return;
        var value = (float)index/length;
        value = Math.Clamp(value,0,1);
		Interlocked.Exchange(ref currentProgress, value);
	}
	public void Report(double value)
    {
        if (disposed) return;
        value = Math.Clamp(value,0,1);
		Interlocked.Exchange(ref currentProgress, value);
	}
    public void Dispose()
    {
        if (disposed) return;
		lock (timer)
        {
			disposed = true;
			UpdateText(string.Empty);
		}
	}
	private void TimerHandler(object? state)
    {
        if (disposed) return;
		lock (timer)
        {
			var progressBlockCount = (int) (currentProgress * blockCount);
			var percent = string.Format("{0,3}",(int)(currentProgress * 100));
            var cages = new string('#', progressBlockCount);
            var dashes = new string('-', blockCount - progressBlockCount);
            var animationChar = animation[animationIndex++ % animation.Length];
			// var text = string.Format("[{0}{1}] {2,3}% {3}",cages,dashes,percent,animationChar);
            var text = $"[{cages}{dashes}] {percent}% {animationChar}";
			UpdateText(text);
			ResetTimer();
		}
	}
	private void UpdateText(string text)
    {
		// Get length of common portion
		int commonPrefixLength = 0;
		int commonLength = Math.Min(currentText.Length, text.Length);
		while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
			commonPrefixLength++;
		var outputBuilder = new StringBuilder();
		outputBuilder.Append('\b', currentText.Length - commonPrefixLength);
		outputBuilder.Append(text.Substring(commonPrefixLength));
		int overlapCount = currentText.Length - text.Length;
		if (overlapCount > 0)
        {
			outputBuilder.Append(' ', overlapCount);
			outputBuilder.Append('\b', overlapCount);
		}
		Console.Write(outputBuilder);
		currentText = text;
	}
	private void ResetTimer()
    {
		timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
	}
}