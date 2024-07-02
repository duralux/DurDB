using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DurDB
{
  public static class ExtensionHelper
  {

    public const string ISOLATION = "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED ";

    #region Static Functions

    public static System.Data.DataTable ConvertToDataTable(
      this IEnumerable<Dictionary<string, object?>> list)
    {
      var result = new System.Data.DataTable();
      if (!list.Any())
      { return result; }

      var columnNames = list
        .SelectMany(dict => dict.Keys)
        .Distinct();

      result.Columns
        .AddRange(columnNames.Select(c => new System.Data.DataColumn(c))
        .ToArray());
      foreach (var lrow in list)
      {
        var row = result.NewRow();
        foreach (var key in lrow.Keys)
        {
          row[key] = lrow[key];
        }

        result.Rows.Add(row);
      }

      return result;
    }


    public static IEnumerable<Dictionary<string, object?>> ConvertFromDataTable(
      this System.Data.DataTable data)
    {
      var result = new List<Dictionary<string, object?>>();
      if (data.Rows.Count == 0)
      { return result; }

      foreach (System.Data.DataRow row in data.Rows)
      {
        if (row.RowState == System.Data.DataRowState.Unchanged || 
          row.RowState == System.Data.DataRowState.Modified ||
          row.RowState == System.Data.DataRowState.Added)
        {
          var dict = new Dictionary<string, object?>();
          foreach (System.Data.DataColumn col in data.Columns)
          {
            dict.Add(col.ColumnName, row[col.ColumnName]);
          }
          result.Add(dict);
        }
      }

      return result;
    }


    /// <summary>
    /// Returns true if the datatype represents a numeric value
    /// </summary>
    /// <param name="value">Object to be checked</param>
    /// <returns>True if numeric, othwerise false</returns>
    public static bool IsNumeric(this object value)
    {
      return value is sbyte
        || value is byte
        || value is short
        || value is ushort
        || value is int
        || value is uint
        || value is long
        || value is ulong
        || value is float
        || value is double
        || value is decimal;
    }


    /// <summary>
    /// Gets the first attribute of a specified type (class) if available, otherwise null
    /// </summary>
    /// <param name="type">Type containing the attribute</param>
    /// <typeparam name="T">Attribute of desire</typeparam>
    /// <returns>Attribute instance of the given type or null</returns>
    public static T? GetAttribute<T>(this Type type, bool inherite = true)
    {
      return (T?)type
        .GetCustomAttributes(typeof(T), inherite)
        .FirstOrDefault();
    }


    /// <summary>
    /// Gets the first attribute of a specified type (class) if available, otherwise null
    /// </summary>
    /// <param name="type">Type containing the attribute</param>
    /// <typeparam name="T">Attribute of desire</typeparam>
    /// <returns>Attribute instance of the given type or null</returns>
    public static T? GetAttribute<T>(this object obj, bool inherite = true)
    {
      return (T?)obj.GetType().GetAttribute<T>(inherite);
    }


    /// <summary>
    /// Gets the first attribute of a specified type (class) if available, otherwise null
    /// </summary>
    /// <param name="type">Type containing the attribute</param>
    /// <typeparam name="T">Attribute of desire</typeparam>
    /// <returns>Attribute instance of the given type or null</returns>
    public static IEnumerable<PropertyInfo> GetPropertyInfos<T>(this object obj,
      bool inherite = true)
    {
      return obj.GetType().GetPropertyInfos<T>(inherite);
    }


    /// <summary>
    /// Gets the first attribute of a specified type (class) if available, otherwise null
    /// </summary>
    /// <param name="type">Type containing the attribute</param>
    /// <typeparam name="T">Attribute of desire</typeparam>
    /// <returns>Attribute instance of the given type or null</returns>
    public static IEnumerable<PropertyInfo> GetPropertyInfos<T>(this Type type,
      bool inherite = true)
    {
      return type
        .GetProperties()
        .Where(p => p.GetCustomAttributes(typeof(T), inherite).Length > 0);
    }


    public static Dictionary<PropertyInfo, IEnumerable<T>> GetPropertiesAndAttributes<T>(
      this Type type, bool inherite = true)
      where T : Attribute
    {
      var propAttributes = new Dictionary<PropertyInfo, IEnumerable<T>>();
      foreach (var property in type.GetPropertyInfos<T>())
      {
        propAttributes.Add(property, property.GetAttributes<T>(inherite));
      }

      return propAttributes;
    }


    /// <summary>
    /// Gets the first attribute of a specified property if available, otherwise null
    /// </summary>
    /// <param name="property">Property containing the attribute</param>
    /// <typeparam name="T">Attribute of desire</typeparam>
    /// <returns>Attribute instance of the given property or null</returns>
    public static T? GetAttribute<T>(this PropertyInfo property, bool inherite = true)
    {
      return (T?)property
        .GetCustomAttributes(typeof(T), inherite)
        .FirstOrDefault();
    }


    /// <summary>
    /// Gets all attributes of a specified property if available
    /// </summary>
    /// <param name="property">Property containing the attribute</param>
    /// <typeparam name="T">Attribute of desire</typeparam>
    /// <returns>Attributes instances of the given property</returns>
    public static IEnumerable<T> GetAttributes<T>(this PropertyInfo property,
      bool inherite = true)
    {
      return property
        .GetCustomAttributes(typeof(T), inherite)
        .Cast<T>();
    }


    public static Func<T, object> BuildUntypedGetter<T>(this MemberInfo memberInfo)
    {
      var targetType = memberInfo.DeclaringType;
      var exInstance = Expression.Parameter(targetType!);

      var exMemberAccess = Expression.MakeMemberAccess(exInstance, memberInfo);       // t.PropertyName
      var exConvertToObject = Expression.Convert(exMemberAccess, typeof(object));     // Convert(t.PropertyName, typeof(object))
      var lambda = Expression.Lambda<Func<T, object>>(exConvertToObject, exInstance);

      var action = lambda.Compile();
      return action;
    }


    public static Action<T, object> BuildUntypedSetter<T>(this MemberInfo memberInfo)
    {
      var targetType = memberInfo.DeclaringType;
      var exInstance = Expression.Parameter(targetType!);
      var exMemberAccess = Expression.MakeMemberAccess(exInstance, memberInfo);

      // t.PropertValue(Convert(p))
      var exValue = Expression.Parameter(typeof(object));
      var exConvertedValue = Expression.Convert(exValue, GetUnderlyingType(memberInfo));
      var exBody = Expression.Assign(exMemberAccess, exConvertedValue);

      var lambda = Expression.Lambda<Action<T, object>>(exBody, exInstance, exValue);
      var action = lambda.Compile();
      return action;
    }


    private static Type GetUnderlyingType(this MemberInfo member)
    {
      return member.MemberType switch
      {
        MemberTypes.Event => ((EventInfo)member).EventHandlerType!,
        MemberTypes.Field => ((FieldInfo)member).FieldType,
        MemberTypes.Method => ((MethodInfo)member).ReturnType,
        MemberTypes.Property => ((PropertyInfo)member).PropertyType,
        _ => throw new ArgumentException(
          "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
          ),
      };
    }


    public static object GetDefault(this Type type)
    {
      if (type.IsValueType)
      {
        return Activator.CreateInstance(type)!;
      }
      return null!;
    }


    public static object ConvertTo(this object value, Type type)
    {
      if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
      {
        return Convert.ChangeType(value, type.GenericTypeArguments[0]);
      }
      else if (value.GetType() != type)
      {
        return Convert.ChangeType(value, type);
      }
      return value;
    }


    public static Dictionary<string, string> GetDictionaryFromString(this string data,
      string delimiterKeyValue = "=", string? delimiterNewEntry = null, 
      StringComparer? stringComparer = null)
    {
      var ret = new Dictionary<string, string>(stringComparer);
#if NETSTANDARD2_0
      var rows = data.Split('\r');
#else
      var rows = data.Split(delimiterNewEntry ?? Environment.NewLine, StringSplitOptions.TrimEntries);
#endif
      foreach (var row in rows)
      {
        try
        {
#if NETSTANDARD2_0
          var splits = row.Split('=');
#else
          var splits = row.Split(delimiterKeyValue, StringSplitOptions.TrimEntries);
#endif
          var k = splits[0];
          var v = splits.Length > 1 ? splits[1] : String.Empty;

          ret[k] = v;
        }
        catch { }
      }
      return ret;
    }


    public static IEnumerable<T> OrderDynamic<T>(IEnumerable<T> Data, string propToOrder, bool descending = false)
    {
      var param = Expression.Parameter(typeof(T));
      var memberAccess = Expression.Property(param, propToOrder);
      var convertedMemberAccess = Expression.Convert(memberAccess, typeof(object));
      var orderPredicate = Expression.Lambda<Func<T, object>>(convertedMemberAccess, param);

      var qDdata = Data.AsQueryable();
      if (descending)
      { 
        return qDdata.OrderByDescending(orderPredicate);
      }
      return qDdata.OrderBy(orderPredicate);
    }

#endregion

  }
}
