namespace Lite.Procedures.Configuration
{
    public interface IProcedureConfiguration<TProcedure>
    {
        void Configure(IProcedureBuilder builder);
    }
    
    public interface IProcedureConfiguration<TArgs, TResult>
    {
        void Configure(IProcedureBuilder builder);
    }
}