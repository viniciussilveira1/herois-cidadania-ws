using System.Text.Json.Serialization;
using System.Collections.Generic;

public class AssessmentResponseDto
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; }

    // "correct" | "neutral" | "wrong"
    [JsonPropertyName("choiceType")]
    public string ChoiceType { get; set; }
}

public class AssessmentRequest
{
    [JsonPropertyName("studentName")]
    public string StudentName { get; set; }

    [JsonPropertyName("finalScore")]
    public int FinalScore { get; set; }

    [JsonPropertyName("responses")]
    public List<AssessmentResponseDto> Responses { get; set; }
}
