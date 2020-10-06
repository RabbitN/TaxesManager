using Domain.Enums;
using System;

namespace Domain.Entities
{
    public class Tax
    {
        public string Municipality { get; set; }
        public Frequency Frequency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double TaxAmount { get; set; }
    }
}
