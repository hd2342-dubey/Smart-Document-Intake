using System.Data;
using Dapper;

namespace LibDataAccess;

public class SqlParamsModel(string sql, bool hasParams)
{
    public string Sql { get; init; } = sql;
    public DynamicParameters Parameters { get; init; } = new DynamicParameters();
    public CommandType CommandType { get; init; } = hasParams ? CommandType.StoredProcedure : CommandType.Text;

    public void AddInParam<T>(string name, T value, DbType dbType)
    {
        Parameters.Add(name, value, dbType: dbType, direction: ParameterDirection.Input);
    }
    public void AddInParamArray<T>(string name, IEnumerable<T> value, DbType dbType)
    {
        T[] valueArray = value.ToArray();
        Parameters.Add(name, valueArray, dbType: dbType, direction: ParameterDirection.Input, size: valueArray.Length);
    }
    public void AddOutParam<T>(string name, T value, DbType dbType)
    {
        Parameters.Add(name, value, dbType: dbType, direction: ParameterDirection.Output);
    }

    public void AddOutParam(string name, DbType dbType)
    {
        Parameters.Add(name, dbType: dbType, direction: ParameterDirection.Output);
    }

    public void AddOutParam<T>(string name, T value, DbType dbType, int size)
    {
        Parameters.Add(name, value, dbType: dbType, direction: ParameterDirection.Output, size: size);
    }

    public T GetOutParamValue<T>(string name)
    {
        return Parameters.Get<T>(name);
    }
}
