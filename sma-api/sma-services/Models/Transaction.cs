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
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TID { get; set; }
        public string ItemSet { get; set; }

        [NotMapped]
        public string[] Data
        {
            get
            {
                if (ItemSet != null)
                {
                    return ItemSet.Split(',');
                }
                else
                    return new string[0];
            }
            set
            {
                Data = value;
                ItemSet = String.Join(",", Data.Select(p => p.ToString()).ToArray());
            }
        }
        public double Price { get; set; }

        [ForeignKey("TickerID")]
        public int TickerID { get; set; }

        public TickerTranSaction TickerTranSaction { get; set; }
    }
}
