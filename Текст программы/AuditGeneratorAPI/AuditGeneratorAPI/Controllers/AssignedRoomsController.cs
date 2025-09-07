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
    public class AssignedRoomsController : ControllerBase
    {
        private readonly AuditGeneratorDbContext _context;

        public AssignedRoomsController(AuditGeneratorDbContext context)
        {
            _context = context;
        }

        // GET: api/AssignedRooms
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAssignedRooms()
        {
            var assignedRooms = await _context.AssignedRooms
                .Include(ar => ar.Room)
                .Include(ar => ar.Teacher)
                .Select(ar => new
                {
                    Id = ar.Id,
                    RoomId = ar.RoomId,
                    RoomNumber = ar.Room.RoomNumber,
                    CampusId = ar.Room.CampusId,
                    TeacherId = ar.TeacherId,
                    TeacherName = ar.Teacher.FullName
                })
                .ToListAsync();

            return Ok(assignedRooms);
        }


        // GET: api/AssignedRooms/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AssignedRoom>> GetAssignedRoom(int id)
        {
            var assignedRoom = await _context.AssignedRooms.FindAsync(id);

            if (assignedRoom == null)
            {
                return NotFound();
            }

            return assignedRoom;
        }

        // POST: api/AssignedRooms
        [HttpPost]
        public async Task<ActionResult<AssignedRoom>> PostAssignedRoom(AssignedRoom assignedRoom)
        {
            var room = await _context.Rooms.FindAsync(assignedRoom.RoomId);
            if (room == null)
            {
                return BadRequest("Аудитория не найдена.");
            }

            assignedRoom.Room = room; // Привязываем комнату к модели
            _context.AssignedRooms.Add(assignedRoom);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAssignedRoom", new { id = assignedRoom.Id }, assignedRoom);
        }


        // PUT: api/AssignedRooms/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssignedRoom(int id, AssignedRoom assignedRoom)
        {
            if (id != assignedRoom.Id)
            {
                return BadRequest();
            }

            var room = await _context.Rooms.FindAsync(assignedRoom.RoomId);
            if (room == null)
            {
                return BadRequest("Аудитория не найдена.");
            }

            assignedRoom.Room = room; // Привязываем комнату к модели

            _context.Entry(assignedRoom).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.AssignedRooms.Any(e => e.Id == id))
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


        // DELETE: api/AssignedRooms/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssignedRoom(int id)
        {
            var assignedRoom = await _context.AssignedRooms.FindAsync(id);
            if (assignedRoom == null)
            {
                return NotFound();
            }

            _context.AssignedRooms.Remove(assignedRoom);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}