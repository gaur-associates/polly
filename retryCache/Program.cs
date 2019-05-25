using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;
using Polly.Caching;
using Polly.Caching.MemoryCache;
using Polly.Retry;

using Polly.Wrap;

namespace myApp
{
    class Program
    {
        static void Main(string[] args)
        {
            setup();
            fetch(1).GetAwaiter().GetResult();
            fetch(1).GetAwaiter().GetResult();
        }

        static async Task fetch(int id)
        {
            Context policyExecutionContext = new Context($"GetById-{id}");

            Console.WriteLine("making http get call");
            HttpResponseMessage response = await policyWrap.ExecuteAsync(
                           (ctx) => httpClient.GetAsync(requestEndpoint + "/" + id), policyExecutionContext);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var post = JsonConvert.DeserializeObject<Post>(json);
                Console.WriteLine(post.title);
            }
        }

        static string requestEndpoint = "https://jsonplaceholder.typicode.com/postsa";
        static HttpClient httpClient = new HttpClient();
        static IAsyncPolicy<HttpResponseMessage> retryPolicy;
        static PolicyWrap<HttpResponseMessage> policyWrap;
        static IAsyncPolicy<HttpResponseMessage> cachePolicy;
        static void setup()
        {
            retryPolicy = Policy
                           .Handle<Exception>()
                           .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                           .RetryAsync(2, (ex, retryCnt) =>
                           {
                               Console.WriteLine($"Retry count {retryCnt}");
                           });

            var myCache = new MemoryCache(new MemoryCacheOptions());
            MemoryCacheProvider memoryCacheProvider
               = new MemoryCacheProvider(myCache);

            cachePolicy =
                Policy.CacheAsync<HttpResponseMessage>(memoryCacheProvider, TimeSpan.FromMinutes(5));


            policyWrap = Policy.WrapAsync<HttpResponseMessage>(cachePolicy, retryPolicy);
        }
    }
}
