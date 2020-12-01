using System;
using System.Collections.Generic;
using System.Linq;

namespace FTAPExcelTools
{
    public class Transaction
    {
        public int TID { get; set; }
        public string ItemSet { get; set; }

        //public char[] Data
        //{
        //    get
        //    {
        //        return Array.ConvertAll(ItemSet.Split(','), Char.Parse);
        //    }
        //    set
        //    {
        //        Data = value;
        //        ItemSet = String.Join(",", Data.Select(p => p.ToString()).ToArray());
        //    }
        //}
        public double Price { get; set; }
    }
}
