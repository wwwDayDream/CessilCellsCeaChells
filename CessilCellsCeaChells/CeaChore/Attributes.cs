using System;

namespace CessilCellsCeaChells.CeaChore;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresFieldAttribute(Type targetType, string fieldName, Type fieldType) : Attribute;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresPropertyAttribute(Type targetType, string propertyName, Type propertyType, bool singletonCreateOnAccess = false) : Attribute;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresMethodAttribute(Type targetType, string methodName, Type returnType, params Type[] arguments) : Attribute;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresMethodDefaultsAttribute(Type targetType, string methodName, Type returnType, Type[] argumentTypes, object[] argumentDefaults) : Attribute;