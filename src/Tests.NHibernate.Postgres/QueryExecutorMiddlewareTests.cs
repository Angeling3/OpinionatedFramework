using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IOKode.OpinionatedFramework.Persistence.Queries;
using IOKode.OpinionatedFramework.Tests.NHibernate.Postgres.Config;
using Xunit;
using Xunit.Abstractions;

namespace IOKode.OpinionatedFramework.Tests.NHibernate.Postgres;

[Collection(nameof(NHibernateTestsFixtureCollection))]
public class QueryExecutorMiddlewareTests(NHibernateTestsFixture fixture, ITestOutputHelper outputHelper)
    : NHibernateTestsBase(fixture, outputHelper)
{
    public static readonly List<string> ExecutedMiddlewares = new();

    private IQueryExecutor CreateExecutor(params QueryMiddleware[] middlewares)
    {
        var factory = Locator.Resolve<IQueryExecutorFactory>();
        return factory.Create(middlewares);
    }

    [Fact]
    public async Task MiddlewareIsInvoked_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);");

        CounterMiddleware.Counter = 0;
        var queryExecutor = CreateExecutor(new CounterMiddleware());

        // Act
        var result = await queryExecutor.QueryAsync<UserDto>("SELECT id, name, email, is_active FROM Users", null);

        // Assert
        Assert.Equal(1, CounterMiddleware.Counter);
        Assert.Single(result);
        Assert.Equal("Test User", result.First().Name);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task MiddlewarePipelineExecutesInOrder_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);");

        ExecutedMiddlewares.Clear();
        CounterMiddleware.Counter = 0;
        CounterMiddleware2.Counter = 0;

        var queryExecutor = CreateExecutor(new CounterMiddleware(), new CounterMiddleware2());

        // Act
        var result = await queryExecutor.QueryAsync<UserDto>("SELECT id, name, email, is_active FROM Users", null);

        // Assert
        Assert.Equal(1, CounterMiddleware.Counter);
        Assert.Equal(1, CounterMiddleware2.Counter);
        Assert.Equal(2, ExecutedMiddlewares.Count);
        Assert.Equal(new List<string> { "Counter", "Counter2" }, ExecutedMiddlewares);
        Assert.Single(result);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task MiddlewareCanModifyQuery_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);");
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('2', 'Another User', 'another@example.com', true);");

        var queryExecutor = CreateExecutor(new QueryModifierMiddleware());

        // Act
        var result = await queryExecutor.QueryAsync<UserDto>("SELECT id, name, email, is_active FROM Users", null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test User", result.First().Name);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task MiddlewareCanHandleExceptions_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();

        ExceptionHandlingMiddleware.ExceptionHandled = false;
        var queryExecutor = CreateExecutor(new ExceptionHandlingMiddleware());

        // Act
        await queryExecutor.QueryAsync<UserDto>("SELECT invalid_column FROM Users", null);

        // Assert
        Assert.True(ExceptionHandlingMiddleware.ExceptionHandled);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task MiddlewareCanProcessDirectives_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);");
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('2', 'Another User', 'another@example.com', false);");

        var queryExecutor = CreateExecutor(new OnlyActiveDirectiveProcessingMiddleware());

        // Act
        // SQL query with a directive comment
        var query = """
                    -- @OnlyActive
                    SELECT id, name, email, is_active FROM Users
                    """;
        var result = await queryExecutor.QueryAsync<UserDto>(query, null);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test User", result.First().Name);
        Assert.True(result.First().IsActive);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task ComplexMiddlewarePipeline_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);");
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('2', 'Another User', 'another@example.com', false);");

        ExecutedMiddlewares.Clear();
        CounterMiddleware.Counter = 0;

        var queryExecutor = CreateExecutor(
            new CounterMiddleware(),
            new OnlyActiveDirectiveProcessingMiddleware(),
            new LoggingMiddleware(),
            new ExceptionHandlingMiddleware()
        );

        // Act
        var query = """
                    -- @OnlyActive
                    SELECT id, name, email, is_active FROM Users
                    """;
        var result = await queryExecutor.QueryAsync<UserDto>(query, null);

        // Assert
        Assert.Equal(1, CounterMiddleware.Counter);
        Assert.Single(result);
        Assert.Equal("Test User", result.First().Name);
        Assert.True(result.First().IsActive);
        Assert.Contains("Counter", ExecutedMiddlewares);
        Assert.Contains("Logging: Before", ExecutedMiddlewares);
        Assert.Contains("Logging: After", ExecutedMiddlewares);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task MiddlewareWithTransaction_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();

        var queryExecutor = CreateExecutor(new TransactionAwareMiddleware());
        await using var transaction = await npgsqlClient.BeginTransactionAsync();

        await npgsqlClient.ExecuteAsync(
            "INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);",
            transaction: transaction);

        // Act
        var result = await queryExecutor.QueryAsync<UserDto>(
            "SELECT id, name, email, is_active FROM Users",
            null,
            transaction);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test User", result.First().Name);
        Assert.True(TransactionAwareMiddleware.TransactionPresent);

        await transaction.RollbackAsync();

        // Assert the data was not committed
        var afterRollback = await npgsqlClient.QueryAsync<UserDto>("SELECT id, name, email, is_active FROM Users");
        Assert.Empty(afterRollback);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    [Fact]
    public async Task MiddlewareWithParameters_Success()
    {
        // Arrange
        await CreateUsersTableQueryAsync();
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('1', 'Test User', 'test@example.com', true);");
        await npgsqlClient.ExecuteAsync("INSERT INTO Users (id, name, email, is_active) VALUES ('2', 'Another User', 'another@example.com', true);");

        var queryExecutor = CreateExecutor(new ParameterLoggingMiddleware());
        var parameters = new UserQueryParameters { Id = "1" };

        // Act
        var result = await queryExecutor.QueryAsync<UserDto>(
            "SELECT id, name, email, is_active FROM Users WHERE id = :id", parameters);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test User", result.First().Name);
        Assert.True(ParameterLoggingMiddleware.ParametersPresent);
        Assert.Equal("1", ParameterLoggingMiddleware.LoggedParameterId);

        // Cleanup
        await DropUsersTableQueryAsync();
    }

    public class UserDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }
}

