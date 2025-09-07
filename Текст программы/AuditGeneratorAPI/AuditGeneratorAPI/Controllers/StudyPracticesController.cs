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
    public class StudyPracticesController : ControllerBase
    {
        private readonly AuditGeneratorDbContext _context;

        public StudyPracticesController(AuditGeneratorDbContext context)
        {
            _context = context;
        }

        // GET: api/StudyPractices
        // GET: api/StudyPractices
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudyPractice>>> GetStudyPractices()
        {
            var practices = await _context.StudyPractices
                .ToListAsync();

            return Ok(practices);
        }

        // GET: api/StudyPractices/5
        [HttpGet("{id}")]
        public async Task<ActionResult<StudyPractice>> GetStudyPractice(int? id)
        {
            var studyPractice = await _context.StudyPractices.FindAsync(id);

            if (studyPractice == null)
            {
                return NotFound();
            }

            return studyPractice;
        }

        // PUT: api/StudyPractices/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStudyPractice(int? id, StudyPractice studyPractice)
        {
            if (id != studyPractice.Id)
            {
                return BadRequest();
            }

            _context.Entry(studyPractice).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudyPracticeExists(id))
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

        // POST: api/StudyPractices
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<StudyPractice>> PostStudyPractice(StudyPractice studyPractice)
        {
            _context.StudyPractices.Add(studyPractice);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStudyPractice", new { id = studyPractice.Id }, studyPractice);
        }

        // DELETE: api/StudyPractices/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudyPractice(int? id)
        {
            var studyPractice = await _context.StudyPractices.FindAsync(id);
            if (studyPractice == null)
            {
                return NotFound();
            }

            _context.StudyPractices.Remove(studyPractice);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool StudyPracticeExists(int? id)
        {
            return _context.StudyPractices.Any(e => e.Id == id);
        }
    }
}
