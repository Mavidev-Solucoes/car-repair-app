using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class ServiceJobsUiControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    private ServiceJobsUiController CreateController(string role = "Admin", bool htmxRequest = false)
    {
        var user = UiControllerTestHelper.CreateUser(role);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user, htmxRequest: htmxRequest);
        return new ServiceJobsUiController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
    }

    private static ServiceJobDto MakeJob(string name = "Oil Change") =>
        new(Guid.NewGuid(), name, "Desc", 50m, DateTime.UtcNow);

    private void SetupPagedJobs(IEnumerable<ServiceJobDto>? jobs = null)
    {
        jobs ??= Array.Empty<ServiceJobDto>();
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<ServiceJobDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<ServiceJobDto>(jobs.ToList(), jobs.Count(), 1, 100));
    }

    [Fact]
    public async Task Index_ReturnsView_WithJobs()
    {
        SetupPagedJobs(new[] { MakeJob() });
        var controller = CreateController();

        var result = await controller.Index(new CatalogListFiltersViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobsPageViewModel>(view.Model);
        Assert.Single(model.Jobs);
    }

    [Fact]
    public async Task Details_ReturnsPartialView()
    {
        var job = MakeJob();
        _apiClientMock.Setup(c => c.GetAsync<ServiceJobDto>(
                It.Is<string>(s => s.Contains(job.Id.ToString())), Ct))
            .ReturnsAsync(job);
        var controller = CreateController();

        var result = await controller.Details(job.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceJobDetailsModalContent", pv.ViewName);
        var model = Assert.IsType<ServiceJobDetailsModalViewModel>(pv.Model);
        Assert.Equal(job.Id, model.Form.Id);
    }

    [Fact]
    public async Task Save_InvalidModel_ReturnsIndex()
    {
        SetupPagedJobs();
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var form = new ServiceJobFormViewModel { Name = "" };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
        Assert.Equal(Guid.Empty, form.Id);
    }

    [Fact]
    public async Task Save_Success_Redirects()
    {
        var job = MakeJob();
        _apiClientMock.Setup(c => c.PostAsync<object, ServiceJobDto>(
                "/api/service-jobs", It.IsAny<object>(), Ct))
            .ReturnsAsync(job);
        var controller = CreateController();

        var form = new ServiceJobFormViewModel { Name = "Oil Change", Description = "Desc", Price = 50m };
        var result = await controller.Save(form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/service-jobs", redirect.Url);
    }

    [Fact]
    public async Task Save_ApiException_WithErrors_ReturnsIndex()
    {
        SetupPagedJobs();
        _apiClientMock.Setup(c => c.PostAsync<object, ServiceJobDto>(
                "/api/service-jobs", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Duplicate", 409,
                new Dictionary<string, string[]> { { "Name", new[] { "Already exists." } } }));
        var controller = CreateController();

        var form = new ServiceJobFormViewModel { Name = "Existing", Description = "Desc", Price = 50m };
        var result = await controller.Save(form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Index", view.ViewName);
    }

    [Fact]
    public async Task Save_ApiException_NoErrors_ReturnsIndex()
    {
        SetupPagedJobs();
        _apiClientMock.Setup(c => c.PostAsync<object, ServiceJobDto>(
                "/api/service-jobs", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Server error", 500));
        var controller = CreateController();

        var form = new ServiceJobFormViewModel { Name = "Oil Change", Description = "Desc", Price = 50m };
        var result = await controller.Save(form, Ct);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Update_InvalidModel_ReturnsPartial()
    {
        var id = Guid.NewGuid();
        var controller = CreateController();
        controller.ModelState.AddModelError("Name", "Required");

        var form = new ServiceJobFormViewModel { Name = "" };
        var result = await controller.Update(id, form, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceJobDetailsModalContent", pv.ViewName);
        Assert.Equal(id, form.Id);
    }

    [Fact]
    public async Task Update_Success_NonHtmx_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: false);

        var form = new ServiceJobFormViewModel { Name = "Oil Change", Description = "Desc", Price = 50m };
        var result = await controller.Update(id, form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/service-jobs", redirect.Url);
    }

    [Fact]
    public async Task Update_Success_HtmxRequest_ReturnsEmpty()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ReturnsAsync(new object());
        var controller = CreateController(htmxRequest: true);

        var form = new ServiceJobFormViewModel { Name = "Oil Change", Description = "Desc", Price = 50m };
        var result = await controller.Update(id, form, Ct);

        Assert.IsType<EmptyResult>(result);
    }

    [Fact]
    public async Task Update_ApiException_ReturnsPartial()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PutAsync<object, object>(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController();

        var form = new ServiceJobFormViewModel { Name = "Oil Change", Description = "Desc", Price = 50m };
        var result = await controller.Update(id, form, Ct);

        Assert.IsType<PartialViewResult>(result);
    }

    [Fact]
    public async Task Delete_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController();

        var result = await controller.Delete(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/service-jobs", redirect.Url);
    }

    [Fact]
    public async Task Delete_ApiException_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController();

        var result = await controller.Delete(id, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/ui/service-jobs", redirect.Url);
    }

    [Fact]
    public async Task Index_FlashFromTempData_IsPopulated()
    {
        SetupPagedJobs();
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        td["FlashMessage"] = "Job created.";
        td["FlashIsError"] = false;
        var controller = new ServiceJobsUiController(_apiClientMock.Object) { ControllerContext = ctx, TempData = td };

        var result = await controller.Index(new CatalogListFiltersViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobsPageViewModel>(view.Model);
        Assert.NotNull(model.Flash);
        Assert.Equal("Job created.", model.Flash!.Message);
    }

    [Fact]
    public async Task Index_ErrorFlashFromTempData_IsError()
    {
        SetupPagedJobs();
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        td["FlashMessage"] = "Error occurred.";
        td["FlashIsError"] = true;
        var controller = new ServiceJobsUiController(_apiClientMock.Object) { ControllerContext = ctx, TempData = td };

        var result = await controller.Index(new CatalogListFiltersViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceJobsPageViewModel>(view.Model);
        Assert.True(model.Flash!.IsError);
    }
}
