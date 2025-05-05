using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace DurDB
{
  public static class CSV
  {

    #region Static Functions


    public static string ConvertToCSVString<T>(
        this IEnumerable<T> data, string delimiter = "\t", bool enquote = false)
    {
      if (data == null || !data.Any())
      {
        return String.Empty;
      }

      var properties = typeof(T).GetPropertiesAndAttributes<
          System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
          .Select(prop =>
          {
            var columnAttribute = prop.Value.FirstOrDefault();
            var order = columnAttribute?.Order ?? int.MaxValue; // Ungesetzte Order-Werte ans Ende
            return new { Property = prop.Key, Name = columnAttribute?.Name ?? prop.Key.Name, Order = order };
          })
          .OrderBy(p => p.Order < 0 ? int.MaxValue : p.Order) // Nach Order sortieren
          .ToList();

      var header = String.Join(delimiter, properties.Select(p => enquote ? Enquote(p.Name) : p.Name));
      var rows = data.Select(item =>
      {
        var values = properties.Select(p =>
        {
          var value = p.Property.GetValue(item);
          var stringValue = value != null ? value.ToString() : string.Empty;
          return enquote ? Enquote(stringValue) : stringValue;
        });
        return string.Join(delimiter, values);
      });

      return string.Join(Environment.NewLine, new[] { header }.Concat(rows));
    }



    private static string Enquote(string? value)
    {
      if (value ==  null || String.IsNullOrEmpty(value))
      { return "\"\""; }

      return $"\"{value.Replace("\"", "\"\"")}\""; // Doppelte Anführungszeichen für CSV-Spezifikation
    }


    public static IEnumerable<T> ConvertFromCSVString<T>(
        this string csv, out string[]? header, string delimiter = "\t", bool enquote = false) where T : new()
    {
      header = null;

      if (String.IsNullOrEmpty(csv))
      {
        return Enumerable.Empty<T>();
      }

      var lines = csv.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);
      header = lines[0].Split([delimiter], StringSplitOptions.None);
      var lheader = header;

      var properties = typeof(T).GetPropertiesAndAttributes<
          System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()
          .Select(prop =>
          {
            var columnAttribute = prop.Value.FirstOrDefault();
            var order = columnAttribute?.Order ?? int.MaxValue; // Ungesetzte Order-Werte ans Ende
            return new { Property = prop.Key, Name = columnAttribute?.Name ?? prop.Key.Name, Order = order };
          })
          .OrderBy(p => p.Order < 0 ? int.MaxValue : p.Order) // Nach Order sortieren
          .ToList();

      var result = new List<T>();
      foreach (var line in lines.Skip(1))
      {
        var values = line.Split([delimiter], StringSplitOptions.None);
        var item = new T();
        for (int i = 0; i < header.Length; i++)
        {
          try
          {
            var property = properties.FirstOrDefault(p => p.Name == lheader[i]);
            if (property != null)
            {
              var value = values[i];
              if (enquote)
              {
                value = value.Trim('"').Replace("\"\"", "\"");
              }
            
              // Unterstützt nullable Typen
              var propertyType = Nullable.GetUnderlyingType(property.Property.PropertyType) ?? property.Property.PropertyType;
              object? convertedValue = String.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(property.Property.PropertyType) != null
                  ? null
                  : Convert.ChangeType(value, propertyType);

              property.Property.SetValue(item, convertedValue);
            }
          } catch
          {}
        }
        result.Add(item);
      }
      return result;
    }

    #endregion
  }
}
