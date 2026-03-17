namespace Lite.Procedures.Playground;

public record CreateOrderRequest(string? ProductName, int Quantity, decimal Price);

public record CreateOrderResult(Guid OrderId, string ProductName, int Quantity, decimal Total);
