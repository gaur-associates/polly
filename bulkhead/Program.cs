using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Bulkhead;

namespace myApp
{
    class Program
    {
        static void Main(string[] args)
        {
            setup();

            List<Task> taskList = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                taskList.Add(Task.Run(() => fetch(i)));
            }
            Task.WaitAll(taskList.ToArray());

        }
        static void setup()
        {
            bulkheadIsolationPolicy = Policy
                          .BulkheadAsync<HttpResponseMessage>(2, 2, OnBulkheadRejectedAsync);
        }
        static async Task fetch(int id)
        {
            try
            {
                LogBulkheadInfo();
                HttpResponseMessage response = await bulkheadIsolationPolicy.ExecuteAsync(
                               () => httpClient.GetAsync(requestEndpoint + "/" + id));

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var post = JsonConvert.DeserializeObject<Post>(json);
                    Console.WriteLine(post.title);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");

            }
        }

        static string requestEndpoint = "https://jsonplaceholder.typicode.com/posts";
        static HttpClient httpClient = new HttpClient();
        static BulkheadPolicy<HttpResponseMessage> bulkheadIsolationPolicy;


        static Task OnBulkheadRejectedAsync(Context context)
        {
            Console.WriteLine($"PollyDemo OnBulkheadRejectedAsync Executed");
            return Task.CompletedTask;
        }
        static void LogBulkheadInfo()
        {

            Console.WriteLine($"BulkheadAvailableCount " +
                                               $"{bulkheadIsolationPolicy.BulkheadAvailableCount}");
            Console.WriteLine($"QueueAvailableCount " +
                                               $"{bulkheadIsolationPolicy.QueueAvailableCount}");
        }
    }
}
