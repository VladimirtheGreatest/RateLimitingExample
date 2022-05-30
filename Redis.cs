using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;

namespace RateLimitingExample
{
    public class Redis : IRedis
    {
        private readonly IConfiguration _configuration;
        private readonly Lazy<ConnectionMultiplexer> _lazyConnection;

        public Redis(IConfiguration configuration)
        {
            _configuration = configuration;
            _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                string cacheConnection = _configuration["CacheConnection"];
                return ConnectionMultiplexer.Connect(cacheConnection);
            });
        }

        public ConnectionMultiplexer Connection
        {
            get
            { 
              return _lazyConnection.Value;
            }
        }
    }
}
