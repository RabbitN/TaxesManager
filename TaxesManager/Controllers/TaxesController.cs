using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Domain.Dto;
using Domain.Constants;
using Application.Taxes;
using System.Linq;

namespace TaxesManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaxesController : ControllerBase
    {
        private readonly ITaxCommand _taxCommands;

        public TaxesController(ITaxCommand taxCommands)
        {
            _taxCommands = taxCommands;
        }

        // GET: api/Taxes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tax>>> GetTaxes()
        {
            var taxes = await _taxCommands.GetTaxes();
            return taxes.ToList();
        }

        // GET: api/Taxes/Vilnius/2020-09-01/2020-09-07
        [HttpGet("{municipality}/{startDate}/{endDate}")]
        public async Task<ActionResult<Tax>> GetTax(string municipality, DateTime startDate, DateTime endDate)
        {
            var tax = await _taxCommands.GetTax(municipality, startDate, endDate);
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
            return await _taxCommands.PostTaxYearly(taxDto);
        }

        // POST: api/Taxes/Monthly
        [HttpPost("Monthly")]
        public async Task<ActionResult<Tax>> PostTaxMonthly(TaxDto taxDto)
        {
            try
            {
                return await _taxCommands.PostTaxMonthly(taxDto);
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
                return await _taxCommands.PostTaxWeekly(taxDto);
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
                return await _taxCommands.PostTaxDaily(taxDto);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest(Messages.MonthOrDayOutOfRange);
            }
        }

        // PUT: api/Taxes/Vilnius/2020-09-01/2020-09-07
        [HttpPut("{municipality}/{startDate}/{endDate}")]
        public async Task<ActionResult<Tax>> PutTax(string municipality, DateTime startDate, DateTime endDate, [FromBody] double taxAmount)
        {
            return await _taxCommands.PutTax(municipality, startDate, endDate, taxAmount);
        }

        // DELETE: api/Taxes/Vilnius/2020-09-01/2020-09-07
        [HttpDelete("{municipality}/{startDate}/{endDate}")]
        public async Task<ActionResult<Tax>> DeleteTax(string municipality, DateTime startDate, DateTime endDate)
        {
            var tax = await _taxCommands.DeleteTax(municipality, startDate, endDate);
            if (tax == null)
            {
                return NotFound(Messages.NotFound);
            }
            return tax;
        }

        // GET: api/Taxes/Amount?municipality=Vilnius&date=2021-09-18
        [HttpGet("Amount")]
        public async Task<ActionResult<Double>> GetTaxAmount(string municipality, string date)
        {
            try
            {
                Task<Tax> tax = _taxCommands.GetTaxAmount(municipality, date);

                if (tax.Result == null)
                {
                    return NotFound(Messages.NotFound);
                }
                return tax.Result.TaxAmount;
            }
            catch (AggregateException)
            {
                return BadRequest(Messages.InvalidDateFormat);
            }           
        }

        // POST: api/Taxes/Import
        [HttpPost("Import")]
        public async Task<IActionResult> ImportTaxes([FromForm(Name = "file")] IFormFile file)
        {
            var result = await _taxCommands.ImportTaxes(file);
            return Ok(result);
        }
    }
}
