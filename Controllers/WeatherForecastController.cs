using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RateLimitingExample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly IRedis _redisCache;
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRedis redisCache)
        {
            _logger = logger;
            _redisCache = redisCache;
        }

        [HttpGet("")]
        [LimitRequests(MaxRequests = 5, TimeWindow = 60)] 
        public IEnumerable<WeatherForecast> Get()
        {
            var connection = _redisCache.Connection;

            IDatabase cache = connection.GetDatabase();


            ClientStatistics clientStatistic = new ClientStatistics
            {
                LastSuccessfulResponseTime = DateTime.Now,
                NumberOfRequestsCompletedSuccessfully = 1,

            };

            var messageSet = cache.StringSet("ServiceProviderKeyExample", JsonConvert.SerializeObject(clientStatistic)).ToString();


            ClientStatistics itemFromCache = JsonConvert.DeserializeObject<ClientStatistics>(cache.StringGet("ServiceProviderKeyExample"));



            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
