using Microsoft.AspNetCore.Mvc;
using sma_services.Models;
using sma_services.Services;
using System;
using System.Collections.Generic;

namespace sma_api.Controllers
{
    [ApiController]
    public class TranSactionController : ControllerBase
    {
        private readonly TranSactionService _context = new TranSactionService();

        /// post transaction list api
        [Route("api/transaction")]
        [HttpPost]
        public IActionResult PostTransactions([FromBody] List<Transaction> transactions)
        {
            try
            {
                _context.AddTransactions(transactions);
                return Ok();
            }
            catch(Exception ex)
            {
                ex.ToString();
                return StatusCode(404, "Error");
            }
        }

        [Route("api/transaction/{tikerID}")]
        [HttpDelete]
        public IActionResult DeleteTransaction(int tikerID)
        {
            try
            {
                _context.RemoveAllTicker(tikerID);
                return Ok();
            }
            catch
            {
                return StatusCode(404, "Error");
            }
        }

    }
}
