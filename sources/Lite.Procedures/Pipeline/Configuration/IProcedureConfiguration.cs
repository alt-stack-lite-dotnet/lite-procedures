namespace Lite.Procedures.Configuration
{
    public interface IProcedureConfiguration<TProcedure>
    {
        void Configure(IProcedureBuilder builder);
    }
}