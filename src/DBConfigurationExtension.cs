using Microsoft.Extensions.Configuration;

namespace DurDB
{
  public static class DBConfigurationExtension
  {

    #region Static Functions

    public static IConfigurationBuilder AddDB(this IConfigurationBuilder builder,
      string connectionString, DBConfigurationSetting configurationSetting,
      bool optional = false)
    {
      return builder.Add(new DBConfigurationSource
      {
        ConnectionString = connectionString,
        ConfigurationSetting = configurationSetting,
        Optional = optional
      });
    }

    #endregion

  }
}
