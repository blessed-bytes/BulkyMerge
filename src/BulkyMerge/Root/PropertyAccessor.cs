using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BulkyMerge.Root
{
    public static class PropertyAccessors
    {
        public static readonly ConcurrentDictionary<Type, object> Cache = new();
    }


    public sealed class PropertyAccessor<T>
    {
        private readonly Dictionary<string, Func<T, object>> _byName;
        private readonly List<Func<T, object>> _byIndex; 
        private readonly Dictionary<string, Action<T, object>> _setters;
        public List<PropertyInfo> Properties { get; }

        private PropertyAccessor()
        {
            Properties = typeof(T).GetProperties()
                .Where(p => p.CanRead)
                .ToList();
            _byName = Properties.ToDictionary(p => p.Name, Getter);
            _byIndex = [.. _byName.Select(x => x.Value).ToList()];
            _setters = Properties.ToDictionary(p => p.Name, Setter);
        }

        public static PropertyAccessor<T> Create()
        {
            return (PropertyAccessor<T>)PropertyAccessors.Cache.GetOrAdd(typeof(T), _ => new PropertyAccessor<T>());
        }

        private Func<T, object> Getter(PropertyInfo property)
        {
            var parameter = Expression.Parameter(typeof(T), "instance");
            var propertyAccess = Expression.Property(parameter, property);
            var convert = Expression.TypeAs(propertyAccess, typeof(object));
            return (Func<T, object>)Expression.Lambda(convert, parameter).Compile();
        }
        private Action<T, object> Setter(PropertyInfo property)
        {
            var instanceParameter = Expression.Parameter(typeof(T), "instance");
            var valueParameter = Expression.Parameter(typeof(object), "value");
            var convertedValue = Expression.Convert(valueParameter, property.PropertyType);
            var propertyAccess = Expression.Property(instanceParameter, property);
            var assign = Expression.Assign(propertyAccess, convertedValue);
            return Expression.Lambda<Action<T, object>>(assign, instanceParameter, valueParameter).Compile();
        }

        public object GetValue(T instance, string propertyName) =>
            _byName.TryGetValue(propertyName, out var getter) ? getter(instance) : null;

        public object GetValue(T instance, int index)
        {
            return _byIndex[index](instance);
        }

        public void SetValue(T instance, string propertyName, object value)
        {
            if (_setters.TryGetValue(propertyName, out var setter))
            {
                setter(instance, value);
            }
        }
    }
}
