using Domain.Constants;
using Domain.Dto;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Persistance;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Domain.Exceptions;

namespace Application.Taxes
{
    public class TaxCommand : ITaxCommand
    {
        private readonly TaxesManagerDbContext _context;

        public TaxCommand(TaxesManagerDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Tax>> GetTaxes()
        {
            return await _context.Taxes.ToListAsync();
        }
        public async Task<Tax> GetTax(string municipality, DateTime startDate, DateTime endDate)
        {
            return await _context.Taxes.FindAsync(municipality, startDate, endDate);
        }

        public async Task<Tax> PostTaxYearly(TaxDto taxDto)
        {
            Frequency frequency = Frequency.Yearly;

            // provide dates for yearly schedule 
            int year = DateTime.Now.Year;
            DateTime startDate = new DateTime(year, 1, 1);
            DateTime endDate = startDate.AddYears(1).AddTicks(-1).Date;

            Tax tax = new Tax()
            {
                Municipality = taxDto.Municipality,
                Frequency = frequency,
                StartDate = startDate,
                EndDate = endDate,
                TaxAmount = taxDto.TaxAmount
            };

            return await AddTax(tax);
        }

        public async Task<Tax> PostTaxMonthly(TaxDto taxDto)
        {
            Frequency frequency = Frequency.Monthly;

            // provide dates for monthly schedule 
            int year = DateTime.Now.Year;
            DateTime startDate = new DateTime(year, taxDto.Month, 1);
            DateTime endDate = new DateTime(year, taxDto.Month, DateTime.DaysInMonth(year, taxDto.Month));

            Tax tax = new Tax()
            {
                Municipality = taxDto.Municipality,
                Frequency = frequency,
                StartDate = startDate,
                EndDate = endDate,
                TaxAmount = taxDto.TaxAmount
            };

            return await AddTax(tax);
        }

        public async Task<Tax> PostTaxWeekly(TaxDto taxDto)
        {
            Frequency frequency = Frequency.Weekly;

            // provide dates for weekly schedule 
            int year = DateTime.Now.Year;
            DateTime startDate = ISOWeek.ToDateTime(year, taxDto.Week, DayOfWeek.Monday);
            DateTime endDate = ISOWeek.ToDateTime(year, taxDto.Week, DayOfWeek.Sunday);

            Tax tax = new Tax()
            {
                Municipality = taxDto.Municipality,
                Frequency = frequency,
                StartDate = startDate,
                EndDate = endDate,
                TaxAmount = taxDto.TaxAmount
            };

            return await AddTax(tax);
        }

        public async Task<Tax> PostTaxDaily(TaxDto taxDto)
        {
            Frequency frequency = Frequency.Daily;

            // provide date for daily schedule 
            int year = DateTime.Now.Year;
            DateTime date = new DateTime(year, taxDto.Month, taxDto.Day);

            Tax tax = new Tax()
            {
                Municipality = taxDto.Municipality,
                Frequency = frequency,
                StartDate = date,
                EndDate = date,
                TaxAmount = taxDto.TaxAmount
            };

            return await AddTax(tax);
        }

        private async Task<Tax> AddTax(Tax tax)
        {
            if (!ValidTaxAmount(tax.TaxAmount))
            {
                throw new TaxesManagerException(Messages.TaxAmountOutOfRange);
            }
            if (tax.Municipality == null || tax.StartDate == null || tax.EndDate == null)
            {
                throw new TaxesManagerException(Messages.MissingPrimaryKey);
            }
            if (TaxExists(tax.Municipality, tax.StartDate, tax.EndDate))
            {
                throw new TaxesManagerException(Messages.ExistingTax);
            }

            try
            {
                _context.Taxes.Add(tax);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new TaxesManagerException();
            }

            return tax;
        }

        public async Task<Tax> PutTax(string municipality, DateTime startDate, DateTime endDate, double taxAmount)
        {
            if (!ValidTaxAmount(taxAmount))
            {
                throw new TaxesManagerException(Messages.TaxAmountOutOfRange);
            }
            var result = await GetTax(municipality, startDate, endDate);
            if (result != null)
            {
                Tax tax = result;
                tax.TaxAmount = taxAmount;
                _context.Entry(tax).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw new TaxesManagerException();
                }

                return tax;
            }
            else
            {
                throw new TaxesManagerException(Messages.NotFound);
            }
        }

        public async Task<Tax> DeleteTax(string municipality, DateTime startDate, DateTime endDate)
        {
            var tax = await _context.Taxes.FindAsync(municipality, startDate, endDate);
            if (tax != null)
            {
                _context.Taxes.Remove(tax);
                await _context.SaveChangesAsync();
            }
            return tax;
        }

        public async Task<Tax> GetTaxAmount(string municipality, string date)
        {
            DateTime dateTime = Convert.ToDateTime(date);
            var tax = (from t in _context.Taxes
                       where t.Municipality.StartsWith(municipality)
                            && (t.StartDate.Month <= dateTime.Month && t.StartDate.Day <= dateTime.Day)
                            && (t.EndDate.Month >= dateTime.Month && t.EndDate.Day >= dateTime.Day)
                       orderby t.Frequency descending
                       select t).FirstOrDefault();
            return tax;
        }

        public async Task<string> ImportTaxes(IFormFile file)
        {
            if (JsonFile(file))
            {
                if (file == null || file.Length == 0)
                {
                    throw new TaxesManagerException("Bad request");
                }

                var reader = new StreamReader(file.OpenReadStream());
                var jsonData = await reader.ReadToEndAsync();
                List<TaxDto> data = JsonConvert.DeserializeObject<List<TaxDto>>(jsonData);
                int taxesCounter = 0;

                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].Day != 0)
                    {
                        try
                        {
                            var result = await PostTaxDaily(data[i]);
                            if (result != null) taxesCounter++;
                        }
                        catch (Exception)
                        { }
                    }
                    else if (data[i].Week != 0)
                    {
                        try
                        {
                            var result = await PostTaxWeekly(data[i]);
                            if (result != null) taxesCounter++;
                        }
                        catch (Exception)
                        { }
                    }
                    else if (data[i].Month != 0)
                    {
                        try
                        {
                            var result = await PostTaxMonthly(data[i]);
                            if (result != null) taxesCounter++;
                        }
                            catch (Exception)
                        { }
                }
                    else if (data[i].Year != 0)
                    {
                        try
                        {
                            var result = await PostTaxYearly(data[i]);
                            if (result != null) taxesCounter++;
                        }
                        catch (Exception)
                        { }
                    }
                }

                return ("Imported " + taxesCounter + " out of " + data.Count);
            }
            else
            {
                throw new TaxesManagerException(Messages.InvalidFileExtension);
            }
        }

        private bool TaxExists(string municipality, DateTime startDate, DateTime endDate)
        {
            if (_context.Taxes != null)
            {
                return _context.Taxes.Any(e => e.Municipality == municipality && e.StartDate == startDate && e.EndDate == endDate);
            }
            else return false;
        }

        private bool JsonFile(IFormFile file)
        {
            var extension = "." + file.FileName.Split('.')[file.FileName.Split('.').Length - 1];
            return (extension == ".json");
        }

        private bool ValidTaxAmount(double taxAmount)
        {
            return (taxAmount >= 0 && taxAmount <= 1);
        }
    }
}
