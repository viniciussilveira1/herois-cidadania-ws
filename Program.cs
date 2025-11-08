using System.Text.Json;
using System.Text;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

var firebaseDbUrl = Environment.GetEnvironmentVariable("FIREBASE_DB_URL");
var firebaseDbSecret = Environment.GetEnvironmentVariable("FIREBASE_DB_SECRET");

if (string.IsNullOrWhiteSpace(firebaseDbUrl) && string.IsNullOrWhiteSpace(firebaseDbSecret))
{
    try
    {
        DotNetEnv.Env.Load();
        firebaseDbUrl = Environment.GetEnvironmentVariable("FIREBASE_DB_URL");
        firebaseDbSecret = Environment.GetEnvironmentVariable("FIREBASE_DB_SECRET");
    }
    catch { }
}

builder.Services.AddCors(p => p.AddDefaultPolicy(
    x => x.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
));

builder.Services.AddHttpClient();

var app = builder.Build();
app.UseCors();

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataDir);

const int MaxBodySizeBytes = 50 * 1024;

app.MapPost("/api/assessment", async (HttpRequest request, IHttpClientFactory httpFactory) =>
{
    if (!request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? true)
        return Results.BadRequest(new { error = "Somente arquivo JSON é aceito" });

    if (request.ContentLength > MaxBodySizeBytes)
        return Results.BadRequest(new { error = "Payload muito grande (limite de 50KB)." });

    string body;
    using (var reader = new StreamReader(request.Body))
        body = await reader.ReadToEndAsync();

    if (string.IsNullOrWhiteSpace(body))
        return Results.BadRequest(new { error = "Corpo da requisição está vazio." });

    AssessmentRequest? req;
    try
    {
        req = JsonSerializer.Deserialize<AssessmentRequest>(body);
    }
    catch
    {
        return Results.BadRequest(new { error = "JSON malformado." });
    }

    if (req == null)
        return Results.BadRequest(new { error = "Corpo da requisição está vazio ou inválido (JSON malformado)." });
        
    var (isValid, errorMsg) = ValidateRequest(req);
    if (!isValid)
        return Results.BadRequest(new { error = errorMsg });

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

    Debug.WriteLine($"url env: {firebaseDbUrl}");
    if (!string.IsNullOrWhiteSpace(firebaseDbUrl))
    {
        try
        {
            var client = httpFactory.CreateClient();

            var url = $"{firebaseDbUrl.TrimEnd('/')}/assessments.json";
            if (!string.IsNullOrWhiteSpace(firebaseDbSecret))
                url += $"?auth={firebaseDbSecret}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await client.PostAsync(url, content);

            if (!resp.IsSuccessStatusCode)
            {
                var bodyResp = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[WS] Erro Firebase: {resp.StatusCode} - {bodyResp}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] Erro ao enviar pro Firebase: {ex.Message}");
        }
    }

    return Results.Ok(new { ok = true, file = fileName });
});

static (bool IsValid, string? Error) ValidateRequest(AssessmentRequest req)
{
    if (req == null)
        return (false, "Corpo da requisição está vazio ou inválido (JSON malformado).");

    if (string.IsNullOrWhiteSpace(req.StudentName))
        return (false, "Campo 'studentName' é obrigatório.");

    if (string.IsNullOrWhiteSpace(req.SchoolName))
        return (false, "Campo 'schoolName' é obrigatório.");

    if (string.IsNullOrWhiteSpace(req.GradeYear))
        return (false, "Campo 'gradeYear' é obrigatório.");

    if (req.Responses == null || req.Responses.Count == 0)
        return (false, "Campo 'responses' é obrigatório e precisa conter pelo menos 1 item.");

    foreach (var r in req.Responses)
    {
        if (string.IsNullOrWhiteSpace(r.QuestionId))
            return (false, "Campo 'questionId' é obrigatório em cada resposta.");
        if (string.IsNullOrWhiteSpace(r.ChoiceType))
            return (false, "Campo 'choiceType' é obrigatório em cada resposta.");
    }

    return (true, null);
}

app.Run();
