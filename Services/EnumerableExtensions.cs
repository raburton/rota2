using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rota2.Services
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> OrderByDynamic<T>(this IEnumerable<T> source, string propertyName, bool asc)
        {
            if (string.IsNullOrEmpty(propertyName)) return source;
            var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop == null) return source;
            return asc ? source.OrderBy(x => prop.GetValue(x, null)) : source.OrderByDescending(x => prop.GetValue(x, null));
        }
    }
}
