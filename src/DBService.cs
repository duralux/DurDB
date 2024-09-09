using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DurDB
{
  public class DBService
  {

    #region Properties

    private readonly ILogger<DBService> _logger;
    private readonly ConnectionStrings _connectionStringsOriginal;

    private string? ConnectionStrings;

    public SqlConnection SqlConnection { get; }
    public static Version? AssemblyVersion => typeof(SqlConnection).Assembly.GetName().Version;

    #endregion


    #region Initialization

    public DBService(ILogger<DBService> logger,
      IOptions<ConnectionStrings> connectionStrings)
    {
      this._logger = logger;
      this._connectionStringsOriginal = connectionStrings.Value;
      this.ConnectionStrings = connectionStrings.Value.DefaultConnection;
      this.SqlConnection = new SqlConnection(this.ConnectionStrings);
    }

    #endregion


    #region Function

    public SqlConnection GetOpenConnection()
    {
      if (this.SqlConnection.State != System.Data.ConnectionState.Open)
      {
        try
        {
          this.SqlConnection.Open();
        }
        catch (Exception ex)
        {
          this._logger.LogError(ex, "Error opening sql connection");
        }
      }
      return this.SqlConnection;
    }


    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
      if (this.SqlConnection.State != System.Data.ConnectionState.Open)
      {
        try
        {
          await this.SqlConnection.OpenAsync();
        }
        catch (Exception ex)
        {
          this._logger.LogError(ex, "Error opening sql connection");
        }
      }
      return this.SqlConnection;
    }


    private void SetNewConnectionString(string server, string database, string? appName = null)
    {
      var parts = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
      var split = this._connectionStringsOriginal.DefaultConnection?.Split(';');
      if (split != null) 
      { 
        foreach (string part in split)
        {
          if (String.IsNullOrEmpty(part))
          { continue; }

          var p = part.Split('=');
          if (p.Length == 2)
          {
            parts.Add(p[0].ToUpper(), p[1]);
          }
        }
      }

      parts["SERVER"] = server;
      parts["DATABASE"] = database;
      if (appName != null)
      {
        parts["APPLICATION NAME"] = appName;
      }

      this.ConnectionStrings = String.Join(";", parts.Select(d => $"{d.Key}={d.Value}"));
    }


    public void Reconnect(string server, string database, string? appName = null)
    {
      SetNewConnectionString(server, database, appName);
      this.SqlConnection.Close();
      this.SqlConnection.ConnectionString = this.ConnectionStrings;
      this.GetOpenConnection();
    }


    public async Task ReconnectAsync(string server, string database, string? appName = null)
    {
      SetNewConnectionString(server, database, appName);
      var isOpen = this.SqlConnection.State != System.Data.ConnectionState.Open;
#if NETSTANDARD2_0
      this.SqlConnection.Close();
#else
      await this.SqlConnection.CloseAsync();
#endif
      this.SqlConnection.ConnectionString = this.ConnectionStrings;
      if (isOpen)
      {
        await this.GetOpenConnectionAsync();
      }
    }

#endregion

  }
}
