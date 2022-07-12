using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace DurDB
{
  public class ConnectionStrings
  {

    #region Properties

    public string? DefaultConnection { get; set; }

    #endregion


    #region Functions

    public SqlConnection GetDefaultConnection(bool open = true)
    {
      var sqlcon = new SqlConnection(this.DefaultConnection);
      if (open)
      {
        sqlcon.Open();
      }
      return sqlcon;
    }


    public async Task<SqlConnection> GetDefaultConnectionAsync(bool open = true)
    {
      var sqlcon = new SqlConnection(this.DefaultConnection);
      if (open)
      {
        await sqlcon.OpenAsync();
      }
      return sqlcon;
    }

    #endregion

  }
}
