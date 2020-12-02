using Microsoft.AspNetCore.Mvc;
using sma_core;
using sma_services.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace sma_api.Controllers
{
    [ApiController]
    public class RuleController : ControllerBase
    {

        private readonly TransactionContext _context = new TransactionContext();

        public RuleController(TransactionContext context)
        {
            _context = context;
        }

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
        public void GetRule(double minProfit, double maxRisk, double minWinRate)
        {
            
            
            SMACore smaCore = new SMACore(minProfit, maxRisk, minWinRate);
            smaCore.GenBP(null, 0, 0);
        }

        /// post transaction list api
        [Route("api/rule")]
        [HttpPost]
        public IActionResult PostTransactions([FromBody] List<Transaction> transactions)
        {
            try
            {
                _context.AddRange(transactions);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
