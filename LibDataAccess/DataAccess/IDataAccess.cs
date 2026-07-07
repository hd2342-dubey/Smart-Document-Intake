namespace LibDataAccess;

public interface IDataAccess
{
    Task ExecuteAsync(IEnumerable<SqlParamsModel> sqlParams);
    Task<int> ExecuteAsync(SqlParamsModel sqlModel);
    Task<IEnumerable<T>?> QueryAsync<T>(SqlParamsModel sqlModel, string outParam = "");
    Task<T?> QueryFirstOrDefaultAsync<T>(SqlParamsModel sqlModel, string outParam = "");
}
