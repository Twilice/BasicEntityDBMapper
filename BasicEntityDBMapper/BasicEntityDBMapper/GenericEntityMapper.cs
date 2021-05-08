using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;

namespace TLM.BasicEntityDBMapper.EntityMapper
{
    public abstract class EntityMapper<T> where T : new()
    {
        public bool Initialized { get; protected set; } = false;
        public void InitializeMapper(IDataRecord record)
        {
            PopulateOrdinals(record);
            Initialized = true;
        }

        protected abstract void PopulateOrdinals(IDataRecord record);
        public abstract T PopulateEntity(IDataRecord record);
        public abstract void PopulateEntity(IDataRecord record, ref T entity);
    }

    public class GenericEntityMapper<T> : EntityMapper<T> where T : new()
    {
        public static GenericEntityMapper<T> cachedMapper;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forceNewInstance"></param>
        /// <returns>A thread safe mapper</returns>
        public static GenericEntityMapper<T> GetEntityMapper(bool forceNewInstance = false)
        {
            if(forceNewInstance)
            {
                return new GenericEntityMapper<T>();
            }
            else
            {
                if (cachedMapper.Initialized)
                    return cachedMapper;
                else return new GenericEntityMapper<T>();
            }
        }

        public override void PopulateEntity(IDataRecord record, ref T entity)
        {
            _populateEntity(record, entity);
        }

        public override T PopulateEntity(IDataRecord record)
        {
            T entity = new T();
            return _populateEntity(record, entity);
        }


        private class PropertyOrdinalMap
        {
            public PropertyInfo Property { get; set; }
            public int Ordinal { get; set; }
        }
        private class PropertyOrdinalMap<TProperty> : PropertyOrdinalMap
        {
            public Action<T, TProperty> SetPropertyValue { get; set; }
        }
        
        private readonly List<PropertyOrdinalMap> _propertyOrdinalMappings = new List<PropertyOrdinalMap>();
        private readonly List<PropertyOrdinalMap<object>> _propertyOrdinalMappingsEnum = new List<PropertyOrdinalMap<object>>();
        private readonly List<PropertyOrdinalMap<int>> _propertyOrdinalMappingsInt = new List<PropertyOrdinalMap<int>>();
        private readonly List<PropertyOrdinalMap<long>> _propertyOrdinalMappingsLong = new List<PropertyOrdinalMap<long>>();
        private readonly List<PropertyOrdinalMap<string>> _propertyOrdinalMappingsString = new List<PropertyOrdinalMap<string>>();
        private readonly List<PropertyOrdinalMap<bool>> _propertyOrdinalMappingsBool = new List<PropertyOrdinalMap<bool>>();
        private readonly List<PropertyOrdinalMap<DateTime>> _propertyOrdinalMappingsDateTime = new List<PropertyOrdinalMap<DateTime>>();
        private readonly List<PropertyOrdinalMap<TimeSpan>> _propertyOrdinalMappingsTimeSpan = new List<PropertyOrdinalMap<TimeSpan>>();
        private readonly List<PropertyOrdinalMap<byte>> _propertyOrdinalMappingsByte = new List<PropertyOrdinalMap<byte>>();
        private readonly List<PropertyOrdinalMap<short>> _propertyOrdinalMappingsShort = new List<PropertyOrdinalMap<short>>();
        private readonly List<PropertyOrdinalMap<char>> _propertyOrdinalMappingsChar = new List<PropertyOrdinalMap<char>>();

