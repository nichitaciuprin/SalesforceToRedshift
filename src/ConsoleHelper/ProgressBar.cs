using System.Text;

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
		if (Console.IsOutputRedirected) return;
        ResetTimer();
	}
    public void ReportIteration(int index, int length)
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
    private static string Message(double percentDone, int blockCount)
    {
        var progressBlockCount = (int) (percentDone * blockCount);
        var cages = new string('#', progressBlockCount);
        var dashes = new string('-', blockCount - progressBlockCount);
        var percentStr = (percentDone * 100).ToString("0.000");
        return $"[{cages}{dashes}] {percentStr}%";
    }
	private void TimerHandler(object? state)
    {
        if (disposed) return;
		lock (timer)
        {
            var text = Message(currentProgress,blockCount);
            var animationChar = animation[animationIndex++ % animation.Length];
			UpdateText($"{text} {animationChar}");
			ResetTimer();
		}
	}
	private void UpdateText(string text)
    {
		int commonPrefixLength = 0;
		int commonLength = Math.Min(currentText.Length, text.Length);
		while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
			commonPrefixLength++;
		var outputBuilder = new StringBuilder();
		outputBuilder.Append('\b', currentText.Length - commonPrefixLength);
		outputBuilder.Append(text.AsSpan(commonPrefixLength));
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