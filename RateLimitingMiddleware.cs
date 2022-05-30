using System;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using RateLimitingExample.Extensions;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace RateLimitingExample
{
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRedis _redisCache;

        public RateLimitingMiddleware(RequestDelegate next, IRedis redisCache)
        {
            _next = next;
            _redisCache = redisCache;
        }

        //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-6.0

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            var rateLimitingDecorator = endpoint?.Metadata.GetMetadata<LimitRequests>();

            if (rateLimitingDecorator is null)
            {
                await _next(context);
                return;
            }

            var connection = _redisCache?.Connection;

            if (connection is null)
            {
                await _next(context);
                return;
            }

            IDatabase cache = connection.GetDatabase();

            var key = GenerateClientKey(context);
            var clientStatistics = await GetClientStatisticsByKey(key, cache);

            if (clientStatistics != null && DateTime.UtcNow < clientStatistics.LastSuccessfulResponseTime.AddSeconds(rateLimitingDecorator.TimeWindow) && clientStatistics.NumberOfRequestsCompletedSuccessfully == rateLimitingDecorator.MaxRequests)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                return;
            }

            await UpdateClientStatisticsStorage(key, rateLimitingDecorator.MaxRequests, cache);
            await _next(context);
        }

        private static string GenerateClientKey(HttpContext context) => $"{context.Request.Path}_{context.Connection.RemoteIpAddress}"; //eventually replace this with the clients api key

        private async Task<ClientStatistics> GetClientStatisticsByKey(string key, IDatabase cache)
        {
            var clientStatistics = await cache.StringGetAsync(key);

            if (clientStatistics.IsNullOrEmpty)
            {
                var genesisClientStat = new ClientStatistics
                {
                    LastSuccessfulResponseTime = DateTime.UtcNow,
                    NumberOfRequestsCompletedSuccessfully = 1
                };

                await cache.StringSetAsync(key, JsonConvert.SerializeObject(genesisClientStat).ToString());
            }

            return JsonConvert.DeserializeObject<ClientStatistics>(await cache.StringGetAsync(key));
        } 

        private async Task UpdateClientStatisticsStorage(string key, int maxRequests, IDatabase cache)
        {
            ClientStatistics clientStat = JsonConvert.DeserializeObject<ClientStatistics>(await cache.StringGetAsync(key));

            if (clientStat != null)
            {
                clientStat.LastSuccessfulResponseTime = DateTime.UtcNow;

                if (clientStat.NumberOfRequestsCompletedSuccessfully == maxRequests)
                    clientStat.NumberOfRequestsCompletedSuccessfully = 1;

                else
                    clientStat.NumberOfRequestsCompletedSuccessfully++;

                await cache.StringSetAsync(key, JsonConvert.SerializeObject(clientStat).ToString());
            }
            else
            {
                var clientStatistics = new ClientStatistics
                {
                    LastSuccessfulResponseTime = DateTime.UtcNow,
                    NumberOfRequestsCompletedSuccessfully = 1
                };

                await cache.StringSetAsync(key, JsonConvert.SerializeObject(clientStatistics).ToString());
            }

        }
    }
}
public class ClientStatistics
{
    public DateTime LastSuccessfulResponseTime { get; set; }
    public int NumberOfRequestsCompletedSuccessfully { get; set; }
}


