using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace AuditGeneratorAPI.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    
    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FilesController(IWebHostEnvironment env)
        {
            _env = env;
        }
        [SwaggerIgnore]
        [HttpPost("upload/schedule")]
        [DisableRequestSizeLimit] 
        public async Task<IActionResult> UploadSchedule([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл пустой");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "files");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, "Расписание.json");

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            Console.WriteLine("🛠 WebRootPath: " + _env.WebRootPath);
            Console.WriteLine("🛠 uploadsFolder: " + uploadsFolder);

            return Ok(new { message = "Файл успешно загружен!" });
        }
        [SwaggerIgnore]
        [HttpPost("upload/excel")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadExcel([FromForm] IFormFile file, [FromForm] int campusId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл пустой");

            // Определяем папку для сохранения файлов
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "files");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Определяем имя файла в зависимости от ID корпуса
            string fileName = campusId == 1 ? "AuditFileNahimov.xlsx" : "AuditFileNezhka.xlsx";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // Сохраняем файл
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Логирование путей для отладки
            Console.WriteLine("🛠 WebRootPath: " + _env.WebRootPath);
            Console.WriteLine("🛠 uploadsFolder: " + uploadsFolder);
            Console.WriteLine($"🛠 File saved as: {filePath}");

            return Ok(new { message = "Excel файл успешно загружен!" });
        }
        [SwaggerIgnore]
        [HttpPost("upload/distributed")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadDistributedTeachers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл пустой");

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "files");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            string fileName = "distributed_teachers_today.json";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Console.WriteLine("✅ Файл распределения преподавателей загружен: " + filePath);

            return Ok(new { message = "Файл с распределением преподавателей успешно загружен!" });
        }
    }
}