public class CounterMiddleware : QueryMiddleware
{
    public static int Counter { get; set; } = 0;

    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        QueryExecutorMiddlewareTests.ExecutedMiddlewares.Add("Counter");
        Counter++;
        await nextAsync();
    }
}

public class CounterMiddleware2 : QueryMiddleware
{
    public static int Counter { get; set; } = 0;

    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        QueryExecutorMiddlewareTests.ExecutedMiddlewares.Add("Counter2");
        Counter++;
        await nextAsync();
    }
}

public class QueryModifierMiddleware : QueryMiddleware
{
    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        executionContext.RawQuery += " WHERE id = '1'";

        await nextAsync();
    }
}

public class ExceptionHandlingMiddleware : QueryMiddleware
{
    public static bool ExceptionHandled { get; set; } = false;

    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        try
        {
            await nextAsync();
        }
        catch (Exception)
        {
            ExceptionHandled = true;
        }
    }
}

public class OnlyActiveDirectiveProcessingMiddleware : QueryMiddleware
{
    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        if (executionContext.Directives.Contains("OnlyActive"))
        {
            string modifiedQuery = executionContext.RawQuery + " WHERE is_active = true";
            executionContext.RawQuery = modifiedQuery;
        }

        await nextAsync();
    }
}

public class LoggingMiddleware : QueryMiddleware
{
    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        QueryExecutorMiddlewareTests.ExecutedMiddlewares.Add("Logging: Before");
        await nextAsync();
        QueryExecutorMiddlewareTests.ExecutedMiddlewares.Add("Logging: After");

        if (executionContext.IsExecuted)
        {
            QueryExecutorMiddlewareTests.ExecutedMiddlewares.Add($"Logging: Got {executionContext.Results.Count} results");
        }
    }
}

public class TransactionAwareMiddleware : QueryMiddleware
{
    public static bool TransactionPresent { get; set; }

    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        TransactionPresent = executionContext.Transaction != null;
        await nextAsync();
    }
}

public class UserQueryParameters
{
    public string Id { get; set; }
}

public class ParameterLoggingMiddleware : QueryMiddleware
{
    public static bool ParametersPresent { get; set; }
    public static string LoggedParameterId { get; set; }

    public override async Task ExecuteAsync(IQueryExecutionContext executionContext, InvokeNextMiddlewareDelegate nextAsync)
    {
        ParametersPresent = executionContext.Parameters != null;

        if (executionContext.Parameters is UserQueryParameters parameters)
        {
            LoggedParameterId = parameters.Id;
        }

        await nextAsync();
    }
}