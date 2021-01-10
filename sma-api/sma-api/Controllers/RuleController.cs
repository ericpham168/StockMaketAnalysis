using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using sma_core;
using System;
using System.Collections.Generic;

namespace sma_api.Controllers
{
    [ApiController]
    public class RuleController : ControllerBase
    {

        //
        [Route("api/rule")]
        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(403);
        }

        /// get rule api
        [Route("api/rule/{minProfit}/{maxRisk}/{minWinRate}/{tickerID}")]
        [EnableCors("policy")]
        [HttpGet]
        public IActionResult GetRule(double minProfit, double maxRisk, double minWinRate,int tickerID)
        {
            try
            {
                SMACore smaCore = new SMACore(minProfit, maxRisk, minWinRate,tickerID);
                List<TradingRule> tradingRules = smaCore.GetRules();
                return Ok(tradingRules);
            }
            catch(Exception e)
            {
                e.Message.ToString();
                return StatusCode(404, "Error");
            }
        }
    }
}
