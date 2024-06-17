using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DurDB
{
  public static class Connection
  {

    #region Static Functions

    /// <summary>Connection-String builder for SqlServer</summary>
    /// <param name="loginType">Specifies the type of login</param>
    /// <param name="server">Address of the server</param>
    /// <param name="database">Database to be used</param>
    /// <param name="user">Name of the SQL-User</param>
    /// <param name="password">Password for the SQL-User</param>
    /// <param name="appname">Specifies the app name, if null, will be filled with 
    /// executing assambly name</param>
    /// <returns>retuns the connection string</returns>
    public static string? GetConnectionStringMsSQL(LoginType loginType, string server,
      string database, string? user = null!, string? password = null!, string? appname = null!)
    {
      appname ??= System.Reflection.Assembly.GetExecutingAssembly().GetName().Name!;

      switch (loginType)
      {
        case LoginType.WinAuth:
          return $"SERVER={server};Database={database};Application Name={appname};" +
            $"Integrated Security=True;";

        case LoginType.SQLAuth:
          if (user == null || password == null)
          {
            throw new Exception("Not all parameters are set to open a db connection! " +
              "[SQLAuth]");
          }
          return $"SERVER={server};UID={user};PWD={password};Database={database};" +
            $"Application Name={appname}";

        default:
          return null!;
      }
    }


    #region ExecScalar

    /// <summary>Returns a scalar</summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted value. NULL is represented as DBNULL</returns>
    public static T ExecScalar<T>(this DbConnection connection, DbCommand command)
    {
      command.Connection = connection;
      object ret = command.ExecuteScalar()!;
      command.Connection = null;

      var baseType = Nullable.GetUnderlyingType(typeof(T));

      if (ret == null && baseType != null)
      { return default!; }

      return Convert.IsDBNull(ret) ?
        default! : (T)Convert.ChangeType(ret, baseType ?? typeof(T))!;
    }


    /// <summary>Returns a scalar</summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="sql">SQL-Querry</param>
    /// <returns>Returns the casted value. NULL is represented as DBNULL</returns>
    public static T ExecScalar<T>(this Microsoft.Data.SqlClient.SqlConnection connection,
      string sql)
    {
      return Connection.ExecScalar<T>(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql));
    }    


    /// <summary>Returns a scalar</summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted value. NULL is represented as DBNULL</returns>
    public static async Task<T> ExecScalarAsync<T>(this DbConnection connection, DbCommand command)
    {
      command.Connection = connection;
      object ret = (await command.ExecuteScalarAsync())!;
      command.Connection = null;

      var baseType = Nullable.GetUnderlyingType(typeof(T));

      if (ret == null && baseType != null)
      { return await Task.FromResult(default(T)!); }

      return Convert.IsDBNull(ret) ?
        default! : (T)Convert.ChangeType(ret, baseType ?? typeof(T))!;
    }


    /// <summary>Returns a scalar</summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="sql">SQL-Querry</param>
    /// <returns>Returns the casted value. NULL is represented as DBNULL</returns>
    public static async Task<T> ExecScalarAsync<T>(this Microsoft.Data.SqlClient.SqlConnection connection,
      string sql)
    {
      return await Connection.ExecScalarAsync<T>(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql));
    }

    #endregion


    #region ExecQuery

    /// <summary>Returns the result in a list of dictionaries</summary>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted value in the dictionary. NULL is represented as 
    /// DBNULL</returns>
    public static IList<Dictionary<string, object?>> ExecQuery(
      this DbConnection connection, DbCommand command)
    {
      command.Connection = connection;
      var ret = new List<Dictionary<string, object?>>();

      using DbDataReader reader = command.ExecuteReader();
      var cols = new List<string>();
      for (int i = 0; i < reader.VisibleFieldCount; i++)
      { cols.Add(reader.GetName(i)); }

      while (reader.Read())
      {
        var oTMP = new Dictionary<string, object?>();
        foreach (string sCol in cols)
        {
          oTMP.Add(sCol, Convert.IsDBNull(reader[sCol]) ? null! : reader[sCol]);
        }
        ret.Add(oTMP);
      }
      return ret;
    }


    /// <summary>Returns the result in a list of dictionaries</summary>
    /// <param name="sql">SQL-Query</param>
    /// <returns>Returns the casted value in the dictionary. NULL is represented as 
    /// DBNULL</returns>
    public static IList<Dictionary<string, object?>> ExecQuery(
      this DbConnection connection, string sql)
    {
      return ExecQuery(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql));
    }


    /// <summary>Returns the result in a list custom classes</summary>
    /// <param name="sql">SQL-Query</param>
    /// <returns>Returns the casted class</returns>
    public static IEnumerable<T> ExecQuery<T>(
      this DbConnection connection, string sql) where T : new()
    {
      return ExecQuery<T>(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql));
    }


    /// <summary>Returns the result in a list custom classes</summary>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted class</returns>
    public static IList<T> ExecQuery<T>(
      this DbConnection connection, DbCommand command) where T : new()
    {
      command.Connection = connection;
      var ret = new List<T>();
      var properties = typeof(T).GetPropertiesAndAttributes<
        System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
        .Select(p => (
          Property: p.Key,
          Attribute: p.Value.First(),
          Setter: p.Key.BuildUntypedSetter<T>()
          ))
        .ToArray();

      using DbDataReader reader = command.ExecuteReader();
      while (reader.Read())
      {
        var obj = new T();
        foreach ((var property, var Attribute, var setter) in properties)
        {
          try
          {
            object value = Convert.IsDBNull(reader[Attribute.Name!]) ?
              property.PropertyType.GetDefault() :
              reader[Attribute.Name!].ConvertTo(property.PropertyType);
            setter(obj, value);
          }
          catch
          { }
        }
        ret.Add(obj);
      }
      return ret;
    }


    /// <summary>Returns the result in a list custom classes</summary>
    /// <param name="sql">SQL-Query</param>
    /// <returns>Returns the casted class</returns>
    public static async Task<IList<T>> ExecQueryAsync<T>(
      this DbConnection connection, string sql, CancellationToken cancellationToken = default) where T : new()
    {
      return await ExecQueryAsync<T>(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql), cancellationToken);
    }


    /// <summary>Returns the result in a list custom classes</summary>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted class</returns>
    public static async Task<IList<T>> ExecQueryAsync<T>(
      this DbConnection connection, DbCommand command, CancellationToken cancellationToken = default) where T : new()
    {
      command.Connection = connection;
      var ret = new List<T>();
      var properties = typeof(T).GetPropertiesAndAttributes<
        System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
        .Select(p => (
          Property: p.Key,
          Attribute: p.Value.First(),
          Setter: p.Key.BuildUntypedSetter<T>()
          ))
        .ToArray();

      using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
      while (await reader.ReadAsync(cancellationToken))
      {
        var obj = new T();
        foreach ((var property, var Attribute, var setter) in properties)
        {
          try
          {
            object value = Convert.IsDBNull(reader[Attribute.Name!]) ?
              property.PropertyType.GetDefault() :
              reader[Attribute.Name!].ConvertTo(property.PropertyType);
            setter(obj, value);
          }
          catch
          { }
        }
        ret.Add(obj);
      }
      return ret;
    }


    /// <summary>Returns the result in a list of dictionaries</summary>
    /// <param name="sql">SQL-Query</param>
    /// <returns>Returns the casted value in the dictionary. NULL is represented as 
    /// DBNULL</returns>
    public static IAsyncEnumerable<Dictionary<string, object?>> ExecQueryEnumerableAsync(
      this DbConnection connection, string sql, CancellationToken cancellationToken = default)
    {
      return ExecQueryEnumerableAsync(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql), cancellationToken);
    }


    /// <summary>Returns the result in a list of dictionaries</summary>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted value in the dictionary. NULL is represented as 
    /// DBNULL</returns>
    public static async IAsyncEnumerable<Dictionary<string, object?>> ExecQueryEnumerableAsync(
      this DbConnection connection, DbCommand command, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
      command.Connection = connection;

      using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
      var cols = new List<string>();
      for (int i = 0; i < reader.VisibleFieldCount; i++)
      { cols.Add(reader.GetName(i)); }

      while (await reader.ReadAsync(cancellationToken))
      {
        var oTMP = new Dictionary<string, object?>();
        foreach (string sCol in cols)
        {
          oTMP.Add(sCol, Convert.IsDBNull(reader[sCol]) ? null! : reader[sCol]);
        }
        yield return oTMP;
      }
    }


    /// <summary>Returns the result in a list custom classes</summary>
    /// <param name="sql">SQL-Query</param>
    /// <returns>Returns the casted class</returns>
    public static IAsyncEnumerable<T> ExecQueryEnumerableAsync<T>(
      this DbConnection connection, string sql, CancellationToken cancellationToken = default) where T : new()
    {
      return ExecQueryEnumerableAsync<T>(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql), cancellationToken);
    }


    /// <summary>Returns the result in a list custom classes</summary>
    /// <param name="command">SQL-Command</param>
    /// <returns>Returns the casted class</returns>
    public static async IAsyncEnumerable<T> ExecQueryEnumerableAsync<T>(
      this DbConnection connection, DbCommand command, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : new()
    {
      command.Connection = connection;
      var ret = new List<T>();
      var properties = typeof(T).GetPropertiesAndAttributes<
        System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
        .Select(p => (
          Property: p.Key,
          Attribute: p.Value.First(),
          Setter: p.Key.BuildUntypedSetter<T>()
          ));

      using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
      List<string> cols = new();

      for (int i = 0; i < reader.VisibleFieldCount; i++)
      { cols.Add(reader.GetName(i)); }

      while (await reader.ReadAsync(cancellationToken))
      {
        var obj = new T();
        foreach ((var property, var Attribute, var setter) in properties)
        {
          try
          {
            object value = Convert.IsDBNull(reader[Attribute.Name!]) ?
              property.PropertyType.GetDefault() :
              reader[Attribute.Name!].ConvertTo(property.PropertyType);
            setter(obj, value);
          }
          catch
          { }
        }
        yield return obj;
      }
    }
        

    #endregion


    #region DataTable


    /// <summary>
    /// Returns the result in a DataTable object
    /// </summary>
    /// <param name="command">SQL-Command</param>
    /// <returns>DataTable containing the result</returns>
    public static DataTable ExecQueryDataTable(
      this Microsoft.Data.SqlClient.SqlConnection connection,
      Microsoft.Data.SqlClient.SqlCommand command)
    {
      command.Connection = connection;
      var dataTable = new DataTable();

      using var reader = new Microsoft.Data.SqlClient.SqlDataAdapter(command);
      reader.Fill(dataTable);
      return dataTable;
    }


    /// <summary>
    /// Returns the result in a DataTable object
    /// </summary>
    /// <param name="sql">SQL-Command</param>
    /// <returns>DataTable containing the result</returns>
    public static DataTable ExecQueryDataTable(
      this Microsoft.Data.SqlClient.SqlConnection connection, string sql)
    {
      return ExecQueryDataTable(connection,
        new Microsoft.Data.SqlClient.SqlCommand(sql));
    }

    #endregion

    #endregion

  }
}
