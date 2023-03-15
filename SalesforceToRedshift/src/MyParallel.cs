public static class MyParallel
{
    public static void ForEachSequential<TItem>(TItem[] items, int taskCount, Func<TItem,Task> body)
    {
        if (items.Length == 0) return;
        if (taskCount <= 0) return;
        taskCount = Math.Clamp(taskCount,1,items.Length);
        var progressBar = new ProgressBar();
        var tasks = Enumerable.Repeat(Task.CompletedTask, taskCount).ToArray();
        for (var i = 0; i < items.Length; i++)
        {
            var taskIndex = Task.WaitAny(tasks);
            var task = tasks[taskIndex];
            if (task.Exception != null)
                throw task.Exception;

            progressBar.ReportIteration(i,items.Length);

            var item = items[i];
            tasks[taskIndex] = body.Invoke(item);
        }
        progressBar.Dispose();
        Task.WaitAll(tasks);
    }
    public static void ForEachSequentialFilesDownload<TItem>(TItem[] items, int taskCount, Func<object?, FileInfo?> body)
    {
        var taskFactory = new TaskFactory<FileInfo?>();
        if (taskCount < 0) throw new ArgumentException("taskCount must be >= 0");
        if (taskCount == 0) return;
        taskCount = Math.Clamp(taskCount,1,items.Length);
        var oldTime = DateTime.Now;
        var byteCount = 0L;
        var tasks = new Task<FileInfo?>[taskCount];
        for (var i = 0; i < taskCount; i++)
        {
            var item = items[i];
            tasks[i] = taskFactory.StartNew(body,item);
        }
        for (var itemIndex = taskCount; itemIndex < items.Length; itemIndex++)
        {
            var taskIndex = Task.WaitAny(tasks);
            var task = tasks[taskIndex];
            if (task.Exception != null)
                throw task.Exception;

            if (task.Result != null)
                byteCount += task.Result.Length;
            var newTime = DateTime.Now;

            Console.WriteLine((itemIndex,items.Length,oldTime,newTime,byteCount));

            var item = items[itemIndex];
            tasks[taskIndex] = taskFactory.StartNew(body,item);
        }
        Task.WaitAll(tasks);
    }
}