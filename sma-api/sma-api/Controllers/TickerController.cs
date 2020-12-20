using Microsoft.AspNetCore.Mvc;
using sma_services.Models;
using sma_services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace sma_api.Controllers
{
    [ApiController]
    public class TickerController : ControllerBase
    {
        private readonly TickerTransactionService _context = new TickerTransactionService();

        [Route("api/ticker")]
        [HttpGet]
        public IActionResult GetListTicker()
        {
            try
            {
                return Ok(_context.GetAllTicker());
            }
            catch
            {
                return StatusCode(404, "Error");
            }
        }

        /// <summary>
        ///  Get ticker by ticker name
        /// </summary>
        /// <param name="ticker"> ticker namen</param>
        /// <returns></returns>
        [Route("api/ticker/{ticker}")]
        [HttpGet]
        public IActionResult GetTransaction(string ticker)
        {
            try
            {
                TickerTranSaction _ticker = _context.GetTicker(ticker);
                return Ok(_ticker);
            }
            catch
            {
                return StatusCode(404, "Error");
            }
        }

        /// post transaction list api
        [Route("api/ticker")]
        [HttpPost]
        public IActionResult AddTicker([FromBody] TickerTranSaction ticker)
        {
            try
            {
                _context.AddTicker(ticker);
                return Ok(_context.GetTicker(ticker.Ticker));
            }
            catch
            {
                return StatusCode(404, "Error");
            }
        }
    }
}
