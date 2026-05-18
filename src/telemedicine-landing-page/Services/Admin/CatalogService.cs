using System.Globalization;
using System.Text;
using TelemedicineLandingPage.Models.Admin;

namespace TelemedicineLandingPage.Services.Admin;

/// <summary>
/// Default singleton catalog service for technical services. Persists data in
/// memory and exposes a stable CSV format used by both the export and import
/// paths so a round-trip preserves rows.
/// </summary>
public sealed class CatalogService : ICatalogService
{
    public static readonly string[] CsvHeader =
    {
        "ServiceCode", "ServiceName", "ServiceType", "Department", "Status",
        "ResourceType", "ResourceCode", "ResourceName", "Unit", "StandardQuantity", "Note",
    };

    private readonly object _gate = new();
    private readonly List<TechnicalServiceRecord> _items;

    public CatalogService()
    {
        _items = SeedData();
    }

    public event Action? StateChanged;

    public IReadOnlyList<TechnicalServiceRecord> Search(CatalogFilter filter)
    {
        lock (_gate)
        {
            IEnumerable<TechnicalServiceRecord> query = _items;
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var needle = filter.Search.Trim();
                query = query.Where(s =>
                    s.Name.Contains(needle, StringComparison.OrdinalIgnoreCase) ||
                    s.Code.Contains(needle, StringComparison.OrdinalIgnoreCase));
            }
            if (filter.ServiceType is { } st) query = query.Where(s => s.ServiceType == st);
            if (filter.Department is { } dept) query = query.Where(s => s.Department == dept);
            if (filter.Status is { } status) query = query.Where(s => s.Status == status);
            return query.OrderBy(s => s.Code).ToList();
        }
    }

    public TechnicalServiceRecord? GetById(Guid id)
    {
        lock (_gate) return _items.FirstOrDefault(s => s.Id == id);
    }

    public TechnicalServiceRecord Create(TechnicalServiceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var next = record with
        {
            Id = record.Id == Guid.Empty ? Guid.NewGuid() : record.Id,
            UpdatedAt = DateTime.Now,
        };
        lock (_gate) _items.Add(next);
        Raise();
        return next;
    }

    public TechnicalServiceRecord Update(Guid id, TechnicalServiceRecord updated)
    {
        ArgumentNullException.ThrowIfNull(updated);
        TechnicalServiceRecord next;
        lock (_gate)
        {
            var index = _items.FindIndex(s => s.Id == id);
            if (index < 0) throw new KeyNotFoundException($"Không tìm thấy kỹ thuật {id}.");
            next = updated with { Id = id, UpdatedAt = DateTime.Now };
            _items[index] = next;
        }
        Raise();
        return next;
    }

    public void Archive(Guid id)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(s => s.Id == id);
            if (index < 0) return;
            _items[index] = _items[index] with { Status = CatalogStatus.NgungSuDung, UpdatedAt = DateTime.Now };
        }
        Raise();
    }

    public void AddResourceNorm(Guid serviceId, ResourceNorm norm)
    {
        ArgumentNullException.ThrowIfNull(norm);
        lock (_gate)
        {
            var index = _items.FindIndex(s => s.Id == serviceId);
            if (index < 0) return;
            var current = _items[index];
            if (current.ResourceNorms.Any(r => string.Equals(r.ResourceCode, norm.ResourceCode, StringComparison.OrdinalIgnoreCase)))
            {
                return; // ignore duplicates by code
            }
            var nextNorms = current.ResourceNorms.Append(norm).ToList();
            _items[index] = current with { ResourceNorms = nextNorms, UpdatedAt = DateTime.Now };
        }
        Raise();
    }

    public void RemoveResourceNorm(Guid serviceId, string resourceCode)
    {
        if (string.IsNullOrWhiteSpace(resourceCode)) return;
        lock (_gate)
        {
            var index = _items.FindIndex(s => s.Id == serviceId);
            if (index < 0) return;
            var current = _items[index];
            var nextNorms = current.ResourceNorms
                .Where(r => !string.Equals(r.ResourceCode, resourceCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
            _items[index] = current with { ResourceNorms = nextNorms, UpdatedAt = DateTime.Now };
        }
        Raise();
    }

    public int ImportFromCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return 0;
        var normalised = csv.StartsWith('\uFEFF') ? csv.TrimStart('\uFEFF') : csv;
        var rows = AdminCsv.Parse(normalised);
        if (rows.Count <= 1) return 0;

        // Header row drives the column-to-field mapping so users can edit the file in Excel.
        var header = rows[0].Select(c => c.Trim()).ToArray();
        int IndexOf(string name)
        {
            for (var i = 0; i < header.Length; i++)
            {
                if (string.Equals(header[i], name, StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        int idxServiceCode = IndexOf("ServiceCode");
        int idxServiceName = IndexOf("ServiceName");
        int idxServiceType = IndexOf("ServiceType");
        int idxDepartment = IndexOf("Department");
        int idxStatus = IndexOf("Status");
        int idxResType = IndexOf("ResourceType");
        int idxResCode = IndexOf("ResourceCode");
        int idxResName = IndexOf("ResourceName");
        int idxUnit = IndexOf("Unit");
        int idxQty = IndexOf("StandardQuantity");
        int idxNote = IndexOf("Note");

        if (idxServiceCode < 0 || idxServiceName < 0)
        {
            throw new InvalidOperationException("CSV thiếu cột ServiceCode hoặc ServiceName.");
        }

        var grouped = new Dictionary<string, (TechnicalServiceRecord Service, List<ResourceNorm> Norms)>(StringComparer.OrdinalIgnoreCase);

        for (var r = 1; r < rows.Count; r++)
        {
            var row = rows[r];
            if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

            string Get(int idx) => idx >= 0 && idx < row.Count ? row[idx].Trim() : string.Empty;
            var serviceCode = Get(idxServiceCode);
            if (string.IsNullOrWhiteSpace(serviceCode)) continue;

            if (!grouped.TryGetValue(serviceCode, out var existing))
            {
                var record = new TechnicalServiceRecord
                {
                    Code = serviceCode,
                    Name = Get(idxServiceName),
                    ServiceType = ParseEnum(Get(idxServiceType), ServiceType.KyThuat),
                    Department = ParseEnum(Get(idxDepartment), Department.HanhChinh),
                    Status = ParseEnum(Get(idxStatus), CatalogStatus.HoatDong),
                    ResourceNorms = Array.Empty<ResourceNorm>(),
                };
                grouped[serviceCode] = (record, new List<ResourceNorm>());
            }

            var resCode = Get(idxResCode);
            if (!string.IsNullOrWhiteSpace(resCode))
            {
                var qtyText = Get(idxQty);
                if (!decimal.TryParse(qtyText, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty))
                {
                    decimal.TryParse(qtyText, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out qty);
                }
                grouped[serviceCode].Norms.Add(new ResourceNorm(
                    ParseEnum(Get(idxResType), ResourceType.VatTu),
                    resCode,
                    Get(idxResName),
                    Get(idxUnit),
                    qty,
                    Get(idxNote)));
            }
        }

        var imported = 0;
        lock (_gate)
        {
            foreach (var (code, (service, norms)) in grouped)
            {
                var index = _items.FindIndex(s => string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
                var record = service with { ResourceNorms = norms };
                if (index < 0)
                {
                    _items.Add(record with { Id = Guid.NewGuid(), UpdatedAt = DateTime.Now });
                }
                else
                {
                    var existing = _items[index];
                    var mergedNorms = existing.ResourceNorms
                        .Concat(norms.Where(n => !existing.ResourceNorms.Any(en => string.Equals(en.ResourceCode, n.ResourceCode, StringComparison.OrdinalIgnoreCase))))
                        .ToList();
                    _items[index] = existing with
                    {
                        Name = record.Name,
                        ServiceType = record.ServiceType,
                        Department = record.Department,
                        Status = record.Status,
                        ResourceNorms = mergedNorms,
                        UpdatedAt = DateTime.Now,
                    };
                }
                imported++;
            }
        }
        if (imported > 0) Raise();
        return imported;
    }

    public string ExportToCsv()
    {
        lock (_gate)
        {
            var sb = new StringBuilder();
            sb.Append('\uFEFF'); // UTF-8 BOM so Excel renders Vietnamese diacritics.
            sb.AppendLine(string.Join(',', CsvHeader));
            foreach (var service in _items.OrderBy(s => s.Code))
            {
                if (service.ResourceNorms.Count == 0)
                {
                    sb.AppendLine(string.Join(',', new[]
                    {
                        AdminCsv.Encode(service.Code),
                        AdminCsv.Encode(service.Name),
                        service.ServiceType.ToString(),
                        service.Department.ToString(),
                        service.Status.ToString(),
                        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    }));
                    continue;
                }
                foreach (var norm in service.ResourceNorms)
                {
                    sb.AppendLine(string.Join(',', new[]
                    {
                        AdminCsv.Encode(service.Code),
                        AdminCsv.Encode(service.Name),
                        service.ServiceType.ToString(),
                        service.Department.ToString(),
                        service.Status.ToString(),
                        norm.ResourceType.ToString(),
                        AdminCsv.Encode(norm.ResourceCode),
                        AdminCsv.Encode(norm.ResourceName),
                        AdminCsv.Encode(norm.Unit),
                        norm.StandardQuantity.ToString(CultureInfo.InvariantCulture),
                        AdminCsv.Encode(norm.Note),
                    }));
                }
            }
            return sb.ToString();
        }
    }

    private void Raise() => StateChanged?.Invoke();

    private static TEnum ParseEnum<TEnum>(string raw, TEnum fallback) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        return Enum.TryParse<TEnum>(raw, ignoreCase: true, out var value) ? value : fallback;
    }

    private static List<TechnicalServiceRecord> SeedData()
    {
        return new List<TechnicalServiceRecord>();
    }
}
