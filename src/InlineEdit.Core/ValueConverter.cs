using System;

namespace InlineEdit.Core
{
    public class ValueConverter
    {
        public static T Convert<T>(string value) => (T)Convert(value, typeof(T));
        public static object Convert(string value, Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return string.IsNullOrWhiteSpace(value) ? null : Convert(value, t.GetGenericArguments()[0]);
            }

            if (t == typeof(Guid))      return new Guid(value);
            if (t.IsEnum)               return Enum.Parse(t, value);
            if (t == typeof(DateTime))  return DateTime.Parse(value);

            return System.Convert.ChangeType(value, t);
        }
    }
}
