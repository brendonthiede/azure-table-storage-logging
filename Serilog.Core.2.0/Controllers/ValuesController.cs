using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Collections.Generic;

namespace LoggingResearch.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        ILogger<ValuesController> logger;

        public ValuesController(ILogger<ValuesController> logger)
        {
            this.logger = logger;
        }

        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            logger.LogInformation("/api/values was requested by {UserId} because {Reason}", new string[] { "me", "I wanted to" });
            logger.LogDebug("I only have hard coded values, and I'm very self conscious about it");
            logger.LogWarning("I'm feeling {feeling} and I want {audience} to know about it", new string[] { "freaked out", "you" });
            logger.LogWarning("My favorite food is {food}", new string[] { "tacos" });
            logger.LogWarning("My favorite drink is {drink}", new string[] { "ionized water" });
            logger.LogWarning("My favorite animal is {animal}", new string[] { "bear" });
            logger.LogError("Something here is {state}", new string[] { "wrong" });
            logger.LogError("Something here is {state}", new string[] { "odd" });
            logger.LogError("Something here is {state}", new string[] { "smelly" });
            logger.LogCritical("I'm feeling {feeling}", new string[] { "hot" });
            logger.LogCritical("I'm feeling {feeling}", new string[] { "sweaty" });
            logger.LogCritical("I'm feeling {feeling}", new string[] { "hungry" });
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            logger.LogInformation($"/api/values/{id} was requested");
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
