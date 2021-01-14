using Newtonsoft.Json;
using sma_core.Models;
using sma_services.Models;
using sma_services.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sma_core
{
    public class SMACore
    {
        #region property
        private const int MAX_SPAN = 2;
        private const double FEE = 1;

        private List<Pattern> oneItemList = new List<Pattern>();
        private int minSup = 0;
        List<TradingRule> tradingRules = new List<TradingRule>();
        private List<Transaction> transactions = new List<Transaction>();
        TranSactionService service = new TranSactionService();
        public double MinProfit = 0;
        public double MaxRisk = 0;
        public double MinWinRate = 0;

        //json list
        private List<string> BuyPatterns = new List<string>();
        private List<string> SellPatterns = new List<string>();
        private List<string> AllRule = new List<string>();
        #endregion

        #region contrucstor
        public SMACore(double minProfit, double maxRisk, double minWinRate, int tickerID)
        {
            transactions = service.GetListTranSaction(tickerID).OrderBy(trans => trans.TID).ToList();
            MinProfit = minProfit;
            MaxRisk = maxRisk;
            MinWinRate = minWinRate;
            minSup = minWinRate > 0 ? 1 : 0;
            List<string> keys = new List<string>();
            transactions.Select(trans => trans.Data).ToList().ForEach(data =>
            {
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

            oneItemList = oneItemList.OrderBy(p => p.name).ToList();
        }
        #endregion


        #region public method
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<TradingRule> GetRules()
        {
            tradingRules.Clear();
            GenBP(null, 0, 0);
            WriteZloc();
            return tradingRules;
        }

        #endregion

        #region private method
        private void GenBP(Pattern prefix, int index, int interval)
        {
            List<TradingRule> tradingRules = new List<TradingRule>();
            // clone prefix to avoid referencing
            Pattern pre = null;
            if (prefix != null)
            {
                pre = new Pattern();
                pre.name = prefix.name;
                pre.TIDSet = prefix.TIDSet.ToList();
            }


            // pattern X = 𝑥1(𝑖1) 𝑥2(𝑖2)⋅⋅⋅𝑥𝑗(𝑖𝑗), where j > 0 and 0 ≥ 𝑖𝑗 ≥ (1 − maxspan)
            if (interval >= MAX_SPAN)
            {
                return;
            }

            for (int i = index; i < oneItemList.Count; i++)
            {

                // clone BP to avoid referencing
                Pattern BP = new Pattern();
                BP.name = oneItemList[i].name;
                BP.TIDSet = oneItemList[i].TIDSet.ToList();

                BP = Shift(BP, interval);
                BP = Join(pre, BP);
                // json 
                BuyPatterns.Add(BP.name);

                // check if exist at least one transaction
                if (BP.TIDSet.Count() >= minSup)
                {

                    GenSP(BP, null, 0, 0);
                    GenBP(BP, i + 1, interval);
                }
            }


            if (pre != null)
            {
                GenBP(pre, 0, interval + 1);
            }
        }

        private void GenSP(Pattern BP, Pattern prefix, int index, int interval)
        {
            Pattern pre = null;
            if (prefix != null)
            {
                pre = new Pattern();
                pre.name = prefix.name;
                pre.TIDSet = prefix.TIDSet.ToList();
            }

            // pattern X = 𝑥1(𝑖1) 𝑥2(𝑖2)⋅⋅⋅𝑥𝑗(𝑖𝑗), where j > 0 and 0 ≥ 𝑖𝑗 ≥ (1 − maxspan)
            if (interval >= MAX_SPAN)
            {
                return;
            }
            for (int i = index; i < oneItemList.Count; i++)
            {
                Pattern SP = new Pattern();
                SP.name = oneItemList[i].name;
                SP.TIDSet = oneItemList[i].TIDSet.ToList();

                SP = Shift(SP, interval);
                SP = Join(pre, SP);

                if(!SellPatterns.Exists(sp => sp == SP.name))
                { 
                    SellPatterns.Add(SP.name);

                    // check if exist at least one transaction
                    if (SP.TIDSet.Count() >= minSup)
                    {

                        if (ComparePattern(BP, SP) != 1 && BP.TIDSet.Count() > 0 && SP.TIDSet.Count > 0)
                        {
                            List<TradingRule> lstTradingRule = new List<TradingRule>();
                            lstTradingRule = RuleGenerator(BP, SP);

                            foreach (var trRule in lstTradingRule)
                            {
                                TradingResult tradingResult = Simulate(trRule);
                                AllRule.Add(String.Format("[{0},{1},{2}] [{3},{4},{5}]",
                                    trRule.topPriority, trRule.BP.name, trRule.SP.name, tradingResult.Profit, tradingResult.Risk, tradingResult.WinRate));

                                if (isResultsatisfy(tradingResult))
                                {
                                    trRule.tradingResult = tradingResult;
                                    if (tradingRules.Count > 1)
                                    {
                                        var x = 0;
                                    }
                                    if (tradingRules.Count > 0)
                                    {
                                        bool isPatternSame = false;
                                        foreach (var o in tradingRules)
                                        {
                                            if (trRule.BP.name == o.BP.name && trRule.SP.name == o.SP.name && trRule.topPriority == o.topPriority)
                                            {
                                                isPatternSame = true;
                                                break;
                                            }
                                        }
                                        //Parallel.ForEach(tradingRules, o =>
                                        //{
                                        //    if (trRule.BP.name == o.BP.name && trRule.SP.name == o.SP.name && trRule.topPriority == o.topPriority)
                                        //    {
                                        //        isPatternSame = true;
                                        //    }
                                        //});
                                        if (!isPatternSame)
                                            tradingRules.Add(trRule);

                                    }
                                    else
                                    {
                                        tradingRules.Add(trRule);
                                    }
                                }
                            }
                            GenSP(BP, SP, i + 1, interval);
                        }
                    }
                }
            }

            if (prefix != null)
            {
                GenSP(BP, pre, 0, interval + 1);
            }
        }

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
            List<int> tidBP = new List<int>();
            if (tradingRule.BP?.TIDSet.Count() > 0)
            {
                tidBP.AddRange(tradingRule.BP?.TIDSet);
            }
            List<int> tidSP = new List<int>();
            if (tradingRule.SP?.TIDSet.Count() > 0)
            {
                tidSP.AddRange(tradingRule.SP?.TIDSet);
            }
            //

            // check if exist at least one complete trading order || Sell trading Command and existing sell transactions when buy transaction end. 
            while ((tidBP?.Count > 0 && tidSP?.Count > 0) || (isBuyTradingCommand && tidSP?.Count > 0))
            {

                // generate all Trading order and Hold position
                SimulatitonPartern sm = new SimulatitonPartern();
                sm.TradingOrder = new TradingOrder();
                sm.TradingResult = new TradingResult();
                sm.HPOS = new POS();
                if (index == 1)
                {
                    sm.No = index;
                    sm.TradingOrder.tc = tradingRule.topPriority == TP.SF ? TradingCommands.Sell : TradingCommands.Buy;

                    sm.TradingOrder.qty = 1;
                    //transaction start at 0-1-2, tid start at 1-2-3
                    sm.TradingOrder.price = sm.TradingOrder.tc == TradingCommands.Buy ? transactions[tidBP[0] - 1].Price : transactions[tidSP[0] - 1].Price;
                    sm.TID = sm.TradingOrder.tc == TradingCommands.Buy ? tidBP[0] : tidSP[0];
                    sm.HPOS.mp = sm.TradingOrder.tc == TradingCommands.Buy ? MarketPosition.Long : MarketPosition.Short;
                    sm.HPOS.hqty = 1;
                    sm.HPOS.hprice = sm.TradingOrder.price;
                    //
                    isBuyTradingCommand = sm.TradingOrder.tc == TradingCommands.Buy ? true : false;

                }
                else
                {
                    if (isBuyTradingCommand)
                    {
                        //simutaled trading is sell
                        sm.No = index;
                        sm.TradingOrder.tc = TradingCommands.Sell;
                        sm.TradingOrder.qty = 1;
                        sm.TradingOrder.price = transactions[tidSP[0] - 1].Price;
                        sm.TID = tidSP[0];
                        sm.HPOS.mp = MarketPosition.Short;
                        sm.HPOS.hqty = 1;
                        sm.HPOS.hprice = sm.TradingOrder.price;
                        isBuyTradingCommand = false;
                    }
                    else
                    {
                        //simutaled trading is buy
                        sm.No = index;
                        sm.TradingOrder.tc = TradingCommands.Buy;
                        sm.TradingOrder.qty = 1;
                        sm.TradingOrder.price = transactions[tidBP[0] - 1].Price;
                        sm.TID = tidBP[0];
                        sm.HPOS.mp = MarketPosition.Long;
                        sm.HPOS.hqty = 1;
                        sm.HPOS.hprice = sm.TradingOrder.price;
                        isBuyTradingCommand = true;
                    }
                }

                if (sm.TradingOrder.tc == TradingCommands.Buy)
                {
                    if (tidSP[0] == tidBP[0])
                    {
                        tidSP.RemoveAt(0);
                    }
                    tidBP.RemoveAt(0);
                }
                else
                {
                    // avoid empty buy pattern 
                    if (tidBP.Count() > 0 && tidBP[0] == tidSP[0])
                    {
                        tidBP.RemoveAt(0);
                    }
                    tidSP.RemoveAt(0);
                }

                index++;
                simulateds.Add(sm);

            }

            for (int i = 0; i < simulateds.Count; i++)
            {
                // set Netprofit
                // Last record don't have Netprofit
                simulateds[i].NP = i == (simulateds.Count - 1) ? 0 : simulateds[i].HPOS.mp == MarketPosition.Long ?
                                    simulateds[i + 1].HPOS.hprice - simulateds[i].HPOS.hprice - MAX_SPAN * FEE :
                                    simulateds[i].HPOS.hprice - simulateds[i + 1].HPOS.hprice - MAX_SPAN * FEE;

                // set Consecutive loss
                //Last record don't have Netprofit
                simulateds[i].CLoss = i == 0 ? simulateds[i].NP : simulateds[i].NP + simulateds[i - 1].CLoss;

                // Closs could not be positive && Last record don't have Netprofit
                simulateds[i].CLoss = (simulateds[i].CLoss > 0) || i == (simulateds.Count - 1) ? 0 : simulateds[i].CLoss;


                // Set Run UP && Draw Down 
                if (i == simulateds.Count - 1)
                {
                    if (isExistsTID(simulateds[i].TID))
                    {
                        simulateds[i].DD = simulateds[i].HPOS.mp == MarketPosition.Short ? 0 :
                                                i == 0 ? 0 + Math.Min(simulateds[i].HPOS.hprice, GetTransactionPrice(simulateds[i].TID + 1)) - simulateds[i].HPOS.hprice :
                                                simulateds[i - 1].CLoss + Math.Min(simulateds[i].HPOS.hprice, GetTransactionPrice(simulateds[i].TID + 1)) - simulateds[i].HPOS.hprice;

                        simulateds[i].RU = simulateds[i].HPOS.mp == MarketPosition.Long ? 0 :
                                                i == 0 ? 0 - Math.Max(simulateds[i].HPOS.hprice, GetTransactionPrice(simulateds[i].TID + 1)) + simulateds[i].HPOS.hprice :
                                                simulateds[i - 1].CLoss - Math.Max(simulateds[i].HPOS.hprice, GetTransactionPrice(simulateds[i].TID + 1)) + simulateds[i].HPOS.hprice;
                    }
                }
                else
                {
                    simulateds[i].DD = simulateds[i].HPOS.mp == MarketPosition.Short ? 0 :
                                            i == 0 ? 0 + Math.Min(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) - simulateds[i].HPOS.hprice :
                                            simulateds[i - 1].CLoss + Math.Min(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) - simulateds[i].HPOS.hprice;
                    simulateds[i].RU = simulateds[i].HPOS.mp == MarketPosition.Long ? 0 :
                                            i == 0 ? 0 - Math.Max(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) + simulateds[i].HPOS.hprice :
                                            simulateds[i - 1].CLoss - Math.Max(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) + simulateds[i].HPOS.hprice;
                }

                // Set Profit
                simulateds[i].TradingResult.Profit = i == (simulateds.Count - 1) ? 0 : simulateds.Sum(o => o.NP);

                //Set Risk
                simulateds[i].TradingResult.Risk = i == 0 ? new List<double>() { Absolute(simulateds[i].CLoss), Absolute(simulateds[i].DD), Absolute(simulateds[i].RU) }.Max() :
                    new List<double>() { Absolute(simulateds[i].CLoss), Absolute(simulateds[i].DD), Absolute(simulateds[i].RU), Absolute(simulateds[i - 1].TradingResult.Risk) }.Max();

                //Set WinRate
                simulateds[i].TradingResult.WinRate = i == (simulateds.Count - 1) ? 0 : Math.Round(simulateds.Where(o => o.NP > 0).Count() * 1.0 / simulateds[i].No * 100, 0);
            }

            // to do
            tradingResult.Profit = simulateds.Count >= 2 ? simulateds[simulateds.Count - 2].TradingResult.Profit : simulateds[simulateds.Count - 1].TradingResult.Profit;
            tradingResult.Risk = isExistsTID(simulateds[simulateds.Count - 1].TID) ? simulateds[simulateds.Count - 1].TradingResult.Risk : simulateds[simulateds.Count - 2].TradingResult.Risk;
            tradingResult.WinRate = simulateds.Count >= 2 ? simulateds[simulateds.Count - 2].TradingResult.WinRate : simulateds[simulateds.Count - 1].TradingResult.WinRate;

            return tradingResult;
        }

        private double Absolute(double value)
        {
            return Math.Abs(value);
        }

        private Pattern Shift(Pattern pattern, int interval)
        {
            //if(interval == MAX_SPAN)
            Pattern newParten = new Pattern();
            newParten = pattern;

            newParten.name = NewName(newParten.name, interval);
            for (int i = 0; i < newParten.TIDSet.Count; i++)
            {
                newParten.TIDSet[i] += interval;
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
                return newPattern;
            }

            return BP;
        }

        private List<TradingRule> RuleGenerator(Pattern x, Pattern y)
        {
            List<TradingRule> lstTradingRule = new List<TradingRule>();

            if (x.TIDSet[0] != y.TIDSet[0] || ComparePattern(x, y) == -1)
            {
                TradingRule trRule1 = new TradingRule();
                TradingRule trRule2 = new TradingRule();
                TradingRule trRule3 = new TradingRule();
                TradingRule trRule4 = new TradingRule();
                //
                trRule1.BP = x;
                trRule1.SP = y;
                trRule1.topPriority = TP.BF;
                trRule2.BP = x;
                trRule2.SP = y;
                trRule2.topPriority = TP.SF;
                trRule3.BP = y;
                trRule3.SP = x;
                trRule3.topPriority = TP.BF; 
                trRule4.BP = y;
                trRule4.SP = x;
                trRule4.topPriority = TP.SF;
                //
                lstTradingRule.Add(trRule1);
                lstTradingRule.Add(trRule2);
                lstTradingRule.Add(trRule3);
                lstTradingRule.Add(trRule4);
            }
            else if (ComparePattern(x, y) == 0)
            {
                TradingRule trRule1 = new TradingRule();
                TradingRule trRule2 = new TradingRule();
                trRule1.BP = x;
                trRule1.SP = y;
                trRule1.topPriority = TP.BF;
                trRule2.BP = x;
                trRule2.SP = y;
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

        private bool isExistsTID(int tid)
        {
            try
            {
                var trans = transactions[tid];
                return true;
            }
            catch
            {
                return false;
            }
        }

        private double GetTransactionPrice(int tid)
        {
            return transactions.FirstOrDefault(trans => trans.TID == tid).Price;
        }

        private int ComparePattern(Pattern BP, Pattern SP)
        {
            string[] subNameBPs = Regex.Matches(BP.name, @"[a-zA-Z]\(.*?\)").Cast<Match>().Select(m => m.Value).ToArray();
            string[] subNameSPs = Regex.Matches(SP.name, @"[a-zA-Z]\(.*?\)").Cast<Match>().Select(m => m.Value).ToArray();
            int lengthMin = subNameBPs.Length > subNameSPs.Length ? subNameSPs.Length : subNameBPs.Length;
            if (BP.name == SP.name)
            {
                return 0;
            }
            // compare based on price if two pattern are same   
            else if (subNameBPs.Length == subNameSPs.Length && subNameBPs.Length != 0)
            {
                Func<Transaction, bool> funcBP = (tid) =>
                {
                    return Array.Exists(BP.TIDSet.ToArray(), tidBP => tidBP == tid.TID);
                };

                Func<Transaction, bool> funcSP = (tid) =>
                {
                    return Array.Exists(SP.TIDSet.ToArray(), tidSP => tidSP == tid.TID);
                };

                double avgBP = transactions.Where(funcBP).Average(o => o.Price);
                double avgSP = transactions.Where(funcSP).Average(o => o.Price);

                if (avgBP < avgSP) return -1;
            }
            // when BP length < SP length. Remove all same 1-pattern. the pattern, null at first, is > the rest. 
            else if (subNameBPs.Length < subNameSPs.Length && subNameBPs.Length != 0)
            {
                Func<string, bool> func = (name) =>
                {
                    return Array.Exists(subNameSPs, nameSP => nameSP == name);
                };

                if (subNameBPs.Except(subNameSPs.Where(func)).Count() > 0)
                {
                    return -1;
                }
            }
            //
            else if (subNameSPs.Length != 0)
            {
                Func<string, bool> func = (name) =>
                {
                    return Array.Exists(subNameBPs, nameBP => nameBP == name);
                };

                if (subNameSPs.Except(subNameBPs.Where(func)).Count() > 0)
                {
                    return -1;
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
            string newName = name;
            if (!newName.Contains("("))
            {
                newName = $"{name}(0)";
            }

            if (interval > 0)
            {
                //int shiftIndex = int.Parse(name.Split('(')[2][0].ToString());
                List<String> shifts = Regex.Matches(newName, @"[0-9]").Cast<Match>().Select(m => m.Value).ToList();
                List<String> letter = Regex.Matches(newName, @"[a-fA-F]").Cast<Match>().Select(m => m.Value).ToList();
                int shift = int.Parse(shifts[0]);
                newName = $"{letter[0]}({shift - interval})";
            }
            return newName;
        }

        private void WriteZloc()
        {
            try
            {
                string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "JsonData");
                // If directory does not exist, create it. 
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }
                File.WriteAllText(Path.Combine(FolderPath, "BP"), JsonConvert.SerializeObject(BuyPatterns.ToList()));
                File.WriteAllText(Path.Combine(FolderPath, "SP"), JsonConvert.SerializeObject(SellPatterns.ToList()));
                File.WriteAllText(Path.Combine(FolderPath, "Rule"), JsonConvert.SerializeObject(AllRule.ToList()));
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        #endregion
    }
}
