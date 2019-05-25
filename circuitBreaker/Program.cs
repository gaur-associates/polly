using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;


namespace myApp
{
    class Program
    {
        static void Main(string[] args)
        {
            setup();
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Thread.Sleep(3000);
                    fetch().GetAwaiter().GetResult();

                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.ResetColor();

                    requestEndpoint = "https://jsonplaceholder.typicode.com/posts/1";

                }
            }

        }
        static Task<HttpResponseMessage> makeHttpCall()
        {
            Console.WriteLine($"made http get call {requestEndpoint} - braker state{breakerPolicy.CircuitState}");
            return httpClient.GetAsync(requestEndpoint);
        }

        static async Task fetch()
        {

            HttpResponseMessage response = await breakerPolicy.ExecuteAsync(makeHttpCall);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var post = JsonConvert.DeserializeObject<Post>(json);

                Console.ForegroundColor = ConsoleColor.Green;

                Console.WriteLine(post.title);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"HTTP Status Code {response.StatusCode}");
            }
        }

        static string requestEndpoint = "https://jsonplaceholder.typicode.com/postsa/1";
        static HttpClient httpClient = new HttpClient();
        static CircuitBreakerPolicy<HttpResponseMessage> breakerPolicy;

        static void setup()
        {
            breakerPolicy = Policy
                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(5),
                    OnBreak, OnReset, OnHalfOpen);
        }
        static void OnHalfOpen()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Half open");
            Console.ResetColor();

            breakerPolicy.Reset();
        }

        static void OnReset(Context context)
        {
            Console.WriteLine("Reset");
        }

        static void OnBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, Context context)
        {
            Console.WriteLine($"Break - braker state{breakerPolicy.CircuitState}");
        }

    }
}
