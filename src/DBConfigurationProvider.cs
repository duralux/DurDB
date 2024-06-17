using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace DurDB
{
  public sealed class DBConfigurationProvider : ConfigurationProvider
  {

    #region Fields

    private readonly DBConfigurationSource _dBConfigurationSource;

    #endregion


    #region Initialization

    public DBConfigurationProvider(DBConfigurationSource dBConfigurationSource)
    {
      this._dBConfigurationSource = dBConfigurationSource;
    }

    #endregion


    #region Functions

    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys,
      string? parentPath)
    {
      try
      {
        //var a = earlierKeys.ToArray();
        using var connection = new Microsoft.Data.SqlClient
          .SqlConnection(this._dBConfigurationSource.ConnectionString);
        connection.Open();

        string table = this._dBConfigurationSource.ConfigurationSetting.Table;
        string kC = this._dBConfigurationSource.ConfigurationSetting.KeyColumn;
        string vC = this._dBConfigurationSource.ConfigurationSetting.ValueColumn;
        string? puc = this._dBConfigurationSource.ConfigurationSetting.PreferUserColumn;
        string u = Environment.UserName;

        string sql = $"SELECT {kC} FROM {table} ";
        if (parentPath == null)
        {
          sql += $"WHERE {kC} LIKE '{parentPath}%' AND {kC} NOT LIKE '%:%' ";
        }
        else
        {
          sql += $"WHERE {kC} LIKE '{parentPath}:%' " +
            $"AND {kC} NOT LIKE '{parentPath}:%:%' ";
        }
        if (puc != null)
        {
          sql += $" AND ({puc} = '{u}' OR {puc} IS NULL) ORDER BY {puc} DESC";
        }
        var res = connection.ExecQuery(sql)
          .Select(r => Convert.ToString(r[kC]!)!.Split(':').LastOrDefault()!)!
          .ToArray();
        return res;
      }
      catch
      {
        if (!this._dBConfigurationSource.Optional)
        {
          throw;
        }
        return Array.Empty<string>();
      }
    }

    public override bool TryGet(string key, out string value)
    {
      try
      {
        using var connection = new Microsoft.Data.SqlClient
          .SqlConnection(this._dBConfigurationSource.ConnectionString);
        connection.Open();

        string table = this._dBConfigurationSource.ConfigurationSetting.Table;
        string kC = this._dBConfigurationSource.ConfigurationSetting.KeyColumn;
        string vC = this._dBConfigurationSource.ConfigurationSetting.ValueColumn;
        string? puc = this._dBConfigurationSource.ConfigurationSetting.PreferUserColumn;
        string u = Environment.UserName;

        string sql = $"SELECT TOP 1 {vC} FROM {table} WHERE {kC} = '{key}'";
        if (puc != null)
        {
          sql += $" AND ({puc} = '{u}' OR {puc} IS NULL) ORDER BY {puc} DESC";
        }
        value = connection.ExecScalar<string>(sql);
      }
      catch
      {
        if (!this._dBConfigurationSource.Optional)
        {
          throw;
        }
        value = null!;
        return false;
      }
      return true;
    }

    public override void Set(string key, string? value)
    {
      throw new NotImplementedException("Cannot set configuration!");
    }

    #endregion

  }
}
