using System.Linq;
using Lite.Procedures;
using Lite.Procedures.Rpc;

namespace Lite.Procedures.Test;

public class RpcRegistryTests
{
    [Fact]
    public void RpcAttribute_IsDiscovered_AndExposedByRegistry()
    {
        var d = RpcRegistry.Descriptors.FirstOrDefault(x => x.ProcedureType == typeof(GetUserByIdProcedure));

        Assert.NotNull(d);
        Assert.Equal("UserService", d!.Service);
        Assert.Equal("GetById", d.Method);
        Assert.Equal("org.myservice.Membership", d.Package);
        Assert.Equal("1.0", d.Version);
        Assert.Equal(typeof(GetUserById), d.ArgumentType);
        Assert.Equal(typeof(User), d.ResultType);
        Assert.Equal(new[] { "membership", "public" }, d.Schemes);
    }

    [Fact]
    public void ForScheme_FiltersByScheme()
    {
        Assert.Contains(RpcRegistry.ForScheme("membership"), x => x.ProcedureType == typeof(GetUserByIdProcedure));
        Assert.DoesNotContain(RpcRegistry.ForScheme("does-not-exist"), x => x.ProcedureType == typeof(GetUserByIdProcedure));
    }

    public sealed record GetUserById(int Id);
    public sealed record User(int Id, string Name);

    [Rpc(Service = "UserService", Method = "GetById", Package = "org.myservice.Membership", Version = "1.0", Schemes = "membership,public")]
    public sealed class GetUserByIdProcedure : IAsyncProcedure<GetUserById, User>
    {
        public ValueTask<User> ExecuteAsync(GetUserById arguments, CancellationToken cancellationToken = default)
            => new(new User(arguments.Id, "test"));
    }
}
