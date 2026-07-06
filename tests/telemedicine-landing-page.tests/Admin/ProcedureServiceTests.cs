using TelemedicineLandingPage.Models.Admin;
using TelemedicineLandingPage.Services.Admin;

namespace TelemedicineLandingPage.Tests.Admin;

public sealed class ProcedureServiceTests
{
    [Fact]
    public void SubmitAndApprove_TransitionsStatusToDaBanHanh()
    {
        var service = new ProcedureService();

        var draft = service.Create(new ProcedureRecord
        {
            Code = "QT-TEST-001",
            Name = "Quy trình kiểm thử",
            Department = Department.NoiTiet,
            Version = "0.1",
            Status = ProcedureStatus.DangSoanThao,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
            UpdatedBy = "ThS. Tester",
            Steps = new List<ProcedureStep>
            {
                new(1, "Khởi tạo", "Bác sĩ", 5, "Hoàn tất biểu mẫu"),
            },
        });

        service.SubmitForApproval(draft.Id, "BS. Nguyễn Văn A");
        Assert.Equal(ProcedureStatus.DangChoPheDuyet, service.GetById(draft.Id)!.Status);

        var raised = false;
        service.StateChanged += () => raised = true;
        service.Approve(draft.Id, "BS. Đặng Thái Sơn");

        var approved = service.GetById(draft.Id);
        Assert.NotNull(approved);
        Assert.Equal(ProcedureStatus.DaBanHanh, approved!.Status);
        Assert.True(raised);
        Assert.Null(approved.RejectionReason);
    }

    [Fact]
    public void Search_FiltersByStatusAndDepartment()
    {
        var service = new ProcedureService();

        var pendingNoiTiet = service.Search(new ProcedureFilter(
            Status: ProcedureStatus.DangChoPheDuyet,
            Department: Department.NoiTiet));
        Assert.All(pendingNoiTiet, p => Assert.Equal(ProcedureStatus.DangChoPheDuyet, p.Status));
        Assert.All(pendingNoiTiet, p => Assert.Equal(Department.NoiTiet, p.Department));

        var releasedXetNghiem = service.Search(new ProcedureFilter(
            Status: ProcedureStatus.DaBanHanh,
            Department: Department.XetNghiem));
        Assert.NotEmpty(releasedXetNghiem);
        Assert.All(releasedXetNghiem, p => Assert.Equal(ProcedureStatus.DaBanHanh, p.Status));
        Assert.All(releasedXetNghiem, p => Assert.Equal(Department.XetNghiem, p.Department));

        var byNameNeedle = service.Search(new ProcedureFilter(Search: "vaccine"));
        Assert.NotEmpty(byNameNeedle);
        Assert.All(byNameNeedle, p => Assert.Contains("vaccine", p.Name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Reject_StoresReasonAndReturnsToDraft()
    {
        var service = new ProcedureService();
        var pending = service.Search(new ProcedureFilter(Status: ProcedureStatus.DangChoPheDuyet)).First();

        service.Reject(pending.Id, "BS. Đặng Thái Sơn", "Cần bổ sung bước kiểm tra dị ứng");

        var rejected = service.GetById(pending.Id);
        Assert.NotNull(rejected);
        Assert.Equal(ProcedureStatus.DangSoanThao, rejected!.Status);
        Assert.Equal("Cần bổ sung bước kiểm tra dị ứng", rejected.RejectionReason);
    }
}
