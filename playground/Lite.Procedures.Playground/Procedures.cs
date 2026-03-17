using Lite.Procedures;
using OneOf;

namespace Lite.Procedures.Playground;

public sealed class EchoProcedure : IAsyncProcedure<string, string>
{
    public ValueTask<string> InvokeAsync(string arguments, CancellationToken cancellationToken)
        => ValueTask.FromResult(arguments);
}

public sealed class CreateOrderProcedure : IAsyncProcedure<CreateOrderRequest, CreateOrderResult>
{
    public ValueTask<CreateOrderResult> InvokeAsync(CreateOrderRequest arguments, CancellationToken cancellationToken)
    {
        var total = arguments.Quantity * arguments.Price;
        return ValueTask.FromResult(
            new CreateOrderResult(Guid.NewGuid(), arguments.ProductName ?? "", arguments.Quantity, total));
    }
}
