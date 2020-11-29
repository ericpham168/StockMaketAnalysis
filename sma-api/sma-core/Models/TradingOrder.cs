using System;
using System.Collections.Generic;
using System.Text;

namespace sma_core.Models
{
    public class TradingOrder
    {
        public TradingCommands tc { get; set; } 

        public int qty { get; set; }

        public double price { get; set; }
        
    }

    public enum TradingCommands
    {
        Buy,
        Sell
    } 
}
