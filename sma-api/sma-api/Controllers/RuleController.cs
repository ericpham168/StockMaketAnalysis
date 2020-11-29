using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using sma_core;
using sma_services.Models;

namespace sma_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RuleController : ControllerBase
    {

        private readonly TransactionContext _context;

        public RuleController(TransactionContext context)
        {
            _context = context;
        }


        // POST api
        // [Authorize]
        [Route("{minProfit}/{maxRisk}/{minWinRate}")]
        [HttpGet]
        public void Post(double minProfit, double maxRisk, double minWinRate)
        {
            SMACore smaCore = new SMACore(minProfit, maxRisk, minWinRate);
            smaCore.GenBP(null, 0, 0);

        }
    }
}
