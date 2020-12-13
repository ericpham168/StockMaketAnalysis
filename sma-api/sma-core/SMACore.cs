﻿using sma_core.Models;
using sma_services.Models;
using sma_services.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace sma_core
{
    public class SMACore
    {
        #region property
        private const int MAX_SPAN = 2;
        private const double FEE = 1;

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
            minSup = minWinRate > 0 ? 1 : 0;
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

            oneItemList = oneItemList.OrderBy(p => p.name).ToList();
        }
        #endregion


        #region public method
        public void GenBP(Pattern prefix, int index, int interval)
        {

            // clone prefix to avoid referencing
            Pattern pre = null;
            if(prefix != null)
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

                // check if exist at least one transaction
                if (BP.TIDSet.Count() >= minSup)
                {
                    foreach (var item in BP.TIDSet)
                    {
                        Debug.WriteLine(BP.name + " " + item);
                    }
                    Debug.WriteLine("");

                    GenSP(BP, null, 0, 0);
                    Debug.WriteLine("==========================================");
                    GenBP(BP, i + 1, interval);
                }
            }


            if (pre != null)
            {
                GenBP(pre, 0, interval + 1);
            }
        }

        public void GenSP(Pattern BP, Pattern prefix, int index, int interval)
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


                // check if exist at least one transaction
                if (SP.TIDSet.Count() >= minSup)
                {
                    //foreach (var item in SP.TIDSet)
                    //{
                    //    Debug.WriteLine(SP.name + " " + item);
                    //}
                    //Debug.WriteLine("------------------------");
                    if (ComparePattern(BP, SP) != 1 && BP.TIDSet.Count() > 0 && SP.TIDSet.Count > 0)
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
                GenSP(BP, pre, 0, interval + 1);
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
            List<int> tidBP = new List<int>();
            if(tradingRule.BP?.TIDSet.Count() > 0)
            {
                tidBP.AddRange(tradingRule.BP?.TIDSet);
            }
            List<int> tidSP = new List<int>();
            if(tradingRule.SP?.TIDSet.Count() > 0)
            {
                tidSP.AddRange(tradingRule.SP?.TIDSet);
            }
            //

            while (tidBP?.Count > 0 && tidSP?.Count > 0)
            {
                SimulatitonPartern sm = new SimulatitonPartern();
                sm.TradingOrder = new TradingOrder();
                sm.TradingResult = new TradingResult();
                sm.HPOS = new POS();
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
                                        i == 0 ? 0 + Math.Min(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) - simulateds[i].HPOS.hprice :
                                        simulateds[i - 1].CLoss + Math.Min(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) - simulateds[i].HPOS.hprice;
                // Set Run UP
                simulateds[i].RU = simulateds[i].HPOS.mp == MarketPosition.Long ? 0 :
                                        i == 0 ? 0 - Math.Max(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) + simulateds[i].HPOS.hprice :
                                        simulateds[i - 1].CLoss - Math.Max(simulateds[i].HPOS.hprice, simulateds[i + 1].HPOS.hprice) + simulateds[i].HPOS.hprice;
                
                // Set Profit
                simulateds[i].TradingResult.Profit = simulateds.Sum(o => o.NP);

                //Set Risk
                simulateds[i].TradingResult.Risk = i == 0 ? new List<double>() { Absolute(simulateds[i].CLoss), Absolute(simulateds[i].DD), Absolute(simulateds[i].RU) }.Max() :
                    new List<double>() { Absolute(simulateds[i].CLoss), Absolute(simulateds[i].DD), Absolute(simulateds[i].RU), Absolute(simulateds[i-1].TradingResult.Risk) }.Max() ;
                
                //Set WinRate
                simulateds[i].TradingResult.WinRate = simulateds.Where(o => o.NP > 0).Count() / simulateds.Count() * 100;
            }

            Console.WriteLine(simulateds);
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

        private double getPrice(int tid)
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
            else if(subNameBPs.Length == subNameSPs.Length && subNameBPs.Length != 0)
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
            else if(subNameBPs.Length < subNameSPs.Length && subNameBPs.Length != 0)
            {
                Func<string, bool> func = (name) =>
                {
                    return Array.Exists(subNameSPs, nameSP => nameSP == name);
                };

                if (subNameBPs.Except(subNameSPs.Where(func)).Count() == 0)
                {
                    return -1;
                }
            }
            else if(subNameSPs.Length != 0)
            {
                Func<string, bool> func = (name) =>
                {
                    return Array.Exists(subNameBPs, nameBP => nameBP == name);
                };

                if (subNameSPs.Except(subNameBPs.Where(func)).Count() == 0)
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
            
            if(interval > 0){
                //int shiftIndex = int.Parse(name.Split('(')[2][0].ToString());
                List<String> shifts = Regex.Matches(newName, @"[0-9]").Cast<Match>().Select(m => m.Value).ToList();
                List<String> letter = Regex.Matches(newName, @"[a-fA-F]").Cast<Match>().Select(m => m.Value).ToList();
                int shift = int.Parse(shifts[0]);
                newName =  $"{letter[0]}({shift - interval})";
            }
            return newName;
        }

        #endregion
    }
}
