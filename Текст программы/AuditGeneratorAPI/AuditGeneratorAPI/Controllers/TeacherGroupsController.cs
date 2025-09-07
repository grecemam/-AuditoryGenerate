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
    public class TeacherGroupsController : ControllerBase
    {
        private readonly AuditGeneratorDbContext _context;

        public TeacherGroupsController(AuditGeneratorDbContext context)
        {
            _context = context;
        }

        // GET: api/TeacherGroups
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeacherGroup>>> GetTeacherGroups()
        {
            return await _context.TeacherGroups.ToListAsync();
        }

        // GET: api/TeacherGroups/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherGroup>> GetTeacherGroup(int? id)
        {
            var teacherGroup = await _context.TeacherGroups.FindAsync(id);

            if (teacherGroup == null)
            {
                return NotFound();
            }

            return teacherGroup;
        }

        // PUT: api/TeacherGroups/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTeacherGroup(int? id, TeacherGroup teacherGroup)
        {
            if (id != teacherGroup.Id)
            {
                return BadRequest();
            }

            _context.Entry(teacherGroup).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherGroupExists(id))
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

        // POST: api/TeacherGroups
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TeacherGroup>> PostTeacherGroup(TeacherGroup teacherGroup)
        {
            _context.TeacherGroups.Add(teacherGroup);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTeacherGroup", new { id = teacherGroup.Id }, teacherGroup);
        }

        // DELETE: api/TeacherGroups/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTeacherGroup(int? id)
        {
            var teacherGroup = await _context.TeacherGroups.FindAsync(id);
            if (teacherGroup == null)
            {
                return NotFound();
            }

            _context.TeacherGroups.Remove(teacherGroup);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TeacherGroupExists(int? id)
        {
            return _context.TeacherGroups.Any(e => e.Id == id);
        }
    }
}
