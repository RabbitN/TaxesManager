namespace Domain.Dto
{
    public class TaxDto
    {
        public string Municipality { get; set; }
        public double TaxAmount { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int Week { get; set; }
        public int Day { get; set; }
    }
}
