using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Persistance;
using Domain.Dto;
using Domain.Enums;
using System.Globalization;
using Domain.Constants;
using System.IO;
using Newtonsoft.Json;
using System.Data;

namespace TaxesManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaxesController : ControllerBase
    {
        private readonly TaxesManagerDbContext _context;

        public TaxesController(TaxesManagerDbContext context)
        {
            _context = context;
        }

        // GET: api/Taxes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tax>>> GetTaxes()
        {
            return await _context.Taxes.ToListAsync();
        }

        // GET: api/Taxes/Vilnius/2020-09-01/2020-09-07
        [HttpGet("{municipality}/{startDate}/{endDate}")]
        public async Task<ActionResult<Tax>> GetTax(string municipality, DateTime startDate, DateTime endDate)
        {
            var tax = await _context.Taxes.FindAsync(municipality, startDate, endDate);
            if (tax == null)
            {
                return NotFound(Messages.NotFound);
            }

            return tax;
        }

        // POST: api/Taxes/Yearly
        [HttpPost("Yearly")]
        public async Task<ActionResult<Tax>> PostTaxYearly(TaxDto taxDto)
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

        // POST: api/Taxes/Monthly
        [HttpPost("Monthly")]
        public async Task<ActionResult<Tax>> PostTaxMonthly(TaxDto taxDto)
        {
            try
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
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(Messages.MonthOutOfRange);
            }
        }

        // POST: api/Taxes/Weekly
        [HttpPost("Weekly")]
        public async Task<ActionResult<Tax>> PostTaxWeekly(TaxDto taxDto)
        {
            try
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
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(Messages.WeeOutOfRange);
            }
        }

        // POST: api/Taxes/Daily
        [HttpPost("Daily")]
        public async Task<ActionResult<Tax>> PostTaxDaily(TaxDto taxDto)
        {
            try
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
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(Messages.MonthOrDayOutOfRange);
            }
        }

        private async Task<ActionResult<Tax>> AddTax(Tax tax)
        {
            if (!ValidTaxAmount(tax.TaxAmount))
            {
                return BadRequest(Messages.TaxAmountOutOfRange);
            }
            if (tax.Municipality == null || tax.StartDate == null || tax.EndDate == null)
            {
                return Conflict(Messages.MissingPrimaryKey);
            }
            if (TaxExists(tax.Municipality, tax.StartDate, tax.EndDate))
            {
                return Conflict(Messages.ExistingTax);
            }

            try
            {
                _context.Taxes.Add(tax);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict();
            }

            return CreatedAtAction("GetTax", new { municipality = tax.Municipality, startDate = tax.StartDate, endDate = tax.EndDate }, tax);
        }

        // PUT: api/Taxes/Vilnius/2020-09-01/2020-09-07
        [HttpPut("{municipality}/{startDate}/{endDate}")]
        public async Task<IActionResult> PutTax(string municipality, DateTime startDate, DateTime endDate, [FromBody] double taxAmount)
        {
            if (!ValidTaxAmount(taxAmount))
            {
                return BadRequest(Messages.TaxAmountOutOfRange);
            }
            var result = await GetTax(municipality, startDate, endDate);
            if (result.Value != null)
            {
                Tax tax = result.Value;
                tax.TaxAmount = taxAmount;
                _context.Entry(tax).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }

                return Ok("Ok");
            }
            else
            {
                return NotFound(Messages.NotFound);
            }
        }

        // DELETE: api/Taxes/Vilnius/2020-09-01/2020-09-07
        [HttpDelete("{municipality}/{startDate}/{endDate}")]
        public async Task<ActionResult<Tax>> DeleteTax(string municipality, DateTime startDate, DateTime endDate)
        {
            var tax = await _context.Taxes.FindAsync(municipality, startDate, endDate);
            if (tax == null)
            {
                return NotFound(Messages.NotFound);
            }

            _context.Taxes.Remove(tax);
            await _context.SaveChangesAsync();

            return tax;
        }

        // GET: api/Taxes/Amount?municipality=Vilnius&date=2021-09-18
        [HttpGet("Amount")]
        public async Task<ActionResult<Double>> GetTaxAmount(string municipality, string date)
        {
            try
            {
                DateTime dateTime = Convert.ToDateTime(date);
                var tax = (from t in _context.Taxes
                           where t.Municipality.StartsWith(municipality)
                                && (t.StartDate.Month <= dateTime.Month && t.StartDate.Day <= dateTime.Day)
                                && (t.EndDate.Month >= dateTime.Month && t.EndDate.Day >= dateTime.Day)
                           orderby t.Frequency descending
                           select t).FirstOrDefault();

                if (tax == null)
                {
                    return NotFound(Messages.NotFound);
                }
                return tax.TaxAmount;
            }
            catch (FormatException)
            {
                return BadRequest(Messages.InvalidDateFormat);
            }           
        }

        // POST: api/Taxes/Import
        [HttpPost("Import")]
        public async Task<IActionResult> ImportTaxes([FromForm(Name = "file")] IFormFile file)
        {
            if (JsonFile(file))
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest();
                }

                var reader = new StreamReader(file.OpenReadStream());
                var jsonData = await reader.ReadToEndAsync();
                List<TaxDto> data = JsonConvert.DeserializeObject<List<TaxDto>>(jsonData);
                int taxesCounter = 0;

                for (int i = 0; i < data.Count; i++)
                {
                    try
                    {
                        if (data[i].Day != 0)
                        {
                            var result = await PostTaxDaily(data[i]);
                            if (JsonConvert.SerializeObject(result.Result).Contains("201")) taxesCounter++;
                        }
                        else if (data[i].Week != 0)
                        {
                            var result = await PostTaxWeekly(data[i]);
                            if (JsonConvert.SerializeObject(result.Result).Contains("201")) taxesCounter++;
                        }
                        else if (data[i].Month != 0)
                        {
                            var result = await PostTaxMonthly(data[i]);
                            if (JsonConvert.SerializeObject(result.Result).Contains("201")) taxesCounter++;
                        }
                        else if(data[i]. Year != 0)
                        {
                            var result = await PostTaxYearly(data[i]);
                            if (JsonConvert.SerializeObject(result.Result).Contains("201")) taxesCounter++;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                string message = "Imported " + taxesCounter + " out of " + data.Count;
                return Ok(message);
            }
            else
            {
                return BadRequest(Messages.InvalidFileExtension);
            }
        }
        private bool TaxExists(string municipality, DateTime startDate, DateTime endDate)
        {
            return _context.Taxes.Any(e => e.Municipality == municipality && e.StartDate == startDate && e.EndDate == endDate);
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
