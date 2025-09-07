using AuditGeneratorAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen; 

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AuditGeneratorAPI.Models.AuditGeneratorDbContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("con")));

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuditGeneratorAPI", Version = "v1" });
    c.OperationFilter<SwaggerFileOperationFilter>(); // <-- добавили поддержку файлов
});


builder.Services.AddHostedService<EventCleanupService>();
builder.Services.AddHostedService<StudyPracticeCleanupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuditGeneratorAPI v1");
    });
}
app.UseCors();
app.UseHttpsRedirection();

app.UseStaticFiles(); // ← вот это ВАЖНО

app.UseRouting();
app.UseAuthorization();


app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
