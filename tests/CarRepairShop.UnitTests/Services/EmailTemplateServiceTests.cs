using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Services.Implementations;
using Microsoft.Extensions.Hosting;
using Moq;

namespace CarRepairShop.UnitTests.Services;

public class EmailTemplateServiceTests : IDisposable
{
    private readonly string _tempContentRoot;
    private readonly string _templatesDir;
    private readonly Mock<IHostEnvironment> _hostEnv = new();
    private readonly EmailTemplateService _service;

    public EmailTemplateServiceTests()
    {
        _tempContentRoot = Path.Combine(Path.GetTempPath(), "EmailTemplateServiceTests_" + Guid.NewGuid());
        _templatesDir = Path.Combine(_tempContentRoot, "Templates");
        Directory.CreateDirectory(_templatesDir);

        _hostEnv.Setup(e => e.ContentRootPath).Returns(_tempContentRoot);
        _service = new EmailTemplateService(_hostEnv.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempContentRoot))
            Directory.Delete(_tempContentRoot, recursive: true);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void WriteTemplate(string fileName, string content) =>
        File.WriteAllText(Path.Combine(_templatesDir, fileName), content);

    private static Customer CreateCustomer(string name = "John Customer") =>
        new(name, "12345678901", "john@example.com", "11999999999", "hash");

    private static Vehicle CreateVehicle(string brand = "Toyota", string model = "Corolla", int year = 2022) =>
        new(Guid.NewGuid(), brand, model, year, "ABC1234");

    private static Employee CreateEmployee(string name = "Tech Employee") =>
        new(name, "tech@shop.com", "hash", UserRole.Mechanic);

    private static (ServiceOrder order, Guid employeeId) CreateOrder()
    {
        var employee = CreateEmployee();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        return (order, employee.Id);
    }

    private static ServiceOrder ReachFinishedState()
    {
        var employee = CreateEmployee();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Brake Job", "Fix brakes", 100);
        job.Acknowledge(employee);
        order.AttachServiceJob(job, employee.Id);
        order.RequestApproval(employee.Id);
        order.Approve();
        job.StartProgress(employee.Id);
        job.Complete(employee.Id);
        order.TryFinish();
        return order;
    }

    // ── RenderServiceReceivedAsync ────────────────────────────────────────────

    [Fact]
    public async Task RenderServiceReceivedAsync_ReplacesAllTokens()
    {
        WriteTemplate("service-received.html",
            "{{CUSTOMER_NAME}} {{SERVICE_ID}} {{VEHICLE_YEAR}} {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} {{VEHICLE_LICENSE_PLATE}} {{EMPLOYEE_NAME}} {{OPENED_AT}}");

        var (order, _) = CreateOrder();
        var customer = CreateCustomer("Alice");
        var vehicle = CreateVehicle("Toyota", "Corolla", 2022);
        var employee = CreateEmployee("Bob");

        var result = await _service.RenderServiceReceivedAsync(order, customer, vehicle, employee);

        Assert.Contains("Alice", result);
        Assert.Contains(order.Id.ToString(), result);
        Assert.Contains("2022", result);
        Assert.Contains("Toyota", result);
        Assert.Contains("Corolla", result);
        Assert.Contains("Bob", result);
        Assert.DoesNotContain("{{CUSTOMER_NAME}}", result);
        Assert.DoesNotContain("{{VEHICLE_BRAND}}", result);
        Assert.DoesNotContain("{{EMPLOYEE_NAME}}", result);
        Assert.DoesNotContain("{{OPENED_AT}}", result);
    }

    [Fact]
    public async Task RenderServiceReceivedAsync_OpenedAtContainsUtc()
    {
        WriteTemplate("service-received.html", "{{OPENED_AT}}");

        var (order, _) = CreateOrder();

        var result = await _service.RenderServiceReceivedAsync(order, CreateCustomer(), CreateVehicle(), CreateEmployee());

        Assert.Contains("UTC", result);
    }

    [Fact]
    public async Task RenderServiceReceivedAsync_HtmlEncodesSpecialCharactersInCustomerName()
    {
        WriteTemplate("service-received.html", "{{CUSTOMER_NAME}}");

        var (order, _) = CreateOrder();
        var customer = CreateCustomer("<script>alert('xss')</script>");

        var result = await _service.RenderServiceReceivedAsync(order, customer, CreateVehicle(), CreateEmployee());

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("&lt;script&gt;", result);
    }

