using System;
using CessilCellsCeaChells.Internal;
using JetBrains.Annotations;

namespace CessilCellsCeaChells.CeaChore;

public class RequiresFieldAttribute : RequiresAttribute {
    [UsedImplicitly] public RequiresFieldAttribute(Type targetType, string fieldName, Type fieldType, bool isPublic = false) {}
}

public class RequiresPropertyAttribute : RequiresAttribute {
    [UsedImplicitly] public RequiresPropertyAttribute(Type targetType, string propertyName, Type propertyType, bool singletonCreateOnAccess = false) {}
}

public class RequiresMethodAttribute : RequiresAttribute {
    [UsedImplicitly] public RequiresMethodAttribute(Type targetType, string methodName, Type returnType, params Type[] arguments) {}
}

public class RequiresMethodDefaultsAttribute : RequiresAttribute {
    [UsedImplicitly] public RequiresMethodDefaultsAttribute(Type targetType, string methodName, Type returnType, Type[] argumentTypes, object[] argumentDefaults) {}
}

public class RequiresEnumInsertionAttribute : RequiresAttribute {
    [UsedImplicitly] public RequiresEnumInsertionAttribute(Type targetType, string entryName) {}
}