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
    public class RoomSchedulesController : ControllerBase
    {
        private readonly AuditGeneratorDbContext _context;

        public RoomSchedulesController(AuditGeneratorDbContext context)
        {
            _context = context;
        }

        // GET: api/RoomSchedules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoomSchedule>>> GetRoomSchedules()
        {
            return await _context.RoomSchedules.ToListAsync();
        }

        // GET: api/RoomSchedules/5
        [HttpGet("{id}")]
        public async Task<ActionResult<RoomSchedule>> GetRoomSchedule(int? id)
        {
            var roomSchedule = await _context.RoomSchedules.FindAsync(id);

            if (roomSchedule == null)
            {
                return NotFound();
            }

            return roomSchedule;
        }

        // PUT: api/RoomSchedules/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutRoomSchedule(int? id, RoomSchedule roomSchedule)
        {
            if (id != roomSchedule.Id)
            {
                return BadRequest();
            }

            _context.Entry(roomSchedule).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomScheduleExists(id))
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

        // POST: api/RoomSchedules
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<RoomSchedule>> PostRoomSchedule(RoomSchedule roomSchedule)
        {
            _context.RoomSchedules.Add(roomSchedule);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetRoomSchedule", new { id = roomSchedule.Id }, roomSchedule);
        }

        // DELETE: api/RoomSchedules/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoomSchedule(int? id)
        {
            var roomSchedule = await _context.RoomSchedules.FindAsync(id);
            if (roomSchedule == null)
            {
                return NotFound();
            }

            _context.RoomSchedules.Remove(roomSchedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool RoomScheduleExists(int? id)
        {
            return _context.RoomSchedules.Any(e => e.Id == id);
        }
    }
}
