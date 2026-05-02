using CarRepairShop.Domain.Entities;
using CarRepairShop.Domain.Enums;
using CarRepairShop.Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace CarRepairShop.Services.Implementations;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IHostEnvironment _hostEnvironment;

    public EmailTemplateService(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public async Task<string> RenderServiceReceivedAsync(
        ServiceOrder order,
        Customer customer,
        Vehicle vehicle,
        User assignedEmployee)
    {
        var template = await ReadTemplateAsync("service-received.html");

        return template
            .Replace("{{CUSTOMER_NAME}}", HtmlEncode(customer.Name))
            .Replace("{{SERVICE_ID}}", order.Id.ToString())
            .Replace("{{VEHICLE_YEAR}}", vehicle.Year.ToString())
            .Replace("{{VEHICLE_BRAND}}", HtmlEncode(vehicle.Brand))
            .Replace("{{VEHICLE_MODEL}}", HtmlEncode(vehicle.Model))
            .Replace("{{VEHICLE_LICENSE_PLATE}}", HtmlEncode(vehicle.LicensePlate))
            .Replace("{{EMPLOYEE_NAME}}", HtmlEncode(assignedEmployee.Name))
            .Replace("{{OPENED_AT}}", order.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
    }

    public async Task<string> RenderWaitingForApprovalAsync(
        ServiceOrder order,
        Customer customer,
        string approvalUrl)
    {
        var template = await ReadTemplateAsync("service-waiting-approval.html");

        var itemsHtml = BuildItemsHtml(order);
        var jobsHtml = BuildJobsHtml(order);

        var vehicle = order.Vehicle;

        return template
            .Replace("{{CUSTOMER_NAME}}", HtmlEncode(customer.Name))
            .Replace("{{SERVICE_ID}}", order.Id.ToString())
            .Replace("{{VEHICLE_YEAR}}", vehicle?.Year.ToString() ?? "")
            .Replace("{{VEHICLE_BRAND}}", HtmlEncode(vehicle?.Brand ?? ""))
            .Replace("{{VEHICLE_MODEL}}", HtmlEncode(vehicle?.Model ?? ""))
            .Replace("{{VEHICLE_LICENSE_PLATE}}", HtmlEncode(vehicle?.LicensePlate ?? ""))
            .Replace("{{TOTAL_PRICE}}", order.TotalPrice.ToString("C"))
            .Replace("{{SERVICE_ITEMS_HTML}}", itemsHtml)
            .Replace("{{SERVICE_JOBS_HTML}}", jobsHtml)
            .Replace("{{APPROVAL_URL}}", approvalUrl);
    }

    public async Task<string> RenderServiceFinishedAsync(ServiceOrder order, Customer customer)
    {
        var template = await ReadTemplateAsync("service-finished.html");

        var itemsHtml = BuildItemsHtml(order);
        var jobsHtml = BuildJobsHtml(order);

        var vehicle = order.Vehicle;

        // Use the timestamp from the StatusHistory when the order transitioned to Finished
        var finishedAt = order.StatusHistory
            .Where(h => h.ToStatus == ServiceStatus.Finished)
            .OrderByDescending(h => h.ChangedAt)
            .Select(h => h.ChangedAt)
            .FirstOrDefault();

        var completedAtText = finishedAt != default
            ? finishedAt.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
            : (order.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") + " UTC" ?? "N/A");

        return template
            .Replace("{{CUSTOMER_NAME}}", HtmlEncode(customer.Name))
            .Replace("{{SERVICE_ID}}", order.Id.ToString())
            .Replace("{{VEHICLE_YEAR}}", vehicle?.Year.ToString() ?? "")
            .Replace("{{VEHICLE_BRAND}}", HtmlEncode(vehicle?.Brand ?? ""))
            .Replace("{{VEHICLE_MODEL}}", HtmlEncode(vehicle?.Model ?? ""))
            .Replace("{{VEHICLE_LICENSE_PLATE}}", HtmlEncode(vehicle?.LicensePlate ?? ""))
            .Replace("{{TOTAL_PRICE}}", order.TotalPrice.ToString("C"))
            .Replace("{{COMPLETED_AT}}", completedAtText)
            .Replace("{{SERVICE_ITEMS_HTML}}", itemsHtml)
            .Replace("{{SERVICE_JOBS_HTML}}", jobsHtml);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> ReadTemplateAsync(string fileName)
    {
        var templatesPath = Path.Combine(_hostEnvironment.ContentRootPath, "Templates", fileName);
        if (!File.Exists(templatesPath))
            throw new FileNotFoundException($"Email template '{fileName}' not found at '{templatesPath}'.");

        return await File.ReadAllTextAsync(templatesPath);
    }

    private static string BuildItemsHtml(ServiceOrder order)
    {
        if (!order.ServiceItems.Any())
            return "<p style=\"color:#888;\">No items recorded.</p>";

        var sb = new StringBuilder("<table style=\"width:100%;border-collapse:collapse;\">");
        sb.Append("<tr><th style=\"text-align:left;padding:6px;background:#f0f0f0;\">Description</th>");
        sb.Append("<th style=\"text-align:right;padding:6px;background:#f0f0f0;\">Qty</th>");
        sb.Append("<th style=\"text-align:right;padding:6px;background:#f0f0f0;\">Unit Price</th>");
        sb.Append("<th style=\"text-align:right;padding:6px;background:#f0f0f0;\">Subtotal</th></tr>");

        foreach (var item in order.ServiceItems)
        {
            sb.Append($"<tr><td style=\"padding:6px;border-bottom:1px solid #eee;\">{HtmlEncode(item.Description)}</td>");
            sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;text-align:right;\">{item.Quantity}</td>");
            sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;text-align:right;\">{item.Price:C}</td>");
            sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;text-align:right;\">{(item.Price * item.Quantity):C}</td></tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string BuildJobsHtml(ServiceOrder order)
    {
        if (!order.ServiceJobs.Any())
            return "<p style=\"color:#888;\">No jobs recorded.</p>";

        var sb = new StringBuilder("<table style=\"width:100%;border-collapse:collapse;\">");
        sb.Append("<tr><th style=\"text-align:left;padding:6px;background:#f0f0f0;\">Job</th>");
        sb.Append("<th style=\"text-align:left;padding:6px;background:#f0f0f0;\">Responsible</th>");
        sb.Append("<th style=\"text-align:right;padding:6px;background:#f0f0f0;\">Cost</th>");
        sb.Append("<th style=\"text-align:left;padding:6px;background:#f0f0f0;\">Status</th></tr>");

        foreach (var job in order.ServiceJobs)
        {
            var workerName = job.AssignedUser?.Name ?? "Unassigned";
            sb.Append($"<tr><td style=\"padding:6px;border-bottom:1px solid #eee;\"><strong>{HtmlEncode(job.Name)}</strong><br/><small>{HtmlEncode(job.Description)}</small></td>");
            sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;\">{HtmlEncode(workerName)}</td>");
            sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;text-align:right;\">{job.Price:C}</td>");
            sb.Append($"<td style=\"padding:6px;border-bottom:1px solid #eee;\">{job.Status}</td></tr>");
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string HtmlEncode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
}
