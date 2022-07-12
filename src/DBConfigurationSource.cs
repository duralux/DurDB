using Microsoft.Extensions.Configuration;

namespace DurDB
{
  public sealed class DBConfigurationSource : IConfigurationSource
  {

    #region Properties

    public string ConnectionString { get; set; } = null!;
    public DBConfigurationSetting ConfigurationSetting { get; set; } = null!;
    public bool Optional { get; set; }

    #endregion


    #region Function

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
      return new DBConfigurationProvider(this);
    }

    #endregion

  }
}
