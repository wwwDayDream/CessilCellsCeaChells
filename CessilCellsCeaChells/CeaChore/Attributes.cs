using System;
using CessilCellsCeaChells.Internal;

namespace CessilCellsCeaChells.CeaChore;

public class RequiresFieldAttribute(Type targetType, string fieldName, Type fieldType) : RequiresAttribute;

public class RequiresPropertyAttribute(Type targetType, string propertyName, Type propertyType, bool singletonCreateOnAccess = false) : RequiresAttribute;

public class RequiresMethodAttribute(Type targetType, string methodName, Type returnType, params Type[] arguments) : RequiresAttribute;

public class RequiresMethodDefaultsAttribute(Type targetType, string methodName, Type returnType, Type[] argumentTypes, object[] argumentDefaults) : RequiresAttribute;