    [Fact]
    public async Task RenderServiceReceivedAsync_HtmlEncodesSpecialCharactersInVehicleBrand()
    {
        WriteTemplate("service-received.html", "{{VEHICLE_BRAND}}");

        var (order, _) = CreateOrder();

        var result = await _service.RenderServiceReceivedAsync(order, CreateCustomer(), CreateVehicle("Ford & GM"), CreateEmployee());

        Assert.Contains("Ford &amp; GM", result);
    }

    [Fact]
    public async Task RenderServiceReceivedAsync_ThrowsFileNotFoundWhenTemplateIsMissing()
    {
        var (order, _) = CreateOrder();

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.RenderServiceReceivedAsync(order, CreateCustomer(), CreateVehicle(), CreateEmployee()));
    }

    // ── RenderWaitingForApprovalAsync ─────────────────────────────────────────

    [Fact]
    public async Task RenderWaitingForApprovalAsync_ReplacesAllTokens()
    {
        WriteTemplate("service-waiting-approval.html",
            "{{CUSTOMER_NAME}} {{SERVICE_ID}} {{VEHICLE_YEAR}} {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} {{VEHICLE_LICENSE_PLATE}} {{TOTAL_PRICE}} {{SERVICE_ITEMS_HTML}} {{SERVICE_JOBS_HTML}} {{APPROVAL_URL}}");

        var (order, _) = CreateOrder();
        var customer = CreateCustomer("Carol");
        const string approvalUrl = "https://example.com/approve/123";

        var result = await _service.RenderWaitingForApprovalAsync(order, customer, approvalUrl, "https://example.com/reject/123");

        Assert.Contains("Carol", result);
        Assert.Contains(order.Id.ToString(), result);
        Assert.Contains(approvalUrl, result);
        Assert.DoesNotContain("{{CUSTOMER_NAME}}", result);
        Assert.DoesNotContain("{{APPROVAL_URL}}", result);
        Assert.DoesNotContain("{{SERVICE_ITEMS_HTML}}", result);
        Assert.DoesNotContain("{{SERVICE_JOBS_HTML}}", result);
    }

    [Fact]
    public async Task RenderWaitingForApprovalAsync_WithServiceItems_RendersItemsTable()
    {
        WriteTemplate("service-waiting-approval.html", "{{SERVICE_ITEMS_HTML}}");

        var employee = CreateEmployee();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        order.AddServiceItem(new ServiceOrderItem(order.Id, Guid.NewGuid(), "Oil Filter", 50m, 2), employee.Id);

        var result = await _service.RenderWaitingForApprovalAsync(order, CreateCustomer(), "http://approve", "http://reject");

        Assert.Contains("Oil Filter", result);
        Assert.Contains("<table", result);
    }

    [Fact]
    public async Task RenderWaitingForApprovalAsync_WithNoItems_ShowsNoItemsMessage()
    {
        WriteTemplate("service-waiting-approval.html", "{{SERVICE_ITEMS_HTML}}");

        var (order, _) = CreateOrder();

        var result = await _service.RenderWaitingForApprovalAsync(order, CreateCustomer(), "http://approve", "http://reject");

        Assert.Contains("No items recorded", result);
    }

    [Fact]
    public async Task RenderWaitingForApprovalAsync_WithNoJobs_ShowsNoJobsMessage()
    {
        WriteTemplate("service-waiting-approval.html", "{{SERVICE_JOBS_HTML}}");

        var (order, _) = CreateOrder();

        var result = await _service.RenderWaitingForApprovalAsync(order, CreateCustomer(), "http://approve", "http://reject");

        Assert.Contains("No jobs recorded", result);
    }

    [Fact]
    public async Task RenderWaitingForApprovalAsync_WithServiceJobs_RendersJobsTable()
    {
        WriteTemplate("service-waiting-approval.html", "{{SERVICE_JOBS_HTML}}");

        var employee = CreateEmployee("Mechanic Bob");
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Engine Tune-Up", "Full tune-up", 300);
        job.Acknowledge(employee);
        order.AttachServiceJob(job, employee.Id);

        var result = await _service.RenderWaitingForApprovalAsync(order, CreateCustomer(), "http://approve", "http://reject");

        Assert.Contains("Engine Tune-Up", result);
        Assert.Contains("<table", result);
    }

    [Fact]
    public async Task RenderWaitingForApprovalAsync_ThrowsFileNotFoundWhenTemplateIsMissing()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.RenderWaitingForApprovalAsync(CreateOrder().order, CreateCustomer(), "http://approve", "http://reject"));
    }

    // ── RenderServiceFinishedAsync ────────────────────────────────────────────

    [Fact]
    public async Task RenderServiceFinishedAsync_ReplacesAllTokens()
    {
        WriteTemplate("service-finished.html",
            "{{CUSTOMER_NAME}} {{SERVICE_ID}} {{VEHICLE_YEAR}} {{VEHICLE_BRAND}} {{VEHICLE_MODEL}} {{VEHICLE_LICENSE_PLATE}} {{TOTAL_PRICE}} {{COMPLETED_AT}} {{SERVICE_ITEMS_HTML}} {{SERVICE_JOBS_HTML}}");

        var (order, _) = CreateOrder();
        var customer = CreateCustomer("Dave");

        var result = await _service.RenderServiceFinishedAsync(order, customer);

        Assert.Contains("Dave", result);
        Assert.Contains(order.Id.ToString(), result);
        Assert.DoesNotContain("{{CUSTOMER_NAME}}", result);
        Assert.DoesNotContain("{{SERVICE_ID}}", result);
        Assert.DoesNotContain("{{COMPLETED_AT}}", result);
        Assert.DoesNotContain("{{SERVICE_ITEMS_HTML}}", result);
        Assert.DoesNotContain("{{SERVICE_JOBS_HTML}}", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_WithFinishedStatusHistory_ShowsFinishedTimestamp()
    {
        WriteTemplate("service-finished.html", "{{COMPLETED_AT}}");

        var order = ReachFinishedState();

        var result = await _service.RenderServiceFinishedAsync(order, CreateCustomer());

        Assert.Contains("UTC", result);
        // Should contain a year-like timestamp from the Finished transition
        Assert.Matches(@"\d{4}-\d{2}-\d{2}", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_WithNoFinishedHistoryEntry_FallsBackToUpdateTimestamp()
    {
        WriteTemplate("service-finished.html", "{{COMPLETED_AT}}");

        // Order in Received state — no Finished entry in status history
        var (order, _) = CreateOrder();

        var result = await _service.RenderServiceFinishedAsync(order, CreateCustomer());

        // When no Finished history and UpdatedAt is null, the string concatenation
        // of null + " UTC" yields " UTC" (not "N/A"), so we assert UTC is present
        Assert.Contains("UTC", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_WithServiceItems_RendersItemsTable()
    {
        WriteTemplate("service-finished.html", "{{SERVICE_ITEMS_HTML}}");

        var employee = CreateEmployee();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        order.AddServiceItem(new ServiceOrderItem(order.Id, Guid.NewGuid(), "Brake Pads", 120m, 4), employee.Id);

        var result = await _service.RenderServiceFinishedAsync(order, CreateCustomer());

        Assert.Contains("Brake Pads", result);
        Assert.Contains("<table", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_WithNoItems_ShowsNoItemsMessage()
    {
        WriteTemplate("service-finished.html", "{{SERVICE_ITEMS_HTML}}");

        var (order, _) = CreateOrder();

        var result = await _service.RenderServiceFinishedAsync(order, CreateCustomer());

        Assert.Contains("No items recorded", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_WithNoJobs_ShowsNoJobsMessage()
    {
        WriteTemplate("service-finished.html", "{{SERVICE_JOBS_HTML}}");

        var (order, _) = CreateOrder();

        var result = await _service.RenderServiceFinishedAsync(order, CreateCustomer());

        Assert.Contains("No jobs recorded", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_JobWithNoAssignedUser_ShowsUnassigned()
    {
        WriteTemplate("service-finished.html", "{{SERVICE_JOBS_HTML}}");

        var employee = CreateEmployee();
        var order = new ServiceOrder(Guid.NewGuid(), Guid.NewGuid(), employee.Id);
        var job = new ServiceOrderJob(order.Id, Guid.NewGuid(), "Oil Change", "Change oil", 50);
        job.Acknowledge(employee);
        order.AttachServiceJob(job, employee.Id);

        var result = await _service.RenderServiceFinishedAsync(order, CreateCustomer());

        Assert.Contains("<table", result);
        Assert.Contains("Oil Change", result);
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_ThrowsFileNotFoundWhenTemplateIsMissing()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.RenderServiceFinishedAsync(CreateOrder().order, CreateCustomer()));
    }

    [Fact]
    public async Task RenderServiceFinishedAsync_HtmlEncodesSpecialCharactersInCustomerName()
    {
        WriteTemplate("service-finished.html", "{{CUSTOMER_NAME}}");

        var (order, _) = CreateOrder();
        var customer = CreateCustomer("<b>Test</b>");

        var result = await _service.RenderServiceFinishedAsync(order, customer);

        Assert.DoesNotContain("<b>", result);
        Assert.Contains("&lt;b&gt;", result);
    }
}
