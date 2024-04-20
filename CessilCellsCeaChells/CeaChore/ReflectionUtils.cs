using System;
using System.Reflection;

namespace CessilCellsCeaChells.CeaChore;

public static class Required<TTargetType> {
    public struct RequiredField<TFieldType>(TTargetType instance, string name) {
        private FieldInfo? Field = typeof(TTargetType).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private TTargetType Instance = instance;

        public void Set(TFieldType value) => Field?.SetValue(Instance, value);
        public TFieldType? Get()
        {
            var value = Field?.GetValue(Instance);
            if (value is TFieldType fieldType) return fieldType;
            return default;
        }
    }
    public static RequiredField<TFieldType> Field<TFieldType>(TTargetType from, string name) => new(from, name);

    public struct RequiredProperty<TPropertyType>(TTargetType instance, string name) {
        private PropertyInfo? Property = typeof(TTargetType).GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        private TTargetType Instance = instance;

        public void Set(TPropertyType value) => (Property?.GetSetMethod() ?? Property?.GetSetMethod(true))?.Invoke(Instance, [value]);
        public TPropertyType? Get()
        {
            var value = (Property?.GetGetMethod() ?? Property?.GetGetMethod(true))?.Invoke(Instance, []);
            if (value is TPropertyType fieldType) return fieldType;
            return default;
        }
    }

    public static RequiredProperty<TPropertyType> Property<TPropertyType>(TTargetType from, string name) => new(from, name);

}