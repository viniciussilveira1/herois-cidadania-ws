using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class AssessmentRequest
{
    [JsonPropertyName("studentName")]
    public string StudentName { get; set; } = string.Empty;

    [JsonPropertyName("schoolName")]
    public string SchoolName { get; set; } = string.Empty;

    [JsonPropertyName("gradeYear")]
    public string GradeYear { get; set; } = string.Empty;

    [JsonPropertyName("finalScore")]
    public int FinalScore { get; set; }

    [JsonPropertyName("sentAt")]
    public string SentAt { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("responses")]
    public List<ResponseData> Responses { get; set; } = new();
}

public class ResponseData
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("choiceType")]
    public string ChoiceType { get; set; } = string.Empty;
}
