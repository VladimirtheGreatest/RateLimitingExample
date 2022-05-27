using System;

namespace RateLimitingExample
{
    [AttributeUsage(AttributeTargets.Method)]
    public class LimitRequests : Attribute
    {
        public int TimeWindow { get; set; } // seconds
        public int MaxRequests { get; set; }  // requests
    }
}
