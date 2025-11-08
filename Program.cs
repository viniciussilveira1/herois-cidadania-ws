using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Cria o builder
var builder = WebApplication.CreateBuilder(args);

// Libera CORS (pra depois o Unity ou outro cliente poder chamar)
builder.Services.AddCors(p => p.AddDefaultPolicy(
    x => x.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
));

var app = builder.Build();
app.UseCors();

// Pasta onde os resultados serão salvos
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataDir);

// POST /api/assessment
// Espera um JSON assim:
// {
//   "studentName": "Ana Souza",
//   "finalScore": 80,
//   "responses": [
//     { "questionId": "Q1", "choiceType": "correct" },
//     { "questionId": "Q2", "choiceType": "wrong" }
//   ]
// }
app.MapPost("/api/assessment", async (AssessmentRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.StudentName))
        return Results.BadRequest(new { error = "studentName is required" });

    if (req.Responses == null || req.Responses.Count == 0)
        return Results.BadRequest(new { error = "responses is required" });

    // Sanitiza nome pra usar no arquivo
    var safeName = new string(req.StudentName
        .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ' ')
        .ToArray());

    if (string.IsNullOrWhiteSpace(safeName))
        safeName = "Aluno";

    var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safeName}.json";
    var path = Path.Combine(dataDir, fileName);

    var options = new JsonSerializerOptions { WriteIndented = true };
    var json = JsonSerializer.Serialize(req, options);

    await File.WriteAllTextAsync(path, json);

    Console.WriteLine($"[WS] Salvo {fileName}");

    return Results.Ok(new { ok = true, file = fileName });
});

app.Run();
