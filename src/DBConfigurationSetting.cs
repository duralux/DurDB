namespace DurDB
{
  public class DBConfigurationSetting
  {

    #region Properties

    public string Table { get; set; } = null!;
    public string KeyColumn { get; set; } = null!;
    public string ValueColumn { get; set; } = null!;
    public string? PreferUserColumn { get; set; }

    #endregion

  }
}
