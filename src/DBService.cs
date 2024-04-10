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
    private readonly ConnectionStrings _connectionStrings;

    private readonly SqlConnection _sqlConnection;
    public SqlConnection SqlConnection => this._sqlConnection;

  #endregion


  #region Initialization

  public DBService(ILogger<DBService> logger,
    IOptions<ConnectionStrings> connectionStrings)
  {
    this._logger = logger;
    this._connectionStrings = connectionStrings.Value;
    this._sqlConnection = new SqlConnection(this._connectionStrings.DefaultConnection);
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

  #endregion

}
}
