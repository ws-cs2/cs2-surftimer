using Dapper;
using Npgsql;

namespace WST;

public class Database
{
    private readonly string _connectionString;

    public Database()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        // _connectionString = "Host=localhost;Username=wst;Password=password;Database=wst";
        _connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")!;
    }

    // Synchronous query method
    public IEnumerable<T> Query<T>(string sql, object? param = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            return connection.Query<T>(sql, param);
        }
    }

    // Asynchronous query method
    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            return await connection.QueryAsync<T>(sql, param);
        }
    }

    // Execute a command synchronously (INSERT, UPDATE, DELETE)
    public int Execute(string sql, object? param = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            return connection.Execute(sql, param);
        }
    }

    // Execute a command asynchronously (INSERT, UPDATE, DELETE)
    public async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using (var connection = new NpgsqlConnection(_connectionString))
        {
            return await connection.ExecuteAsync(sql, param);
        }
    }
}