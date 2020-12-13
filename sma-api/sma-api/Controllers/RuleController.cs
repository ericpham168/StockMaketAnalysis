using Microsoft.AspNetCore.Mvc;
using sma_core;
using sma_services.Models;
using sma_services.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sma_api.Controllers
{
    [ApiController]
    public class RuleController : ControllerBase
    {

        private readonly TranSactionService _context = new TranSactionService();

        //
        [Route("api/rule")]
        [HttpGet]
        public IActionResult Get()
        {
            return StatusCode(403);
        }

        /// get rule api
        [Route("api/rule/{minProfit}/{maxRisk}/{minWinRate}")]
        [HttpGet]
        public IActionResult GetRule(double minProfit, double maxRisk, double minWinRate)
        {
            try
            {
                SMACore smaCore = new SMACore(minProfit, maxRisk, minWinRate);
                List<TradingRule> tradingRules = smaCore.GetRules();
                return Ok(tradingRules);
            }
            catch
            {
                return StatusCode(404, "Error");
            }
        }

        /// post transaction list api
        [Route("api/rule")]
        [HttpPost]
        public IActionResult PostTransactions([FromBody] List<Transaction> transactions)
        {
            try
            {
                _context.RemoveAll();
                _context.AddTransactions(transactions);
                return Ok();
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
