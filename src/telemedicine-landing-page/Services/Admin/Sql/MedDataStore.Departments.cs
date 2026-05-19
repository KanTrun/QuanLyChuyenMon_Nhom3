using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

public sealed partial class MedDataStore
{
    public void AddDepartment(Department dept)
    {
        lock (_lock)
        {
            if (_departments.Any(d => d.Code == dept.Code))
                throw MedDomainException.Constraint(
                    "UQ_departments_code", 2627, $"Mã khoa/phòng '{dept.Code}' đã tồn tại.");

            if (dept.ParentDepartmentId.HasValue &&
                !_departments.Any(d => d.DepartmentId == dept.ParentDepartmentId.Value))
                throw MedDomainException.Constraint(
                    "FK_departments_parent", 547, "Khoa/phòng cha không tồn tại.");

            _departments.Add(dept);

            // Cạnh tự tham chiếu (self-edge)
            _closure.Add(new DepartmentClosureEdge
            {
                AncestorDepartmentId = dept.DepartmentId,
                DescendantDepartmentId = dept.DepartmentId,
                Depth = 0
            });

            // Cạnh tổ tiên từ khoa/phòng cha
            if (dept.ParentDepartmentId.HasValue)
            {
                var parentEdges = _closure
                    .Where(e => e.DescendantDepartmentId == dept.ParentDepartmentId.Value)
                    .ToList();
                foreach (var pe in parentEdges)
                {
                    _closure.Add(new DepartmentClosureEdge
                    {
                        AncestorDepartmentId = pe.AncestorDepartmentId,
                        DescendantDepartmentId = dept.DepartmentId,
                        Depth = pe.Depth + 1
                    });
                }
            }

            RaiseStateChanged();
        }
    }

    public void UpdateDepartment(Department dept)
    {
        lock (_lock)
        {
            var current = _departments.FirstOrDefault(d => d.DepartmentId == dept.DepartmentId)
                ?? throw MedDomainException.Constraint(
                    "PK_departments", 547, "Khoa/phòng không tồn tại.");

            if (_departments.Any(d => d.DepartmentId != dept.DepartmentId && d.Code == dept.Code))
                throw MedDomainException.Constraint(
                    "UQ_departments_code", 2627, $"Mã khoa/phòng '{dept.Code}' đã tồn tại.");

            if (dept.ParentDepartmentId != current.ParentDepartmentId)
            {
                UpdateDepartmentParent(dept.DepartmentId, dept.ParentDepartmentId);
                current = _departments.First(d => d.DepartmentId == dept.DepartmentId);
            }

            var idx = _departments.IndexOf(current);
            _departments[idx] = current with
            {
                Code = dept.Code,
                Name = dept.Name,
                Status = dept.Status,
                UpdatedAt = DateTime.UtcNow
            };

            RaiseStateChanged();
        }
    }

    public void UpdateDepartmentParent(Guid departmentId, Guid? newParentId)
    {
        lock (_lock)
        {
            var dept = _departments.FirstOrDefault(d => d.DepartmentId == departmentId)
                ?? throw MedDomainException.Constraint(
                    "PK_departments", 547, "Khoa/phòng không tồn tại.");

            if (newParentId.HasValue)
            {
                // Bảo vệ vòng lặp: không thể di chuyển xuống con cháu của chính nó (lỗi 51021)
                var isDescendant = _closure.Any(e =>
                    e.AncestorDepartmentId == departmentId &&
                    e.DescendantDepartmentId == newParentId.Value);
                if (isDescendant)
                    throw MedDomainException.Constraint(
                        "TR_departments_update_parent_closure", 51021,
                        "Không thể di chuyển khoa/phòng xuống con cháu của chính nó.");

                if (!_departments.Any(d => d.DepartmentId == newParentId.Value))
                    throw MedDomainException.Constraint(
                        "FK_departments_parent", 547, "Khoa/phòng cha không tồn tại.");
            }

            // Lấy danh sách ID con cháu trong cây con
            var subtreeIds = _closure
                .Where(e => e.AncestorDepartmentId == departmentId)
                .Select(e => e.DescendantDepartmentId)
                .ToHashSet();

            // Xóa các cạnh tổ tiên cũ (cạnh mà descendant nằm trong cây con
            // và ancestor KHÔNG nằm trong cây con)
            _closure.RemoveAll(e =>
                subtreeIds.Contains(e.DescendantDepartmentId) &&
                !subtreeIds.Contains(e.AncestorDepartmentId));

            // Thêm các cạnh tổ tiên mới từ cha mới
            if (newParentId.HasValue)
            {
                var newParentAncestors = _closure
                    .Where(e => e.DescendantDepartmentId == newParentId.Value)
                    .ToList();

                var subtreeEdges = _closure
                    .Where(e => e.AncestorDepartmentId == departmentId &&
                                subtreeIds.Contains(e.DescendantDepartmentId))
                    .ToList();

                foreach (var ancestor in newParentAncestors)
                {
                    foreach (var sub in subtreeEdges)
                    {
                        _closure.Add(new DepartmentClosureEdge
                        {
                            AncestorDepartmentId = ancestor.AncestorDepartmentId,
                            DescendantDepartmentId = sub.DescendantDepartmentId,
                            Depth = ancestor.Depth + sub.Depth + 1
                        });
                    }
                }
            }

            // Cập nhật bản ghi khoa/phòng
            var idx = _departments.IndexOf(dept);
            _departments[idx] = dept with
            {
                ParentDepartmentId = newParentId,
                UpdatedAt = DateTime.UtcNow
            };

            RaiseStateChanged();
        }
    }

    public void ArchiveDepartment(Guid departmentId)
    {
        lock (_lock)
        {
            var dept = _departments.FirstOrDefault(d => d.DepartmentId == departmentId);

            if (dept is null || dept.Status != "active")
                throw MedDomainException.Constraint(
                    "sp_archive_department", 51023,
                    "Khoa/phòng không hoạt động hoặc không tồn tại.");

            var hasActiveChildren = _departments.Any(d =>
                d.ParentDepartmentId == departmentId && d.Status == "active");
            if (hasActiveChildren)
                throw MedDomainException.Constraint(
                    "sp_archive_department", 51024,
                    "Không thể lưu trữ khoa/phòng có con đang hoạt động. Hãy lưu trữ con trước.");

            var idx = _departments.IndexOf(dept);
            _departments[idx] = dept with { Status = "archived", UpdatedAt = DateTime.UtcNow };

            RaiseStateChanged();
        }
    }
}
