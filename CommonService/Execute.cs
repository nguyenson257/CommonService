using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonService
{
    public class Execute
    {
        public async static Task RunTaskQueue()
        {
            var service = new TaskQueueService(5); // Allow 5 concurrent tasks

            var httpClient = new HttpClient();
            List<string> urls = new List<string> // Example URLs
        {
            "https://example.com/segment1.ts",
            "https://example.com/segment2.ts",
            "https://example.com/segment3.ts"
        };

            // Dynamically queue tasks for downloading and writing files
            foreach (var url in urls)
            {
                var segmentPath = $"/path/to/save/{Guid.NewGuid()}.ts";

                await service.QueueTaskAsync(async () =>
                {
                    var segmentResponse = await httpClient.GetAsync(url);
                    var segmentBytes = await segmentResponse.Content.ReadAsByteArrayAsync();

                    // Write the segment to file asynchronously
                    await File.WriteAllBytesAsync(segmentPath, segmentBytes);
                    Console.WriteLine($"File written: {segmentPath}");
                });
            }

            // Mark no more tasks will be added
            service.Complete();

            // Wait for all tasks to finish
            await service.WaitForAllTasksAsync();

            Console.WriteLine("All tasks completed.");
        }
    }
}
