using System.Data;
using Microsoft.Data.Sqlite;

namespace EasyCLib.NET.Sdk
{
    public class SqliteDbFunction : IDbFunction
    {
        private SqliteConnection? _connection;
        private DataSet? _dataSet;
        private SqliteDataReader? _reader;
        private SqliteCommand? _readerCommand;

        public DataView SqlDataView => _dataSet?.Tables[0]?.DefaultView ?? new DataView();

        public System.Data.Common.DbDataReader SqlServerReader => _reader!;

        public string? LastError { get; private set; }

        public void DbConnect(string connectionString)
        {
            DbClose();
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
        }

        public void DbClose()
        {
            _reader?.Close();
            _reader?.Dispose();
            _reader = null;
            _readerCommand?.Dispose();
            _readerCommand = null;
            _dataSet?.Dispose();
            _dataSet = null;
            _connection?.Close();
            _connection?.Dispose();
            _connection = null;
        }

        public bool SelectDbDataView(string sql, string tableName)
        {
            LastError = null;
            if (_connection == null) return false;
            try
            {
                _dataSet?.Dispose();
                _dataSet = new DataSet { EnforceConstraints = false };
                using var cmd = new SqliteCommand(sql, _connection);
                using var reader = cmd.ExecuteReader();
                var table = LoadReaderToTable(reader, tableName);
                _dataSet.Tables.Add(table);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public bool SelectDbDataViewWithParams(string sql, string tableName, Dictionary<string, object> parameters)
        {
            LastError = null;
            if (_connection == null) return false;
            try
            {
                _dataSet?.Dispose();
                _dataSet = new DataSet { EnforceConstraints = false };
                using var cmd = new SqliteCommand(sql, _connection);
                if (parameters != null)
                    foreach (var p in parameters)
                        cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                using var reader = cmd.ExecuteReader();
                var table = LoadReaderToTable(reader, tableName);
                _dataSet.Tables.Add(table);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        /// <summary>載入 Reader 至 DataTable，不推斷主鍵/約束，避免 JOIN 查詢重複值觸發約束錯誤</summary>
        private static DataTable LoadReaderToTable(System.Data.Common.DbDataReader reader, string tableName)
        {
            var table = new DataTable(tableName);
            for (int i = 0; i < reader.FieldCount; i++)
                table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
            while (reader.Read())
            {
                var row = table.NewRow();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                table.Rows.Add(row);
            }
            return table;
        }

        public bool SelectDbReader(string sql)
        {
            if (_connection == null) return false;
            try
            {
                _reader?.Close();
                _reader?.Dispose();
                _readerCommand?.Dispose();
                _readerCommand = new SqliteCommand(sql, _connection);
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
                using var cmd = new SqliteCommand(sql, _connection);
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
