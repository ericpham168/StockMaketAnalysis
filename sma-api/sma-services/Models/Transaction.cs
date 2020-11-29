using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace sma_services.Models
{
    public class Transaction
    {
        [Key]
        public int TID { get; set; }
        public string ItemSet { get; set; }

        [NotMapped]
        public char[] Data
        {
            get
            {
                return Array.ConvertAll(ItemSet.Split(','), Char.Parse);
            }
            set
            {
                Data = value;
                ItemSet = String.Join(",", Data.Select(p => p.ToString()).ToArray());
            }
        }
        public double Price { get; set; }
    }
}
