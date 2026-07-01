namespace CarRepairShop.API.Requests;

/// <summary>Request body for adding a service item to a service order.</summary>
public record AddServiceItemRequest(Guid ServiceItemId, int Quantity);

/// <summary>Request body for adding a service job to a service order.</summary>
public record AddServiceJobRequest(Guid ServiceJobId);
