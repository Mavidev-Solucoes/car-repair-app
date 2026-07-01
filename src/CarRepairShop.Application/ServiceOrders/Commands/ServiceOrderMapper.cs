using CarRepairShop.Application.DTOs;
using CarRepairShop.Domain.Entities;

namespace CarRepairShop.Application.ServiceOrders.Commands;

internal static class ServiceOrderMapper
{
    public static ServiceOrderDto MapToDto(ServiceOrder order)
    {
        var items = order.ServiceItems
            .Select(i => new ServiceOrderItemDto(i.Id, i.ServiceOrderId, i.ServiceItemId, i.Description, i.Price, i.Quantity));

        var jobs = order.ServiceJobs
            .Select(j => new ServiceOrderJobDto(
                j.Id, j.ServiceOrderId, j.ServiceJobId, j.Name, j.Description, j.Price,
                j.Status.ToString(), j.AssignedUserId, j.CreatedAt, j.CreatedUserId, j.LastUpdatedUserId));

        var history = order.StatusHistory
            .Select(h => new ServiceStatusHistoryDto(
                h.Id, h.ServiceOrderId, h.FromStatus?.ToString(), h.ToStatus.ToString(),
                h.ChangedAt, h.ChangedByUserId));

        return new ServiceOrderDto(
            order.Id,
            order.VehicleId,
            order.CustomerId,
            order.AssignedUserId,
            order.Status.ToString(),
            order.TotalPrice,
            order.CreatedAt,
            items,
            jobs,
            history);
    }
}
