using System;
using System.Reflection;

namespace DesperateDevs.Reflection
{
    public sealed class PublicMemberInfo
    {
        readonly FieldInfo _fieldInfo;
        readonly PropertyInfo _propertyInfo;

        public PublicMemberInfo(FieldInfo fieldInfo)
        {
            _fieldInfo = fieldInfo;
            Name = fieldInfo.Name;
            Type = fieldInfo.FieldType;
        }

        public PublicMemberInfo(PropertyInfo propertyInfo)
        {
            _propertyInfo = propertyInfo;
            Name = propertyInfo.Name;
            Type = propertyInfo.PropertyType;
        }

        public string Name { get; }
        public Type Type { get; }

        public object GetValue(object target) =>
            _fieldInfo != null
                ? _fieldInfo.GetValue(target)
                : _propertyInfo.GetValue(target, null);

        public void SetValue(object target, object value)
        {
            if (_fieldInfo != null)
                _fieldInfo.SetValue(target, value);
            else
                _propertyInfo.SetValue(target, value, null);
        }
    }
}
