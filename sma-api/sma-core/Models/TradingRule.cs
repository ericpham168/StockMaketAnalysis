using System;
using System.Collections.Generic;
using System.Text;

namespace sma_core
{
    public class TradingRule
    {
        public Pattern BP { get; set; }

        public Pattern SP { get; set; }

        public TP topPriority { get; set; }
        
        public TradingResult tradingResult { get; set; }
    }

    //// Top priority
    public enum TP
    {
        Both,
        SF,
        BF
    }


}
