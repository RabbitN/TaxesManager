using Domain.Dto;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Taxes
{
    public interface ITaxCommand
    {
        public Task<IEnumerable<Tax>> GetTaxes();
        public Task<Tax> GetTax(string municipality, DateTime startDate, DateTime endDate);
        public Task<Tax> PostTaxYearly(TaxDto taxDto);
        public Task<Tax> PostTaxMonthly(TaxDto taxDto);
        public Task<Tax> PostTaxWeekly(TaxDto taxDto);
        public Task<Tax> PostTaxDaily(TaxDto taxDto);
        public Task<Tax> PutTax(string municipality, DateTime startDate, DateTime endDate, double taxAmount);
        public Task<Tax> DeleteTax(string municipality, DateTime startDate, DateTime endDate);
        public Task<Tax> GetTaxAmount(string municipality, string date);
        public Task<string> ImportTaxes(IFormFile file);
    }
}
