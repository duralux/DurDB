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

    private readonly SqlConnection _sqlConnection;
    public SqlConnection SqlConnection => this._sqlConnection;

    #endregion


    #region Initialization

    public DBService(ILogger<DBService> logger,
      IOptions<ConnectionStrings> connectionStrings)
    {
      this._logger = logger;
      this._connectionStringsOriginal = connectionStrings.Value;
      this.ConnectionStrings = connectionStrings.Value.DefaultConnection;
      this._sqlConnection = new SqlConnection(this.ConnectionStrings);
    }

    #endregion


    #region Function

    public SqlConnection GetOpenConnection()
    {
      if (this._sqlConnection.State != System.Data.ConnectionState.Open)
      {
        try
        {
          this._sqlConnection.Open();
        }
        catch (Exception ex)
        {
          this._logger.LogError(ex, "Error opening sql connection");
        }
      }
      return this._sqlConnection;
    }


    public async Task<SqlConnection> GetOpenConnectionAsync()
    {
      if (this._sqlConnection.State != System.Data.ConnectionState.Open)
      {
        try
        {
          await this._sqlConnection.OpenAsync();
        }
        catch (Exception ex)
        {
          this._logger.LogError(ex, "Error opening sql connection");
        }
      }
      return this._sqlConnection;
    }


    private void SetNewConnectionString(string server, string database, string? appName = null)
    {
      var parts = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
      foreach (string part in this._connectionStringsOriginal.DefaultConnection?.Split(";") ?? Array.Empty<string>())
      {
        var p = part.Split("=");
        parts.Add(p[0], p[1]);
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
      this._sqlConnection.Close();
      this._sqlConnection.ConnectionString = this.ConnectionStrings;
      this.GetOpenConnection();
    }


    public async Task ReconnectAsync(string server, string database, string? appName = null)
    {
      SetNewConnectionString(server, database, appName);
      await this._sqlConnection.CloseAsync();
      this._sqlConnection.ConnectionString = this.ConnectionStrings;
      await this.GetOpenConnectionAsync();
    }

    #endregion

  }
}
