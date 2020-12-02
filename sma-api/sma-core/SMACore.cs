using sma_core.Models;
using sma_services.Models;
using sma_services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace sma_core
{
    public class SMACore
    {
        #region property
        private const int MAX_SPAN = 2;
        private const double FEE = 0;

        private List<Pattern> oneItemList = new List<Pattern>();
        private int minSup = 0;
        private List<TradingRule> lstTradingRule = new List<TradingRule>();
        private List<Transaction> transactions = new List<Transaction>();
        TranSactionService service = new TranSactionService();
        public double MinProfit = 0;
        public double MaxRisk = 0;
        public double MinWinRate = 0;
        #endregion

        #region contrucstor
        public SMACore(double minProfit, double maxRisk, double minWinRate)
        {
            transactions = service.GetListTranSaction();
            MinProfit = minProfit;
            MaxRisk = maxRisk;
            MinWinRate = minWinRate;

            List<string> keys = new List<string>();
            transactions.Select(trans => trans.Data).ToList().ForEach( data => {
                keys = keys.Concat(data).ToList();
            });

            keys.Distinct().ToList().ForEach(key =>
            {
                Pattern pattern = new Pattern();
                pattern.name = key;
                List<int> TIDSetItem = transactions.Where(trans => Array.Exists(trans.Data, i => i == key)).Select(trans => trans.TID).ToList();
                pattern.TIDSet = TIDSetItem;
                oneItemList.Add(pattern);
            });
        }
        #endregion


        #region public method
        public void GenBP(Pattern prefix, int index, int interval)
        {
            if (interval > MAX_SPAN)
            {
                return;
            }

            for (int i = index; i < oneItemList.Count; i++)
            {
                Pattern BP = oneItemList[i];
                BP = Shift(BP, interval);
                BP = Join(prefix, BP);

                if (ComputeSup(BP) >= minSup)
                {
                    GenSP(BP, null, 0, 0);
                    GenBP(BP, i + 1, interval);
                }
            }


            if (prefix != null)
            {
                GenBP(prefix, 0, interval + 1);
            }


        }

        public void GenSP(Pattern BP, Pattern prefix, int index, int interval)
        {
            if (interval > MAX_SPAN)
            {
                return;
            }
            for (int i = index; i < oneItemList.Count; i++)
            {
                Pattern SP = new Pattern();
                SP = oneItemList[i];
                SP = Shift(SP, interval);
                SP = Join(prefix, SP);

                if (ComputeSup(SP) >= minSup)
                {
                    if (ComparePattern(BP, SP) != 1)
                    {
                        lstTradingRule = RuleGenerator(BP, SP);
                        foreach (var trRule in lstTradingRule)
                        {
                            TradingResult result = Simulate(trRule);
                            if (isResultsatisfy(result))
                            {
                                trRule.tradingResult = result;
                            }
                        }
                        GenSP(BP, SP, i + 1, interval);
                    }
                }
            }

            if (prefix != null)
            {
                GenSP(BP, prefix, 0, interval + 1);
            }
        }

        #endregion

        #region private method
        private bool isResultsatisfy(TradingResult fResult)
        {
            if (fResult.Profit < MinProfit || fResult.Risk > MaxRisk || fResult.WinRate < MinWinRate)
            {
                return false;
            }
            return true;
        }

        private TradingResult Simulate(TradingRule tradingRule)
        {
            TradingResult tradingResult = new TradingResult();
            List<SimulatitonPartern> simulateds = new List<SimulatitonPartern>();
            POS pOS = new POS();
            int index = 1;
            bool isBuyTradingCommand = false;
            List<int> tidBP = tradingRule.BP.TIDSet;
            List<int> tidSP = tradingRule.SP.TIDSet;
            //

            while (tidBP.Count > 0 && tidSP.Count > 0)
            {
                SimulatitonPartern sm = new SimulatitonPartern();
                if (index == 1)
                {
                    sm.No = index;
                    sm.TradingOrder.tc = tradingRule.topPriority == TP.SF ? TradingCommands.Sell : TradingCommands.Buy;
                    sm.TradingOrder.qty = 1;
                    sm.TradingOrder.price = sm.TradingOrder.tc == TradingCommands.Buy ? transactions[tidBP[0]].Price : transactions[tidSP[0]].Price;
                    sm.TID = sm.TradingOrder.tc == TradingCommands.Buy ? tidBP[0] : tidSP[0];
                    sm.HPOS.mp = sm.TradingOrder.tc == TradingCommands.Buy ? MarketPosition.Long : MarketPosition.Short;
                    sm.HPOS.hqty = 1;
                    sm.HPOS.hprice = sm.TradingOrder.price;
                    //
                    simulateds.Add(sm);
                    index++;
                    isBuyTradingCommand = sm.TradingOrder.tc == TradingCommands.Buy ? true : false;
                    if (tradingRule.topPriority == TP.BF)
                    {
                        tidBP.RemoveAt(0);
                    }
                    else
                    {
                        tidSP.RemoveAt(0);
                    }
                    continue;
                }

                if (isBuyTradingCommand)
                {
                    //simutaled trading is sell
                    sm.No = index;
                    sm.TradingOrder.tc = TradingCommands.Sell;
                    sm.TradingOrder.qty = 1;
                    sm.TradingOrder.price = transactions[tidSP[0]].Price;
                    sm.TID = tidSP[0];
                    sm.HPOS.mp = MarketPosition.Short;
                    sm.HPOS.hqty = 1;
                    sm.HPOS.hprice = sm.TradingOrder.price;
                    tidSP.RemoveAt(0);
                }
                else
                {
                    //simutaled trading is buy
                    sm.No = index;
                    sm.TradingOrder.tc = TradingCommands.Buy;
                    sm.TradingOrder.qty = 1;
                    sm.TradingOrder.price = transactions[tidBP[0]].Price;
                    sm.TID = tidBP[0];
                    sm.HPOS.mp = MarketPosition.Long;
                    sm.HPOS.hqty = 1;
                    sm.HPOS.hprice = sm.TradingOrder.price;
                    tidSP.RemoveAt(0);
                }
                index++;
                simulateds.Add(sm);

            }

            for (int i = 0; i < simulateds.Count - 1; i++)
            {
                // set Netprofit
                simulateds[i].NP = simulateds[i].HPOS.mp == MarketPosition.Long ?
                                    simulateds[i + 1].HPOS.hprice - simulateds[i].HPOS.hprice - MAX_SPAN * FEE :
                                    simulateds[i].HPOS.hprice - simulateds[i + 1].HPOS.hprice - MAX_SPAN * FEE;

                // set Consecutive loss
                simulateds[i].CLoss = i == 0 ? simulateds[i].NP : simulateds[i].NP + simulateds[i - 1].CLoss;

                // set Draw Down 
                simulateds[i].DD = simulateds[i].HPOS.mp == MarketPosition.Short ? 0 :
                                        i == 0 ? 0 + minP(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) - simulateds[i].HPOS.hprice :
                                        simulateds[i - 1].CLoss + minP(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) - simulateds[i].HPOS.hprice;
                // Set Run UP
                simulateds[i].RU = simulateds[i].HPOS.mp == MarketPosition.Long ? 0 :
                                        i == 0 ? 0 - maxP(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) + simulateds[i].HPOS.hprice :
                                        simulateds[i - 1].CLoss - maxP(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) + simulateds[i].HPOS.hprice;
            }

            Console.WriteLine(simulateds);
            return tradingResult;
        }

        private double maxP(double a, double b)
        {
            return a > b ? a : b;
        }

        private double minP(double a, double b)
        {
            return a < b ? a : b;
        }

        private Pattern Shift(Pattern pattern, int interval)
        {
            Pattern newParten = new Pattern();

            newParten.name = NewName(pattern.name, interval);

            for (int i = 0; i < pattern.TIDSet.Count; i++)
            {
                newParten.TIDSet[i] = pattern.TIDSet[i] - interval;
            }
            return newParten;
        }

        private Pattern Join(Pattern Prefix, Pattern BP)
        {
            Pattern newPattern = new Pattern();

            if (Prefix != null)
            {
                newPattern.name = Prefix.name + BP.name;

                Func<int, bool> func = (tid) =>
                {
                    return Array.Exists(BP.TIDSet.ToArray(), prefix => prefix == tid);
                };

                newPattern.TIDSet = Prefix.TIDSet.Where(func).ToList();
            }

            return BP;
        }

        private List<TradingRule> RuleGenerator(Pattern x, Pattern y)
        {
            List<TradingRule> lstTradingRule = new List<TradingRule>();

            if (x.TIDSet[0] != y.TIDSet[0])
            {
                TradingRule trRule1 = new TradingRule();
                TradingRule trRule2 = new TradingRule();
                trRule1.BP = x;
                trRule1.SP = y;
                trRule1.topPriority = TP.Both;
                trRule2.BP = y;
                trRule2.BP = x;
                trRule2.topPriority = TP.Both;
                lstTradingRule.Add(trRule1);
                lstTradingRule.Add(trRule2);
            }
            else if (ComparePattern(x, y) == 0)
            {
                TradingRule trRule1 = new TradingRule();
                TradingRule trRule2 = new TradingRule();
                trRule1.BP = x;
                trRule1.SP = y;
                trRule1.topPriority = TP.BF;
                trRule2.BP = x;
                trRule2.BP = y;
                trRule2.topPriority = TP.SF;
                lstTradingRule.Add(trRule1);
                lstTradingRule.Add(trRule2);

            }
            else if (ComparePattern(x, y) == -1)
            {
                TradingRule trRule1 = new TradingRule();
                TradingRule trRule2 = new TradingRule();
                trRule1.BP = x;
                trRule1.SP = y;
                trRule1.topPriority = TP.BF;
                trRule2.BP = x;
                trRule2.BP = y;
                trRule2.topPriority = TP.SF;
                lstTradingRule.Add(trRule1);
                lstTradingRule.Add(trRule2);

            }

            return lstTradingRule;


        }

        /// <summary>
        /// count appear amount pattern on MegaTransaction
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private int ComputeSup(Pattern pattern)
        {
            int megaTransactionCount = 0;

            for (int i = 0; i < pattern.TIDSet.Count - 1; i++)
            {
                if (pattern.TIDSet[i + 1] - pattern.TIDSet[i] >= MAX_SPAN)
                {
                    megaTransactionCount += 1;
                    break;
                }
            }
            return megaTransactionCount;
        }

        private int ComparePattern(Pattern BP, Pattern SP)
        {
            string[] subNameBPs = Regex.Matches(BP.name, @"[a-z]\(.*?\)").Cast<Match>().Select(m => m.Value).ToArray();
            string[] subNameSPs = Regex.Matches(SP.name, @"[a-z]\(.*?\)").Cast<Match>().Select(m => m.Value).ToArray();
            int lengthMin = subNameBPs.Length > subNameSPs.Length ? subNameSPs.Length : subNameBPs.Length;
            if (BP.name == SP.name)
            {
                return 0;
            }
            for (int i = 0; i < lengthMin; i++)
            {
                if (subNameBPs[i] != subNameSPs[i])
                {
                    int numBP = Int32.Parse(Regex.Matches(SP.name, @"\(.*?\)")
                                    .Cast<Match>().Select(m => m.Value).ToString()
                                    .Replace("(", "").Replace(")", "").ToString());
                    int numSP = Int32.Parse(Regex.Matches(SP.name, @"\(.*?\)")
                                    .Cast<Match>().Select(m => m.Value).ToString()
                                    .Replace("(", "").Replace(")", "").ToString());
                    if (numBP > numSP)
                    {
                        return -1;
                    }
                }
            }
            return 1;
        }

        /// <summary>
        /// generate new name when a pattern is shifted
        /// </summary>
        /// <param name="name"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        private string NewName(String name, int interval)
        {
            if (!name.Contains("("))
            {
                return $"{name}(0)";
            }
            else
            {
                int shiftIndex = int.Parse(name.Split('(')[2][0].ToString());
                return $"{name}({shiftIndex - interval})";
            }

        }

        #endregion
    }
}
