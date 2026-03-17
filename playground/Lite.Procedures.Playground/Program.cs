using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Playground;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLiteProcedures(b =>
{
    b.AddProcedure<EchoProcedure>();
    b.AddProcedure<CreateOrderProcedure>();
    b.Build();
});
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
