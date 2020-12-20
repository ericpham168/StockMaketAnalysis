using sma_services.Models;
using System.Collections.Generic;
using System.Linq;

namespace sma_services.Services
{
    public class TickerTransactionService
    {
        TransactionContext db = new TransactionContext();

        public List<TickerTranSaction> GetAllTicker()
        {
            return db.TickerTranSactions.ToList();
        }

        public TickerTranSaction GetTicker(string ticker)
        {
            return db.TickerTranSactions.FirstOrDefault(o => o.Ticker == ticker);
        }

        public void AddTicker(TickerTranSaction ticker)
        {
            db.TickerTranSactions.Add(ticker);
            db.SaveChanges();
        }

    }
}
