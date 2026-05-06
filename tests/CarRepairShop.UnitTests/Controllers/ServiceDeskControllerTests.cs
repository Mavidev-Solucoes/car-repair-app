using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CarRepairShop.API.Controllers;
using CarRepairShop.API.Services;
using CarRepairShop.API.ViewModels;
using CarRepairShop.Application.Common;
using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarRepairShop.UnitTests.Controllers;

public class ServiceDeskControllerTests
{
    private readonly Mock<IUiApiClient> _apiClientMock = new();
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly string[] BadRequestError = ["Bad request."];
    private static readonly string[] OutOfStockError = ["Out of stock."];
    private static readonly string[] JobNotFoundError = ["Job not found."];

    private ServiceDeskController CreateController(string role = "Admin", Guid? userId = null)
    {
        userId ??= Guid.NewGuid();
        var user = UiControllerTestHelper.CreateUser(role, userId);
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        return new ServiceDeskController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };
    }

    private static ServiceOrderDto MakeServiceOrder(
        ServiceStatus status = ServiceStatus.Received,
        Guid? customerId = null,
        Guid? vehicleId = null,
        Guid? assignedUserId = null) =>
        new(
            Guid.NewGuid(),
            vehicleId ?? Guid.NewGuid(),
            customerId ?? Guid.NewGuid(),
            assignedUserId ?? Guid.NewGuid(),
            status.ToString(),
            100m,
            DateTime.UtcNow,
            Array.Empty<ServiceOrderItemDto>(),
            Array.Empty<ServiceOrderJobDto>(),
            Array.Empty<ServiceStatusHistoryDto>());

    private static CustomerDto MakeCustomer(Guid? id = null) =>
        new(id ?? Guid.NewGuid(), "Alice", "12345678901", "alice@example.com", "11987654321", true, DateTime.UtcNow);

    private static VehicleDto MakeVehicle(Guid? id = null, Guid? customerId = null) =>
        new(id ?? Guid.NewGuid(), customerId ?? Guid.NewGuid(), "Ford", "Focus", 2022, "ABC1234", "Black", DateTime.UtcNow);

    private void SetupServicesPage(
        IEnumerable<ServiceOrderDto>? orders = null,
        IEnumerable<CustomerDto>? customers = null,
        IEnumerable<VehicleDto>? vehicles = null)
    {
        orders ??= Array.Empty<ServiceOrderDto>();
        customers ??= Array.Empty<CustomerDto>();
        vehicles ??= Array.Empty<VehicleDto>();

        _apiClientMock.Setup(c => c.GetAsync<List<ServiceOrderDto>>("/api/services", Ct))
            .ReturnsAsync(orders.ToList());
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.Is<string>(s => s.Contains("/api/customers")), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(customers.ToList(), customers.Count(), 1, 200));
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<VehicleDto>>(
                It.Is<string>(s => s.Contains("/api/vehicles")), Ct))
            .ReturnsAsync(new PagedResult<VehicleDto>(vehicles.ToList(), vehicles.Count(), 1, 200));
    }

    private void SetupServiceDetail(ServiceOrderDto order)
    {
        var customer = MakeCustomer(order.CustomerId);
        var vehicle = MakeVehicle(order.VehicleId);

        _apiClientMock.Setup(c => c.GetAsync<ServiceOrderDto>(
                It.Is<string>(s => s.Contains($"/api/services/{order.Id}")), Ct))
            .ReturnsAsync(order);
        _apiClientMock.Setup(c => c.GetAsync<CustomerDto>(
                It.Is<string>(s => s.Contains($"/api/customers/{order.CustomerId}")), Ct))
            .ReturnsAsync(customer);
        _apiClientMock.Setup(c => c.GetAsync<VehicleDto>(
                It.Is<string>(s => s.Contains($"/api/vehicles/{order.VehicleId}")), Ct))
            .ReturnsAsync(vehicle);
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<ServiceItemDto>>(
                It.Is<string>(s => s.Contains("/api/serviceitems")), Ct))
            .ReturnsAsync(new PagedResult<ServiceItemDto>(new List<ServiceItemDto>(), 0, 1, 200));
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<ServiceJobDto>>(
                It.Is<string>(s => s.Contains("/api/service-jobs")), Ct))
            .ReturnsAsync(new PagedResult<ServiceJobDto>(new List<ServiceJobDto>(), 0, 1, 200));
        _apiClientMock.Setup(c => c.GetAsync<List<ServiceStatusHistoryDto>>(
                It.Is<string>(s => s.Contains("/history")), Ct))
            .ReturnsAsync(new List<ServiceStatusHistoryDto>());
    }

    // ── Index / Results ──────────────────────────────────────────────────

    [Fact]
    public async Task Index_ReturnsView_WithServicesPage()
    {
        SetupServicesPage();
        var controller = CreateController();

        var result = await controller.Index(new ServicesPageViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ServicesPageViewModel>(view.Model);
    }

    [Fact]
    public async Task Index_WithSearch_FiltersServices()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var order = MakeServiceOrder(customerId: customerId, vehicleId: vehicleId);
        var customer = MakeCustomer(customerId);
        var vehicle = MakeVehicle(vehicleId);
        SetupServicesPage(new[] { order }, new[] { customer }, new[] { vehicle });
        var controller = CreateController();

        var result = await controller.Index(new ServicesPageViewModel { Search = "Alice" }, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServicesPageViewModel>(view.Model);
        Assert.Single(model.Services);
    }

    [Fact]
    public async Task Index_WithSearch_NonMatching_FiltersOutAll()
    {
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var order = MakeServiceOrder(customerId: customerId, vehicleId: vehicleId);
        var customer = MakeCustomer(customerId);
        var vehicle = MakeVehicle(vehicleId);
        SetupServicesPage(new[] { order }, new[] { customer }, new[] { vehicle });
        var controller = CreateController();

        var result = await controller.Index(new ServicesPageViewModel { Search = "xyz_nomatch" }, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServicesPageViewModel>(view.Model);
        Assert.Empty(model.Services);
    }

    [Fact]
    public async Task Index_WithStatus_FiltersServices()
    {
        var order = MakeServiceOrder(ServiceStatus.Received);
        SetupServicesPage(new[] { order });
        var controller = CreateController();

        var result = await controller.Index(
            new ServicesPageViewModel { Status = ServiceStatus.Diagnosing }, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServicesPageViewModel>(view.Model);
        Assert.Empty(model.Services); // Received != Diagnosing
    }

    [Fact]
    public async Task Index_CustomerRole_OnlyShowsOwnOrders()
    {
        var currentUserId = Guid.NewGuid();
        var ownOrder = MakeServiceOrder(customerId: currentUserId);
        var otherOrder = MakeServiceOrder();
        SetupServicesPage(new[] { ownOrder, otherOrder });

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.Role, "Customer"),
            new(JwtRegisteredClaimNames.Sub, currentUserId.ToString()),
            new(ClaimTypes.Name, "Alice")
        }, "TestAuthentication"));
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        var controller = new ServiceDeskController(_apiClientMock.Object)
        {
            ControllerContext = ctx,
            TempData = td
        };

        var result = await controller.Index(new ServicesPageViewModel(), Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServicesPageViewModel>(view.Model);
        Assert.Single(model.Services);
    }

    [Fact]
    public async Task Results_ReturnsPartialView()
    {
        SetupServicesPage();
        var controller = CreateController();

        var result = await controller.Results(new ServicesPageViewModel(), Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceResults", pv.ViewName);
    }

    // ── Create (GET) ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Get_NonAdmin_ReturnsForbid()
    {
        var controller = CreateController("Mechanic");

        var result = await controller.Create(Ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_Get_Admin_ReturnsView()
    {
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto>(), 0, 1, 200));
        var controller = CreateController("Admin");

        var result = await controller.Create(Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ServiceCreateViewModel>(view.Model);
    }

    // ── VehicleOptions ───────────────────────────────────────────────────

    [Fact]
    public async Task VehicleOptions_NoCustomer_ReturnsPartialWithEmptyList()
    {
        var controller = CreateController();

        var result = await controller.VehicleOptions(null, null, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_VehicleOptions", pv.ViewName);
        var model = Assert.IsAssignableFrom<IReadOnlyList<LookupOptionViewModel>>(pv.Model);
        Assert.Empty(model);
    }

    [Fact]
    public async Task VehicleOptions_WithCustomer_ReturnsVehicles()
    {
        var customerId = Guid.NewGuid();
        var vehicles = new List<VehicleDto> { MakeVehicle(customerId: customerId) };
        _apiClientMock.Setup(c => c.GetAsync<List<VehicleDto>>(
                It.Is<string>(s => s.Contains(customerId.ToString())), Ct))
            .ReturnsAsync(vehicles);
        var controller = CreateController();

        var result = await controller.VehicleOptions(customerId, null, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<List<LookupOptionViewModel>>(pv.Model);
        Assert.Single(model);
    }

    // ── Create (POST) ─────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Post_NonAdmin_ReturnsForbid()
    {
        var controller = CreateController("Mechanic");
        var model = new ServiceCreateViewModel { CustomerId = Guid.NewGuid(), VehicleId = Guid.NewGuid() };

        var result = await controller.Create(model, Ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_Post_InvalidModel_ReturnsView()
    {
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto>(), 0, 1, 200));
        var controller = CreateController("Admin");
        controller.ModelState.AddModelError("CustomerId", "Required");

        var model = new ServiceCreateViewModel();
        var result = await controller.Create(model, Ct);

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_Success_RedirectsToDetail()
    {
        var orderId = Guid.NewGuid();
        var order = MakeServiceOrder();
        _apiClientMock.Setup(c => c.PostAsync<object, ServiceOrderDto>(
                "/api/services", It.IsAny<object>(), Ct))
            .ReturnsAsync(order);
        var controller = CreateController("Admin");

        var model = new ServiceCreateViewModel { CustomerId = Guid.NewGuid(), VehicleId = Guid.NewGuid() };
        var result = await controller.Create(model, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(order.Id.ToString(), redirect.Url);
    }

    [Fact]
    public async Task Create_Post_ApiException_ReturnsView()
    {
        _apiClientMock.Setup(c => c.PostAsync<object, ServiceOrderDto>(
                "/api/services", It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 400,
                new Dictionary<string, string[]> { { "", BadRequestError } }));
        _apiClientMock.Setup(c => c.GetAsync<PagedResult<CustomerDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new PagedResult<CustomerDto>(new List<CustomerDto>(), 0, 1, 200));
        // setup for GetVehicleOptionsAsync called when model.CustomerId is set
        _apiClientMock.Setup(c => c.GetAsync<List<VehicleDto>>(
                It.IsAny<string>(), Ct))
            .ReturnsAsync(new List<VehicleDto>());
        var controller = CreateController("Admin");

        var model = new ServiceCreateViewModel { CustomerId = Guid.NewGuid(), VehicleId = Guid.NewGuid() };
        var result = await controller.Create(model, Ct);

        Assert.IsType<ViewResult>(result);
    }

    // ── Details ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Details_ReturnsView_WithServiceDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        var controller = CreateController();

        var result = await controller.Details(order.Id, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceDetailViewModel>(view.Model);
        Assert.Equal(order.Id, model.Id);
    }

    // ── AddItem / DeleteItem ─────────────────────────────────────────────

    [Fact]
    public async Task AddItem_NonAdmin_ReturnsForbid()
    {
        var controller = CreateController("Mechanic");
        var form = new AddServiceItemFormViewModel { CatalogItemId = Guid.NewGuid(), Quantity = 1 };

        var result = await controller.AddItem(Guid.NewGuid(), form, Ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AddItem_InvalidModel_ReturnsDetailView()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        var controller = CreateController("Admin");
        controller.ModelState.AddModelError("CatalogItemId", "Required");

        var form = new AddServiceItemFormViewModel();
        var result = await controller.AddItem(order.Id, form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Details", view.ViewName);
    }

    [Fact]
    public async Task AddItem_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PostAsync(
                It.IsAny<string>(), It.IsAny<object>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Admin");

        var form = new AddServiceItemFormViewModel { CatalogItemId = Guid.NewGuid(), Quantity = 2 };
        var result = await controller.AddItem(id, form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
    }

    [Fact]
    public async Task AddItem_ApiException_ReturnsDetailView()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PostAsync(
                It.Is<string>(s => s.Contains("items")), It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Out of stock", 400,
                new Dictionary<string, string[]> { { "", OutOfStockError } }));
        var controller = CreateController("Admin");

        var form = new AddServiceItemFormViewModel { CatalogItemId = Guid.NewGuid(), Quantity = 1 };
        var result = await controller.AddItem(order.Id, form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Details", view.ViewName);
    }

    [Fact]
    public async Task DeleteItem_NonAdmin_ReturnsForbid()
    {
        var controller = CreateController("Mechanic");

        var result = await controller.DeleteItem(Guid.NewGuid(), Guid.NewGuid(), Ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteItem_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Admin");

        var result = await controller.DeleteItem(id, Guid.NewGuid(), Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
    }

    [Fact]
    public async Task DeleteItem_ApiException_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController("Admin");

        var result = await controller.DeleteItem(id, Guid.NewGuid(), Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
    }

    // ── AddJob / DeleteJob ────────────────────────────────────────────────

    [Fact]
    public async Task AddJob_NonAdmin_ReturnsForbid()
    {
        var controller = CreateController("Mechanic");
        var form = new AddServiceJobFormViewModel { CatalogJobId = Guid.NewGuid() };

        var result = await controller.AddJob(Guid.NewGuid(), form, Ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AddJob_InvalidModel_ReturnsDetailView()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        var controller = CreateController("Admin");
        controller.ModelState.AddModelError("CatalogJobId", "Required");

        var form = new AddServiceJobFormViewModel();
        var result = await controller.AddJob(order.Id, form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Details", view.ViewName);
    }

    [Fact]
    public async Task AddJob_Success_Redirects()
    {
        var id = Guid.NewGuid();
        _apiClientMock.Setup(c => c.PostAsync(
                It.Is<string>(s => s.Contains("jobs")), It.IsAny<object>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Admin");

        var form = new AddServiceJobFormViewModel { CatalogJobId = Guid.NewGuid() };
        var result = await controller.AddJob(id, form, Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(id.ToString(), redirect.Url);
    }

    [Fact]
    public async Task AddJob_ApiException_ReturnsDetailView()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PostAsync(
                It.Is<string>(s => s.Contains("jobs")), It.IsAny<object>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 400,
                new Dictionary<string, string[]> { { "JobForm", JobNotFoundError } }));
        var controller = CreateController("Admin");

        var form = new AddServiceJobFormViewModel { CatalogJobId = Guid.NewGuid() };
        var result = await controller.AddJob(order.Id, form, Ct);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Details", view.ViewName);
    }

    [Fact]
    public async Task DeleteJob_NonAdmin_ReturnsForbid()
    {
        var controller = CreateController("Mechanic");

        var result = await controller.DeleteJob(Guid.NewGuid(), Guid.NewGuid(), Ct);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteJob_Success_Redirects()
    {
        var serviceId = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Admin");

        var result = await controller.DeleteJob(serviceId, Guid.NewGuid(), Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(serviceId.ToString(), redirect.Url);
    }

    [Fact]
    public async Task DeleteJob_ApiException_Redirects()
    {
        var serviceId = Guid.NewGuid();
        _apiClientMock.Setup(c => c.DeleteAsync(It.IsAny<string>(), Ct))
            .ThrowsAsync(new UiApiException("Error", 500));
        var controller = CreateController("Admin");

        var result = await controller.DeleteJob(serviceId, Guid.NewGuid(), Ct);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains(serviceId.ToString(), redirect.Url);
    }

    // ── Status transitions ────────────────────────────────────────────────

    [Fact]
    public async Task RequestApproval_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("request-approval")), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Admin");

        var result = await controller.RequestApproval(order.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
        var model = Assert.IsType<ServiceDetailViewModel>(pv.Model);
        Assert.NotNull(model.Flash);
    }

    [Fact]
    public async Task RequestApproval_ApiException_ReturnsPartialWithError()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("request-approval")), Ct))
            .ThrowsAsync(new UiApiException("Cannot request", 400));
        var controller = CreateController("Admin");

        var result = await controller.RequestApproval(order.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        var model = Assert.IsType<ServiceDetailViewModel>(pv.Model);
        Assert.NotNull(model.Flash);
        Assert.True(model.Flash!.IsError);
    }

    [Fact]
    public async Task Approve_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.WaitingForApproval);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.GetAsync<ServiceOrderDto>(
                It.Is<string>(s => s.Contains("approve")), Ct))
            .ReturnsAsync(order);
        var controller = CreateController("Customer");

        var result = await controller.Approve(order.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
    }

    [Fact]
    public async Task Deliver_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Finished);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("deliver")), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Admin");

        var result = await controller.Deliver(order.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
    }

    [Fact]
    public async Task Dispute_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Finished);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("dispute")), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Customer");

        var result = await controller.Dispute(order.Id, Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
    }

    // ── Job transitions ────────────────────────────────────────────────────

    [Fact]
    public async Task AcknowledgeJob_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Diagnosing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("acknowledge")), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Mechanic");

        var result = await controller.AcknowledgeJob(order.Id, Guid.NewGuid(), Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
    }

    [Fact]
    public async Task StartJobProgress_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Executing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("start-progress")), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Mechanic");

        var result = await controller.StartJobProgress(order.Id, Guid.NewGuid(), Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
    }

    [Fact]
    public async Task CompleteJob_Success_ReturnsPartialWithDetail()
    {
        var order = MakeServiceOrder(ServiceStatus.Executing);
        SetupServiceDetail(order);
        _apiClientMock.Setup(c => c.PatchAsync(
                It.Is<string>(s => s.Contains("complete")), Ct))
            .Returns(Task.CompletedTask);
        var controller = CreateController("Mechanic");

        var result = await controller.CompleteJob(order.Id, Guid.NewGuid(), Ct);

        var pv = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_ServiceDetailContent", pv.ViewName);
    }

    [Fact]
    public async Task Details_WithFlashFromTempData_PopulatesFlash()
    {
        var order = MakeServiceOrder(ServiceStatus.Received);
        SetupServiceDetail(order);
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        td["FlashMessage"] = "Done!";
        td["FlashIsError"] = false;
        var controller = new ServiceDeskController(_apiClientMock.Object) { ControllerContext = ctx, TempData = td };

        var result = await controller.Details(order.Id, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceDetailViewModel>(view.Model);
        Assert.NotNull(model.Flash);
        Assert.Equal("Done!", model.Flash!.Message);
    }

    [Fact]
    public async Task Details_WithErrorFlashFromTempData_PopulatesErrorFlash()
    {
        var order = MakeServiceOrder(ServiceStatus.Received);
        SetupServiceDetail(order);
        var user = UiControllerTestHelper.CreateUser("Admin");
        var (ctx, td) = UiControllerTestHelper.CreateControllerContext(user);
        td["FlashMessage"] = "Error!";
        td["FlashIsError"] = true;
        var controller = new ServiceDeskController(_apiClientMock.Object) { ControllerContext = ctx, TempData = td };

        var result = await controller.Details(order.Id, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceDetailViewModel>(view.Model);
        Assert.NotNull(model.Flash);
        Assert.True(model.Flash!.IsError);
    }

    [Fact]
    public async Task Details_WithServiceJobsAndHistory_BuildsViewModel()
    {
        var assignedUserId = Guid.NewGuid();
        var catalogJobId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();
        var jobs = new[]
        {
            new ServiceOrderJobDto(
                jobId, serviceOrderId, catalogJobId, "Fix brake",
                "Desc", 80m, "Open", assignedUserId, DateTime.UtcNow)
        };
        var items = new[]
        {
            new ServiceOrderItemDto(Guid.NewGuid(), serviceOrderId, Guid.NewGuid(), "Oil", 15m, 2)
        };
        var historyEntry = new ServiceStatusHistoryDto(Guid.NewGuid(), serviceOrderId, null, "Received",
            DateTime.UtcNow, assignedUserId);
        var order = new ServiceOrderDto(
            serviceOrderId, Guid.NewGuid(), Guid.NewGuid(), assignedUserId,
            "Diagnosing", 110m, DateTime.UtcNow, items, jobs, Array.Empty<ServiceStatusHistoryDto>());

        SetupServiceDetail(order);

        // Override history mock to return actual history data
        _apiClientMock.Setup(c => c.GetAsync<List<ServiceStatusHistoryDto>>(
                It.Is<string>(s => s.Contains("/history")), Ct))
            .ReturnsAsync(new List<ServiceStatusHistoryDto> { historyEntry });

        var controller = CreateController("Admin", assignedUserId);

        var result = await controller.Details(serviceOrderId, Ct);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ServiceDetailViewModel>(view.Model);
        Assert.Single(model.Jobs);
        Assert.Single(model.Items);
        Assert.Single(model.History);
    }
}
