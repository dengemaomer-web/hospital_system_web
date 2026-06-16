using HospitalSystem.Models.Entities;

namespace HospitalSystem.Services.Interfaces;

public interface IMedicineSuggestionService
{
    Task<List<string>> SuggestAsync(string diagnosisName);
    Task<List<Diagnosis>> GetDiagnosesAsync();
}
