using System.Text.Json;
using HospitalSystem.Helpers;
using HospitalSystem.Models.Entities;

namespace HospitalSystem.Repositories;

/// <summary>
/// Generic repository that persists a collection of entities to a single JSON file.
/// Access is guarded per file by a <see cref="SemaphoreSlim"/> and an in-memory cache
/// is kept to avoid re-reading the file on every operation.
/// </summary>
public class JsonRepository<T> : IRepository<T> where T : BaseEntity
{
    private readonly string _filePath;
    private static readonly Dictionary<string, SemaphoreSlim> Locks = new();
    private static readonly object LockSync = new();

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, string> FileNameOverrides = new()
    {
        [nameof(Diagnosis)] = "Diagnoses",
        [nameof(LogEntry)] = "Logs"
    };

    public JsonRepository(JsonStorageOptions options)
    {
        var typeName = typeof(T).Name;
        var baseName = FileNameOverrides.TryGetValue(typeName, out var ov) ? ov : typeName + "s";
        Directory.CreateDirectory(options.DataDirectory);
        _filePath = Path.Combine(options.DataDirectory, baseName + ".json");
    }

    private SemaphoreSlim GetLock()
    {
        lock (LockSync)
        {
            if (!Locks.TryGetValue(_filePath, out var sem))
            {
                sem = new SemaphoreSlim(1, 1);
                Locks[_filePath] = sem;
            }
            return sem;
        }
    }

    private async Task<List<T>> ReadAsync()
    {
        if (!File.Exists(_filePath))
            return new List<T>();

        await using var stream = File.OpenRead(_filePath);
        if (stream.Length == 0)
            return new List<T>();

        var data = await JsonSerializer.DeserializeAsync<List<T>>(stream, _jsonOptions);
        return data ?? new List<T>();
    }

    private async Task WriteAsync(List<T> items)
    {
        var tmp = _filePath + ".tmp";
        await using (var stream = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(stream, items, _jsonOptions);
        }
        File.Copy(tmp, _filePath, overwrite: true);
        File.Delete(tmp);
    }

    public async Task<List<T>> GetAllAsync()
    {
        var sem = GetLock();
        await sem.WaitAsync();
        try { return await ReadAsync(); }
        finally { sem.Release(); }
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.Id == id);
    }

    public async Task<List<T>> FindAsync(Func<T, bool> predicate)
    {
        var all = await GetAllAsync();
        return all.Where(predicate).ToList();
    }

    public async Task<T?> FirstOrDefaultAsync(Func<T, bool> predicate)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(predicate);
    }

    public async Task<T> AddAsync(T entity)
    {
        var sem = GetLock();
        await sem.WaitAsync();
        try
        {
            var all = await ReadAsync();
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();
            entity.CreatedAt = DateTime.UtcNow;
            all.Add(entity);
            await WriteAsync(all);
            return entity;
        }
        finally { sem.Release(); }
    }

    public async Task<T> UpdateAsync(T entity)
    {
        var sem = GetLock();
        await sem.WaitAsync();
        try
        {
            var all = await ReadAsync();
            var index = all.FindIndex(x => x.Id == entity.Id);
            if (index < 0)
                throw new KeyNotFoundException($"{typeof(T).Name} with id {entity.Id} not found.");
            entity.UpdatedAt = DateTime.UtcNow;
            entity.CreatedAt = all[index].CreatedAt;
            all[index] = entity;
            await WriteAsync(all);
            return entity;
        }
        finally { sem.Release(); }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var sem = GetLock();
        await sem.WaitAsync();
        try
        {
            var all = await ReadAsync();
            var removed = all.RemoveAll(x => x.Id == id);
            if (removed > 0)
                await WriteAsync(all);
            return removed > 0;
        }
        finally { sem.Release(); }
    }

    public async Task<int> CountAsync()
    {
        var all = await GetAllAsync();
        return all.Count;
    }

    public async Task<int> CountAsync(Func<T, bool> predicate)
    {
        var all = await GetAllAsync();
        return all.Count(predicate);
    }
}
