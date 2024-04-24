using System;

namespace CessilCellsCeaChells.Internal;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public abstract class RequiresAttribute : Attribute;