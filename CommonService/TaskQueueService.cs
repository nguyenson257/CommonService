using System.Threading.Channels;

namespace CommonService
{
    public class TaskQueueService
    {
        private readonly Channel<Func<Task>> _taskChannel;
        private readonly SemaphoreSlim _semaphore;
        private readonly TaskCompletionSource _completionSource;

        public TaskQueueService(int maxConcurrency)
        {
            _taskChannel = Channel.CreateUnbounded<Func<Task>>();
            _semaphore = new SemaphoreSlim(maxConcurrency); // Control concurrent I/O operations
            _completionSource = new TaskCompletionSource();

            // Start background workers to process tasks
            for (int i = 0; i < maxConcurrency; i++)
            {
                _ = Task.Run(() => ProcessTasksAsync());
            }
        }

        // Producer: Add new tasks to the queue
        public async Task QueueTaskAsync(Func<Task> taskFunc)
        {
            await _taskChannel.Writer.WriteAsync(taskFunc);
        }

        // Complete the channel once no more tasks will be added
        public void Complete()
        {
            _taskChannel.Writer.Complete();
        }

        // Consumer: Process tasks from the queue
        private async Task ProcessTasksAsync()
        {
            await foreach (var taskFunc in _taskChannel.Reader.ReadAllAsync())
            {
                await _semaphore.WaitAsync(); // Control concurrency

                try
                {
                    await taskFunc(); // Run the actual I/O-bound task (like file writes)
                }
                finally
                {
                    _semaphore.Release();
                }
            }

            // Once all tasks have been processed, mark the TaskCompletionSource as completed
            _completionSource.TrySetResult();
        }

        // Wait for all tasks to complete
        public Task WaitForAllTasksAsync()
        {
            return _completionSource.Task;
        }
    }
}
