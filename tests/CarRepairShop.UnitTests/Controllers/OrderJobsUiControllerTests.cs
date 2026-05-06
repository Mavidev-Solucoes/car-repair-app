using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class OrderJobsUiControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private OrderJobsUiController CreateController(string role = "Mechanic")
    {
        var userId = Guid.NewGuid();
        var user = UiControllerTestHelper.CreateUser(role, userId);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        return new OrderJobsUiController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
    }

    private static ServiceOrderJobDto MakeJob(JobStatus status = JobStatus.Open) =>
        new(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Fix engine", "Description", 100m,
            status.ToString(), null, DateTime.UtcNow);

    private static ServiceOrderJobStatusHistoryDto MakeHistory() =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, "Open", DateTime.UtcNow, null, null);

    [Fact]
    public async Task Index_ReturnsView_WithJobs()
    {
        var jobs = new[] { MakeJob() };
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<ServiceOrderJobDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<ServiceOrderJobDto>(jobs.ToList(), 1, 1, 100));
        var controller = CreateController();

        var result = await controller.Index(null, null, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobsPageViewModel>(view.Model);
        Assert.Single(model.Jobs);
    }

    [Fact]
    public async Task Index_WithSearchAndStatus_BuildsQueryCorrectly()
    {
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<ServiceOrderJobDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<ServiceOrderJobDto>(new List<ServiceOrderJobDto>(), 0, 1, 100));
        var controller = CreateController();

        await controller.Index("engine", JobStatus.Open, Ct);

        _apiClientMock.Verify(c => c.GetAsync<PagedResult<ServiceOrderJobDto>>(
            It.Is<string>(s => s.Contains("name=engine") && s.Contains("status=Open")), Ct),
            Times.Once);
    }

    [Fact]
    public async Task Details_ReturnsView_WithJobDetail()
    {
        var job = MakeJob(JobStatus.Open);
        var history = new List<ServiceOrderJobStatusHistoryDto> { MakeHistory() };

        _apiClientMock.Setup(c => c.GetAsync<ServiceOrderJobDto>(
                It.Is<string>(s => s.Contains(job.Id.ToString())), Ct))
            .ReturnsAsync(job);
        _apiClientMock.Setup(c => c.GetAsync<List<ServiceOrderJobStatusHistoryDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(history);
        var controller = CreateController();

        var result = await controller.Details(job.Id, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobDetailViewModel>(view.Model);
        Assert.Equal(job.Id, model.Id);
        Assert.True(model.CanAcknowledge); // Open status
        Assert.False(model.CanStartProgress);
        Assert.False(model.CanComplete);
    }

    [Fact]
    public async Task Details_AcknowledgedJob_CanStartProgress()
    {
        var job = MakeJob(JobStatus.Acknowledged);
        _apiClientMock.Setup(c => c.GetAsync<ServiceOrderJobDto>(
                It.Is<string>(s => s.Contains(job.Id.ToString())), Ct))
            .ReturnsAsync(job);
        _apiClientMock.Setup(c => c.GetAsync<List<ServiceOrderJobStatusHistoryDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new List<ServiceOrderJobStatusHistoryDto>());
        var controller = CreateController();

        var result = await controller.Details(job.Id, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobDetailViewModel>(view.Model);
        Assert.False(model.CanAcknowledge);
        Assert.True(model.CanStartProgress);
    }

    [Fact]
    public async Task Details_InProgressJob_CanComplete()
    {
        var job = MakeJob(JobStatus.InProgress);
        _apiClientMock.Setup(c => c.GetAsync<ServiceOrderJobDto>(
                It.Is<string>(s => s.Contains(job.Id.ToString())), Ct))
            .ReturnsAsync(job);
        _apiClientMock.Setup(c => c.GetAsync<List<ServiceOrderJobStatusHistoryDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new List<ServiceOrderJobStatusHistoryDto>());
        var controller = CreateController();

        var result = await controller.Details(job.Id, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobDetailViewModel>(view.Model);
        Assert.True(model.CanComplete);
    }

    [Fact]
    public async Task Acknowledge_PatchesAndRedirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PatchAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.Acknowledge(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
        _apiClientMock.Verify(c => c.PatchAsync(
            It.Is<string>(s => s.Contains("acknowledge")), Ct), Times.Once);
    }

    [Fact]
    public async Task StartProgress_PatchesAndRedirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PatchAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.StartProgress(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
    }

    [Fact]
    public async Task Complete_PatchesAndRedirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PatchAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.Complete(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
    }
}
