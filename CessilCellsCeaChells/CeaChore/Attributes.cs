using System;
using CessilCellsCeaChells.Internal;

namespace CessilCellsCeaChells.CeaChore;

public class RequiresFieldAttribute(Type targetType, string fieldName, Type fieldType) : RequiresAttribute;

public class RequiresPropertyAttribute(Type targetType, string propertyName, Type propertyType, bool singletonCreateOnAccess = false) : RequiresAttribute;

public class RequiresMethodAttribute(Type targetType, string methodName, Type returnType, params Type[] arguments) : RequiresAttribute;

public class RequiresMethodDefaultsAttribute(Type targetType, string methodName, Type returnType, Type[] argumentTypes, object[] argumentDefaults) : RequiresAttribute;

[Flags]
public enum FieldChange { FieldType = 1 }
[Flags]
public enum PropertyChange { PropertyType = 1, AddSetter = 2, AddGetter = 4, AddSingletonGetter = 8 }

public class RequiresFieldChangeAttribute(Type targetType, string fieldName, FieldChange modificationType, Type? newType = null) : RequiresAttribute;

public class RequiresPropertyChangeAttribute(Type targetType, string propertyName, PropertyChange modificationType, Type? newType = null) : RequiresAttribute;