using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(p => p.AddDefaultPolicy(
    x => x.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
));

// HttpClient singleton
builder.Services.AddHttpClient();

var app = builder.Build();
app.UseCors();

// Pasta local (debug / opcional)
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataDir);

// Lê configs do Firebase (setadas no Render depois)
var firebaseDbUrl = Environment.GetEnvironmentVariable("FIREBASE_DB_URL");      // ex: https://herois-da-cidadania-default-rtdb.firebaseio.com
var firebaseDbSecret = Environment.GetEnvironmentVariable("FIREBASE_DB_SECRET"); // se usar ?auth=...

app.MapPost("/api/assessment", async (AssessmentRequest req, IHttpClientFactory httpFactory) =>
{
    if (string.IsNullOrWhiteSpace(req.StudentName))
        return Results.BadRequest(new { error = "studentName is required" });

    if (req.Responses == null || req.Responses.Count == 0)
        return Results.BadRequest(new { error = "responses is required" });

    // --------- 1) Salva localmente (útil pra debug) ----------
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
    Console.WriteLine($"[WS] Local salvo {fileName}");

    // --------- 2) Envia pro Firebase Realtime Database ----------
    if (!string.IsNullOrWhiteSpace(firebaseDbUrl))
    {
        try
        {
            var client = httpFactory.CreateClient();

            // cada envio vira um nó com push-id em /assessments
            var url = $"{firebaseDbUrl.TrimEnd('/')}/assessments.json";

            // se estiver usando secret/token:
            if (!string.IsNullOrWhiteSpace(firebaseDbSecret))
                url += $"?auth={firebaseDbSecret}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);
            if (resp.IsSuccessStatusCode)
            {
                Console.WriteLine("[WS] Enviado pro Firebase com sucesso");
            }
            else
            {
                var body = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[WS] Erro Firebase: {resp.StatusCode} - {body}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] Erro ao enviar pro Firebase: {ex.Message}");
        }
    }

    return Results.Ok(new { ok = true, file = fileName });
});

app.Run();
