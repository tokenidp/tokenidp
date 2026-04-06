using TokenIDP.Infrastructure.Persistence;

namespace IDP.Tests.Infrastructure;

[Collection("IDP Integration Tests")]
public class IntegrationTestBase : IClassFixture<IntegrationTestFixture>
{
    protected ApplicationDbContext? DbContext { get; }

    private readonly IntegrationTestFixture _fixture;
    protected IntegrationTestBase(IntegrationTestFixture integrationTestFixture)
    {
        DbContext = integrationTestFixture.DbContext;
        _fixture = integrationTestFixture;

        RecreateInMemoryDatabase();
    }

    protected void RecreateInMemoryDatabase()
    {
        //if (DbContext != null && !DbContext.CheckDataLoaded())
        //{
        //    DbContext.Database.EnsureDeleted();
        //    DbContext.Database.EnsureCreated();

        //    DbContext.CreateData();
        //}
    }

    public RequestBuilder NewRequest => new RequestBuilder(_fixture.HttpClient);
}