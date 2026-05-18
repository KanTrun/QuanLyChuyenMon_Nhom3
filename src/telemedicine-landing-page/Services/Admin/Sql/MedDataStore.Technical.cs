using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>Phần quản lý dịch vụ kỹ thuật, nguồn lực, đơn kỹ thuật, sử dụng thực tế.</summary>
public sealed partial class MedDataStore
{
    public void AddTechnicalService(TechnicalService svc)
    {
        lock (_lock)
        {
            if (_technicalServices.Any(s => s.ServiceCode == svc.ServiceCode))
                throw MedDomainException.Constraint("UQ_technical_services_code", 2627, $"Mã dịch vụ '{svc.ServiceCode}' đã tồn tại.");
            _technicalServices.Add(svc);
            RaiseStateChanged();
        }
    }

    public void AddResourceCatalogItem(ResourceCatalogItem item)
    {
        lock (_lock)
        {
            if (_resourceCatalog.Any(r => r.ResourceCode == item.ResourceCode))
                throw MedDomainException.Constraint("UQ_resource_catalog_code", 2627, $"Mã nguồn lực '{item.ResourceCode}' đã tồn tại.");
            _resourceCatalog.Add(item);
            RaiseStateChanged();
        }
    }

    public void AddTechnicalResourceNorm(TechnicalResourceNorm norm)
    {
        lock (_lock)
        {
            if (norm.StandardQuantity <= 0)
                throw MedDomainException.Constraint("CK_technical_resource_norms_qty", 50003, "Số lượng định mức phải lớn hơn 0.");
            _technicalResourceNorms.Add(norm);
            RaiseStateChanged();
        }
    }

    public void AddProcedureVersionResourceNorm(ProcedureVersionResourceNorm norm)
    {
        lock (_lock)
        {
            if (norm.StandardQuantity <= 0)
                throw MedDomainException.Constraint("CK_procedure_version_resource_norms_qty", 50003, "Số lượng định mức phải lớn hơn 0.");
            _procedureVersionResourceNorms.Add(norm);
            RaiseStateChanged();
        }
    }

    public void AddTechnicalOrder(TechnicalOrder order)
    {
        lock (_lock)
        {
            _technicalOrders.Add(order);
            RaiseStateChanged();
        }
    }

    public void AddResourceAvailabilitySnapshot(ResourceAvailabilitySnapshot snap)
    {
        lock (_lock)
        {
            ValidateJson(snap.ExternalPayloadJson, "external_payload");
            _resourceSnapshots.Add(snap);
            RaiseStateChanged();
        }
    }

    public void AddActualResourceUsage(ActualResourceUsage usage)
    {
        lock (_lock)
        {
            if (usage.ActualQuantity < 0)
                throw MedDomainException.Constraint("CK_actual_resource_usages_qty", 50003, "Số lượng sử dụng không được âm.");

            // TR_actual_resource_usages_set_final: khi đặt IsFinal=true,
            // hạ cấp bản ghi IsFinal=true khác cùng (order, resource)
            if (usage.IsFinal)
            {
                for (int i = 0; i < _actualResourceUsages.Count; i++)
                {
                    var existing = _actualResourceUsages[i];
                    if (existing.TechnicalOrderId == usage.TechnicalOrderId &&
                        existing.ResourceId == usage.ResourceId &&
                        existing.IsFinal &&
                        existing.ActualResourceUsageId != usage.ActualResourceUsageId)
                    {
                        _actualResourceUsages[i] = existing with { IsFinal = false };
                    }
                }
            }

            _actualResourceUsages.Add(usage);
            RaiseStateChanged();
        }
    }
}
