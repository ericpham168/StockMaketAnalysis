using System;
using System.Collections.Generic;
using System.Text;

namespace sma_core.Models
{
    public class SimulatitonPartern
    {
        public int No { get; set; }

        public TradingOrder TradingOrder { get; set; }

        public int TID { get; set; }

        public POS HPOS { get; set; }

        public double NP { get; set; }

        public double CLoss { get; set; }

        public double DD { get; set; }
        
        public double RU { get; set; }

        public TradingResult TradingResult { get; set; }
        
    }
}
