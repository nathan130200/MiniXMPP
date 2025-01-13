using System.Reflection;
using MiniXmpp.Attributes;

namespace MiniXmpp.Collections;

public static class XmppEnum
{
    static class Cache<TEnum> where TEnum : struct, Enum
    {
        public static IEnumerable<TEnum> Values { get; }
        public static IReadOnlyDictionary<TEnum, string> EnumToXmpp { get; }

        static Cache()
        {
            var values = new List<TEnum>();
            var mapping = new Dictionary<TEnum, string>();

            foreach (var field in typeof(TEnum)
                .GetFields()
                .Where(x => x.FieldType == typeof(TEnum)))
            {
                var self = (TEnum)field.GetValue(null)!;

                values.Add(self);

                var attr = field.GetCustomAttribute<XmppMemberAttribute>();

                if (attr != null)
                    mapping[self] = attr.Value;
            }

            Values = values;
            EnumToXmpp = mapping;
        }
    }

    public static string? ToXmpp<TEnum>(this TEnum value) where TEnum : struct, Enum
        => Cache<TEnum>.EnumToXmpp.GetValueOrDefault(value);

    public static TEnum FromXmpp<TEnum>(string str) where TEnum : struct, Enum
        => Cache<TEnum>.EnumToXmpp.FirstOrDefault(x => x.Value == str).Key;
}
