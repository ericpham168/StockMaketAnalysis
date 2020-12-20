using sma_services.Models;
using System.Collections.Generic;
using System.Linq;

namespace sma_services.Services
{
    public class TranSactionService
    {

        TransactionContext db = new TransactionContext();

        public List<Transaction> GetListTranSaction()
        
        {
            return db.Transactions.ToList(); 
        }

        public List<Transaction> GetListTranSaction(int ticker)

        {
            return db.Transactions.Where(o => o.TickerID == ticker).ToList();
        }

        public void AddTransactions(List<Transaction> transactions)
        {
            db.AddRange(transactions);
            db.SaveChanges(true);
        }

        public void RemoveAllTicker(int tickerID)
        {
            db.Transactions.RemoveRange(db.Transactions.Where(o => o.TickerID == tickerID));
            db.SaveChanges(true);
        }
    }
}
