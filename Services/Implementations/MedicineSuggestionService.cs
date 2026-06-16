using HospitalSystem.Models.Entities;
using HospitalSystem.Repositories;
using HospitalSystem.Services.Interfaces;

namespace HospitalSystem.Services.Implementations;

/// <summary>
/// Smart medicine suggestion engine. Looks up the diagnosis (case-insensitive,
/// partial match) and returns its recommended medicines.
/// </summary>
public class MedicineSuggestionService : IMedicineSuggestionService
{
    private readonly IRepository<Diagnosis> _diagnoses;

    public MedicineSuggestionService(IRepository<Diagnosis> diagnoses)
    {
        _diagnoses = diagnoses;
    }

    public async Task<List<string>> SuggestAsync(string diagnosisName)
    {
        if (string.IsNullOrWhiteSpace(diagnosisName))
            return new List<string>();

        var all = await _diagnoses.GetAllAsync();
        var match = all.FirstOrDefault(d =>
                        string.Equals(d.Name, diagnosisName, StringComparison.OrdinalIgnoreCase))
                    ?? all.FirstOrDefault(d =>
                        d.Name.Contains(diagnosisName, StringComparison.OrdinalIgnoreCase) ||
                        diagnosisName.Contains(d.Name, StringComparison.OrdinalIgnoreCase));

        return match?.RecommendedMedicines ?? new List<string>();
    }

    public async Task<List<Diagnosis>> GetDiagnosesAsync()
    {
        var all = await _diagnoses.GetAllAsync();
        return all.OrderBy(d => d.Name).ToList();
    }
}
