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
    public class WeekTypesController : ControllerBase
    {
        private readonly AuditGeneratorDbContext _context;

        public WeekTypesController(AuditGeneratorDbContext context)
        {
            _context = context;
        }

        // GET: api/WeekTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeekType>>> GetWeekTypes()
        {
            return await _context.WeekTypes.ToListAsync();
        }

        // GET: api/WeekTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WeekType>> GetWeekType(int? id)
        {
            var weekType = await _context.WeekTypes.FindAsync(id);

            if (weekType == null)
            {
                return NotFound();
            }

            return weekType;
        }

        // PUT: api/WeekTypes/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWeekType(int? id, WeekType weekType)
        {
            if (id != weekType.Id)
            {
                return BadRequest();
            }

            _context.Entry(weekType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WeekTypeExists(id))
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

        // POST: api/WeekTypes
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<WeekType>> PostWeekType(WeekType weekType)
        {
            _context.WeekTypes.Add(weekType);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWeekType", new { id = weekType.Id }, weekType);
        }

        // DELETE: api/WeekTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWeekType(int? id)
        {
            var weekType = await _context.WeekTypes.FindAsync(id);
            if (weekType == null)
            {
                return NotFound();
            }

            _context.WeekTypes.Remove(weekType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WeekTypeExists(int? id)
        {
            return _context.WeekTypes.Any(e => e.Id == id);
        }
    }
}
