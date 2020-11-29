using Microsoft.EntityFrameworkCore;
using sma_services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sma_services.Services
{
    public class TranSactionService
    {

        TransactionContext db = new TransactionContext();

        public List<Transaction> GetListTranSaction()
        
        {
            return db.Transactions.ToList(); 
        }
    }
}
