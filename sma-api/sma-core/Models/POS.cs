using System;
using System.Collections.Generic;
using System.Text;

namespace sma_core.Models
{
    public class POS
    {
        public MarketPosition mp { get; set; }

        // 0 or 1 
        public int hqty { get; set; }

        public double hprice { get; set; }

    }

    public enum MarketPosition
    {
        Long,
        Short,
        None
    }
}
