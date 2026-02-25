using System.Data;
using Microsoft.Data.SqlClient;

namespace EasyCLib.NET.Sdk
{
    public class SqlServerDbFunction : IDbFunction
    {
        private SqlConnection? _connection;
        private SqlDataAdapter? _adapter;
        private DataSet? _dataSet;
        private SqlDataReader? _reader;
        private SqlCommand? _readerCommand;

        public DataView SqlDataView => _dataSet?.Tables[0]?.DefaultView ?? new DataView();

        public System.Data.Common.DbDataReader SqlServerReader => _reader!;

        public string? LastError => null;

        public void DbConnect(string connectionString)
        {
            DbClose();
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        public void DbClose()
        {
            _reader?.Close();
            _reader?.Dispose();
            _reader = null;
            _readerCommand?.Dispose();
            _readerCommand = null;
            _adapter?.Dispose();
            _adapter = null;
            _dataSet?.Dispose();
            _dataSet = null;
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        public bool SelectDbDataView(string sql, string tableName)
        {
            if (_connection == null) return false;
            try
            {
                _dataSet?.Dispose();
                _dataSet = new DataSet();
                _adapter?.Dispose();
                _adapter = new SqlDataAdapter(sql, _connection);
                _adapter.Fill(_dataSet, tableName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SelectDbDataViewWithParams(string sql, string tableName, Dictionary<string, object> parameters)
        {
            if (_connection == null) return false;
            try
            {
                _dataSet?.Dispose();
                _dataSet = new DataSet();
                _adapter?.Dispose();
                using var cmd = new SqlCommand(sql, _connection);
                if (parameters != null)
                    foreach (var p in parameters)
                        cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                using var adapter = new SqlDataAdapter(cmd);
                adapter.Fill(_dataSet, tableName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SelectDbReader(string sql)
        {
            if (_connection == null) return false;
            try
            {
                _reader?.Close();
                _reader?.Dispose();
                _readerCommand?.Dispose();
                _readerCommand = new SqlCommand(sql, _connection);
                _reader = _readerCommand.ExecuteReader();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool AlterDb(string sql)
        {
            if (_connection == null) return false;
            try
            {
                using var cmd = new SqlCommand(sql, _connection);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
