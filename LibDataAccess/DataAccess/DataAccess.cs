using Dapper;
using Npgsql;
using System.Data;

namespace LibDataAccess;

public class DataAccess(IDbConnection dbConnection) : IDataAccess
{
    private readonly IDbConnection _dbConnection = dbConnection;
    public async Task<T?> QueryFirstOrDefaultAsync<T>(SqlParamsModel sqlModel, string outParam = "")
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }
        IDbTransaction transaction = _dbConnection.BeginTransaction();
        T? result;
        if (string.IsNullOrWhiteSpace(outParam))
        {
            result = await _dbConnection.QueryFirstOrDefaultAsync<T>(sqlModel.Sql, sqlModel.Parameters, commandType: sqlModel.CommandType
                , transaction: transaction);
        }
        else
        {
            await _dbConnection.QueryFirstOrDefaultAsync<T>(sqlModel.Sql,
                    sqlModel.Parameters, commandType: CommandType.Text, transaction: transaction);

            result = await _dbConnection.QueryFirstOrDefaultAsync<T>("FETCH ALL IN \"" + outParam.Replace("\\", "\\\\") + "\";",
                   null, commandType: CommandType.Text, transaction: transaction);
        }
        transaction.Commit();
        _dbConnection.Close();
        return result;
    }

    public async Task<IEnumerable<T>?> QueryAsync<T>(SqlParamsModel sqlModel, string outCursor = "")
    {
        await using var connection = new NpgsqlConnection(_dbConnection.ConnectionString);
        await connection.OpenAsync();

        IEnumerable<T>? result;

        if (string.IsNullOrWhiteSpace(outCursor))
        {
            result = await connection.QueryAsync<T>(
                sqlModel.Sql,
                sqlModel.Parameters,
                commandType: sqlModel.CommandType
            );
        }
        else
        {
            await using var transaction = await connection.BeginTransactionAsync();

            await connection.ExecuteAsync(
                sqlModel.Sql,
                sqlModel.Parameters,
                transaction: transaction,
                commandType: CommandType.Text
            );

            var fetchSql = $"FETCH ALL IN \"{outCursor}\";";
            result = await connection.QueryAsync<T>(
                fetchSql,
                transaction: transaction,
                commandType: CommandType.Text
            );

            await transaction.CommitAsync();
        }

        return result;
    }

    public Task<int> ExecuteAsync(SqlParamsModel sqlModel)
    {
        Task<int> result = _dbConnection.ExecuteAsync(sqlModel.Sql, sqlModel.Parameters, commandType: sqlModel.CommandType);
        return result;
    }

    public async Task ExecuteAsync(IEnumerable<SqlParamsModel> sqlParams)
    {
        if (_dbConnection.State != ConnectionState.Open)
        {
            _dbConnection.Open();
        }
        IDbTransaction transaction = _dbConnection.BeginTransaction();
        foreach (SqlParamsModel sqlParam in sqlParams)
        {
            await _dbConnection.ExecuteAsync(sqlParam.Sql, sqlParam.Parameters, commandType: sqlParam.CommandType, transaction: transaction);
        }
        transaction.Commit();
        _dbConnection.Close();
    }
}
