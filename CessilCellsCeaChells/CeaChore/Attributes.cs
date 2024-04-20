using System;
using Mono.Cecil;

namespace CessilCellsCeaChells.CeaChore;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresFieldAttribute(Type targetType, string fieldName, Type fieldType) : Attribute;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresPropertyAttribute(Type targetType, string propertyName, Type propertyType) : Attribute;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RequiresMethodAttribute(Type targetType, string methodName, Type returnType, params Type[] arguments) : Attribute;