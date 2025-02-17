using System.Threading.Tasks;
using Dapper;
using Npgsql;
using Xunit;

namespace IOKode.OpinionatedFramework.Tests.Events.Config;

public class EventsTestsBase(EventsTestsFixture fixture) : IClassFixture<EventsTestsFixture>
{
    protected NpgsqlConnection npgsqlClient => fixture.NpgsqlClient;
    
    protected async Task CreateEventsTableQueryAsync()
    {
        npgsqlClient.Open();
        await fixture.NpgsqlClient.ExecuteAsync(
            """
            CREATE SCHEMA IF NOT EXISTS opinionated_framework;
            CREATE TABLE opinionated_framework.events (
                id UUID PRIMARY KEY,
                event_type TEXT NOT NULL,
                dispatched_at TIMESTAMP,
                payload JSONB NOT NULL
            );
            """);
        npgsqlClient.Close();
    }

    protected async Task DropEventsTableQueryAsync()
    {
        npgsqlClient.Open();
        await npgsqlClient.ExecuteAsync("DROP TABLE opinionated_framework.events;");
        npgsqlClient.Close();
    }
}