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

    public void UpdateTechnicalService(TechnicalService svc)
    {
        lock (_lock)
        {
            if (_technicalServices.Any(s => s.ServiceCode == svc.ServiceCode && s.TechnicalServiceId != svc.TechnicalServiceId))
                throw MedDomainException.Constraint("UQ_technical_services_code", 2627, $"Mã dịch vụ '{svc.ServiceCode}' đã tồn tại.");
            var idx = _technicalServices.FindIndex(s => s.TechnicalServiceId == svc.TechnicalServiceId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_technical_services", 547, "Dịch vụ kỹ thuật không tồn tại.");
            _technicalServices[idx] = svc;
            RaiseStateChanged();
        }
    }

    public void RemoveTechnicalService(Guid technicalServiceId)
    {
        lock (_lock)
        {
            var idx = _technicalServices.FindIndex(s => s.TechnicalServiceId == technicalServiceId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_technical_services", 547, "Dịch vụ kỹ thuật không tồn tại.");
            _technicalServices[idx] = _technicalServices[idx] with { Status = "archived", UpdatedAt = DateTime.UtcNow };
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

    public void UpdateResourceCatalogItem(ResourceCatalogItem item)
    {
        lock (_lock)
        {
            if (_resourceCatalog.Any(r => r.ResourceCode == item.ResourceCode && r.ResourceId != item.ResourceId))
                throw MedDomainException.Constraint("UQ_resource_catalog_code", 2627, $"Mã nguồn lực '{item.ResourceCode}' đã tồn tại.");
            var idx = _resourceCatalog.FindIndex(r => r.ResourceId == item.ResourceId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_resource_catalog", 547, "Tài nguyên không tồn tại.");
            _resourceCatalog[idx] = item;
            RaiseStateChanged();
        }
    }

    public void RemoveResourceCatalogItem(Guid resourceId)
    {
        lock (_lock)
        {
            var idx = _resourceCatalog.FindIndex(r => r.ResourceId == resourceId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_resource_catalog", 547, "Tài nguyên không tồn tại.");
            _resourceCatalog[idx] = _resourceCatalog[idx] with { Status = "archived", UpdatedAt = DateTime.UtcNow };
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

    public void RemoveTechnicalResourceNorm(Guid normId)
    {
        lock (_lock)
        {
            var removed = _technicalResourceNorms.RemoveAll(n => n.TechnicalResourceNormId == normId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_technical_resource_norms", 547, "Định mức dịch vụ không tồn tại.");
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

    public void RemoveProcedureVersionResourceNorm(Guid normId)
    {
        lock (_lock)
        {
            var removed = _procedureVersionResourceNorms.RemoveAll(n => n.ProcedureVersionResourceNormId == normId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_procedure_version_resource_norms", 547, "Định mức phiên bản quy trình không tồn tại.");
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

    public void UpdateTechnicalOrder(TechnicalOrder order)
    {
        lock (_lock)
        {
            var idx = _technicalOrders.FindIndex(o => o.TechnicalOrderId == order.TechnicalOrderId);
            if (idx < 0)
                throw MedDomainException.Constraint("PK_technical_orders", 547, "Chỉ định kỹ thuật không tồn tại.");
            _technicalOrders[idx] = order;
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

    public void RemoveActualResourceUsage(Guid usageId)
    {
        lock (_lock)
        {
            var removed = _actualResourceUsages.RemoveAll(u => u.ActualResourceUsageId == usageId);
            if (removed == 0)
                throw MedDomainException.Constraint("PK_actual_resource_usages", 547, "Ghi nhận sử dụng thực tế không tồn tại.");
            RaiseStateChanged();
        }
    }
}
