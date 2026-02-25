using System.Data;

namespace EasyCLib.NET.Sdk
{
    public interface IDbFunction
    {
        void DbConnect(string connectionString);
        void DbClose();
        bool SelectDbDataView(string sql, string tableName);
        bool SelectDbDataViewWithParams(string sql, string tableName, Dictionary<string, object> parameters);
        DataView SqlDataView { get; }
        bool SelectDbReader(string sql);
        System.Data.Common.DbDataReader SqlServerReader { get; }
        bool AlterDb(string sql);
    string? LastError { get; }
    }
}