        private readonly object initLock = new object();
        protected override void PopulateOrdinals(IDataRecord reader)
        {
            lock (initLock)
            {
                if (Initialized) return;
                // Get the PropertyInfo for our type and map them to the ordinals for the fields with the same names in our reader.  
                PropertyInfo[] properties = typeof(T).GetProperties();

                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();

                foreach (PropertyInfo property in properties)
                {
                    int index = columns.FindIndex(x => string.Equals(x, property.Name, StringComparison.OrdinalIgnoreCase));

                    if (index < 0) // if not found, check if columnattribute exist
                    {
                        var attribute = property.GetCustomAttribute<ColumnAttribute>();
                        if (attribute != null)
                        {
                            index = columns.FindIndex(x => string.Equals(x, attribute.Name, StringComparison.OrdinalIgnoreCase));
                        }
                    }

                    if (index >= 0)
                    {
                        if (property.PropertyType == typeof(int))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsInt);
                        }
                        else if (property.PropertyType == typeof(long))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsLong);
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsString);
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsBool);
                        }
                        else if (property.PropertyType == typeof(DateTime))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsDateTime);
                        }
                        else if (property.PropertyType == typeof(TimeSpan))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsTimeSpan);
                        }
                        else if (property.PropertyType == typeof(byte))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsByte);
                        }
                        else if (property.PropertyType == typeof(short))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsShort);
                        }
                        else if (property.PropertyType == typeof(char))
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsChar);
                        }
                        else if (property.PropertyType.IsEnum)
                        {
                            CreateCachedSetMethod(_propertyOrdinalMappingsEnum);
                        }
                        else
                        {
                            PropertyOrdinalMap map = new PropertyOrdinalMap();
                            map.Property = property;
                            map.Ordinal = index;
                            _propertyOrdinalMappings.Add(map);
                        }

                        void CreateCachedSetMethod<TProperty>(List<PropertyOrdinalMap<TProperty>> mappings)
                        {

                            PropertyOrdinalMap<TProperty> map = new PropertyOrdinalMap<TProperty>();
                            map.Property = property;
                            map.Ordinal = index;
                            map.SetPropertyValue = (Action<T, TProperty>)property.GetSetMethod().CreateDelegate(typeof(Action<T, TProperty>));
                            mappings.Add(map);
                        }
                    }
                }
                Initialized = true;
            }
        }     

        private T _populateEntity(IDataRecord record, T entity)
        {
            if (!Initialized)
            {
                InitializeMapper(record);
            }
         
            SetPropertyValue((row, i) => row.GetBoolean(i), _propertyOrdinalMappingsBool);
            SetPropertyValue((row, i) => row.GetByte(i), _propertyOrdinalMappingsByte);
            SetPropertyValue((row, i) => row.GetInt16(i), _propertyOrdinalMappingsShort);
            SetPropertyValue((row, i) => row.GetInt32(i), _propertyOrdinalMappingsInt);
            SetPropertyValue((row, i) => row.GetInt64(i), _propertyOrdinalMappingsLong);
            SetPropertyValue((row, i) => row.GetString(i), _propertyOrdinalMappingsString);
            SetPropertyValue((row, i) => row.GetString(i).First(), _propertyOrdinalMappingsChar);
            SetPropertyValue((row, i) => row.GetDateTime(i), _propertyOrdinalMappingsDateTime);
            SetPropertyValue((row, i) => new TimeSpan(row.GetInt64(i)), _propertyOrdinalMappingsTimeSpan);

            void SetPropertyValue<TProperty>(Func<IDataRecord, int, TProperty> getRecordOrdinalValue, List<PropertyOrdinalMap<TProperty>> mappings)
            {
                foreach (var map in mappings)
                {
                    if (!record.IsDBNull(map.Ordinal))
                    {
                        try
                        {
                            map.SetPropertyValue(entity, getRecordOrdinalValue(record, map.Ordinal));
                        }
                        catch (InvalidCastException e)
                        {
                            string message = $"{typeof(T).Name}: property {map.Property.Name} is of type {map.Property.PropertyType.Name} but sqlType from query is {record.GetDataTypeName(map.Ordinal)}";
                            throw new InvalidCastException(message, innerException: e);
                        }
                    }
                }
            }

            foreach (PropertyOrdinalMap<object> map in _propertyOrdinalMappingsEnum)
            {
                if (!record.IsDBNull(map.Ordinal))
                {
                    map.SetPropertyValue(entity, GetEnumeration(record, map));
                }
            }

            foreach (PropertyOrdinalMap map in _propertyOrdinalMappings)
            {
                if (!record.IsDBNull(map.Ordinal))
                {
                    map.Property.SetValue(entity, record.GetValue(map.Ordinal), null);
                }
            }
            return entity;
        }

        private static object GetEnumeration(IDataRecord record, PropertyOrdinalMap map)
        {
            Type propertyType = Nullable.GetUnderlyingType(map.Property.PropertyType);
            if (propertyType == null)
                propertyType = map.Property.PropertyType;

            if (record.GetFieldType(map.Ordinal) == typeof(string))
            {
                string value = record.GetString(map.Ordinal);
                try
                {
                    return Enum.Parse(propertyType, value);
                }
                catch (ArgumentException e)
                {
                    string message = $"{typeof(T).Name}: Unable to parse value: '{value}' to enum type '{propertyType.Name}' for property {map.Property.Name}.";
                    throw new ArgumentException(message, innerException: e);
                }
            }
            else if (record.GetFieldType(map.Ordinal) == typeof(string))
            {
                string value = record.GetString(map.Ordinal);
                try
                {
                    return EnumStringParser.Parse(propertyType, value); ;
                }
                catch (ArgumentException e)
                {
                    string message = $"{typeof(T).Name}: Unable to parse value: '{value}' to enum type '{propertyType.Name}' for property {map.Property.Name}.";
                    throw new ArgumentException(message, innerException: e);
                }
            }
            else
            {
                return Enum.ToObject(propertyType, record.GetValue(map.Ordinal));
            }
        }

        /// <summary>
        /// Parses a string case insensitive to enum type. With the addition of comparing Description Attribute as well as fieldname.
        /// Example: var enumValue = EnumExtractor.GetValue(typeof(Status), "OK");
        /// </summary>
        public static class EnumStringParser
        {
            public static object Parse(Type enumType, string value)
            {
                if (!enumType.IsEnum) throw new InvalidOperationException($"Tried to extract enum from non enum {enumType} with value {value}.");

                // Caution! Example why extra check is needed.
                // (example where 2 is an underlying enumtype)
                // IsDefined(type, "2") returns false and IsDefined(type, 2) returns true, but both are actually valid parses... 
                // Parse(type, "2") and Parse(type, 2) will both return the same value because "2" is converted to int
                // (and TryParse does not take a Type as parameter)

                // Check for any matching integer value
                int intValue;
                if (int.TryParse(value, out intValue))
                {
                    if (Enum.IsDefined(enumType, intValue))
                        return Enum.ToObject(enumType, intValue);
                }

                // Check any matching string
                if (Enum.GetNames(enumType).Any(x => x.Equals(value, StringComparison.OrdinalIgnoreCase)))
                    return Enum.Parse(enumType, value, true);

                // Check for matching DescriptionAttribute
                var fields = enumType.GetFields();
                foreach (var field in fields)
                {
                    var attribute = Attribute.GetCustomAttribute(field,
                        typeof(DescriptionAttribute)) as DescriptionAttribute;
                    if (attribute != null)
                    {
                        if (attribute.Description.Equals(value, StringComparison.OrdinalIgnoreCase))
                            return Enum.Parse(enumType, field.Name, true);
                    }
                }

                throw new ArgumentException($"Can't parse undefined value {value} to enum {enumType}");
            }
        }
    }
}
