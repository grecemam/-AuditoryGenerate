using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuditGeneratorAPI.Models;

namespace AuditGeneratorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WeekdaysController : ControllerBase
    {
        private readonly AuditGeneratorDbContext _context;

        public WeekdaysController(AuditGeneratorDbContext context)
        {
            _context = context;
        }

        // GET: api/Weekdays
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Weekday>>> GetWeekdays()
        {
            return await _context.Weekdays.ToListAsync();
        }

        // GET: api/Weekdays/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Weekday>> GetWeekday(int? id)
        {
            var weekday = await _context.Weekdays.FindAsync(id);

            if (weekday == null)
            {
                return NotFound();
            }

            return weekday;
        }

        // PUT: api/Weekdays/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWeekday(int? id, Weekday weekday)
        {
            if (id != weekday.Id)
            {
                return BadRequest();
            }

            _context.Entry(weekday).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WeekdayExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Weekdays
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Weekday>> PostWeekday(Weekday weekday)
        {
            _context.Weekdays.Add(weekday);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWeekday", new { id = weekday.Id }, weekday);
        }

        // DELETE: api/Weekdays/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWeekday(int? id)
        {
            var weekday = await _context.Weekdays.FindAsync(id);
            if (weekday == null)
            {
                return NotFound();
            }

            _context.Weekdays.Remove(weekday);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WeekdayExists(int? id)
        {
            return _context.Weekdays.Any(e => e.Id == id);
        }
    }
}
