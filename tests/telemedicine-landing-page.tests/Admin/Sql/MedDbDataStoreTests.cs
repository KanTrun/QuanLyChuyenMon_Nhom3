using TelemedicineLandingPage.Models.Admin.Sql;
using TelemedicineLandingPage.Services.Admin.Sql;

namespace TelemedicineLandingPage.Tests.Admin.Sql;

public sealed class MedDbDataStoreTests
{
    [Fact]
    public void AddDepartment_DuplicateCode_ThrowsFriendlyDomainError()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);

        var ex = Assert.Throws<MedDomainException>(() =>
            store.AddDepartment(new Department
            {
                Code = "khoa-noi",
                Name = "Khoa Nội trùng mã"
            }));

        Assert.Equal(2627, ex.SqlErrorNumber);
        Assert.Contains("Mã khoa/phòng", ex.Message);
    }

    [Fact]
    public void ArchiveDepartment_WithActiveChildren_ThrowsFriendlyDomainError()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);

        var ex = Assert.Throws<MedDomainException>(() =>
            store.ArchiveDepartment(MedDataStoreSeed.RootDeptId));

        Assert.Equal(50021, ex.SqlErrorNumber);
        Assert.Contains("đơn vị con", ex.Message);
    }

    [Fact]
    public void AddRole_DuplicateCode_ThrowsFriendlyDomainError()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);

        var ex = Assert.Throws<MedDomainException>(() =>
            store.AddRole(new Role
            {
                Code = "system_admin",
                Name = "Vai trò trùng mã"
            }));

        Assert.Equal(2627, ex.SqlErrorNumber);
        Assert.Contains("Mã vai trò", ex.Message);
    }

    [Fact]
    public void AddGroup_NormalizesCodeAndValidatesDepartment()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);
        var groupId = Guid.NewGuid();

        store.AddGroup(new Group
        {
            GroupId = groupId,
            Code = "nhom-kiem-thu",
            Name = "Nhóm kiểm thử",
            DepartmentId = MedDataStoreSeed.DeptNoiId
        });

        var group = store.Groups.First(g => g.GroupId == groupId);
        Assert.Equal("NHOM-KIEM-THU", group.Code);
        Assert.Equal(MedDataStoreSeed.DeptNoiId, group.DepartmentId);
    }

    [Fact]
    public void AddGroup_InvalidDepartment_ThrowsFriendlyDomainError()
    {
        using var db = TestDbHelper.CreateSeededContext();
        var store = new MedDbDataStore(db);

        var ex = Assert.Throws<MedDomainException>(() =>
            store.AddGroup(new Group
            {
                Code = "GROUP-BAD-DEPT",
                Name = "Nhóm sai khoa",
                DepartmentId = Guid.NewGuid()
            }));

        Assert.Equal(547, ex.SqlErrorNumber);
        Assert.Contains("Khoa/phòng", ex.Message);
    }
}
