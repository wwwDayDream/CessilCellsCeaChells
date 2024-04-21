using System.Linq;
using CessilCellsCeaChells.CeaChore;
using Mono.Cecil;

namespace CessilCellsCeaChells;

internal class Merge {
    internal enum MergeType { Field, Property, Method }
    internal TypeReference TargetTypeReference;
    internal string TargetAssemblyName => TargetTypeReference.Resolve().Module.Assembly.Name.Name + ".dll";
    
    internal string Identifer;
    internal TypeReference IdType;
    internal MergeType Type;

    internal TypeReference[] AdditionalTypeParameters = [];

    internal Merge(CustomAttribute attribute)
    {
        TargetTypeReference = (TypeReference)attribute.ConstructorArguments[0].Value;
        Identifer = (string)attribute.ConstructorArguments[1].Value;
        IdType = (TypeReference)attribute.ConstructorArguments[2].Value;
        
        if (attribute.AttributeType.FullName == typeof(RequiresFieldAttribute).FullName)
        {
            Type = MergeType.Field;
        } else if (attribute.AttributeType.FullName == typeof(RequiresPropertyAttribute).FullName)
        {
            Type = MergeType.Property;
        } else if (attribute.AttributeType.FullName == typeof(RequiresMethodAttribute).FullName)
        {
            Type = MergeType.Method;
            AdditionalTypeParameters = ((CustomAttributeArgument[])attribute.ConstructorArguments[3].Value)
                .Select(attr => (TypeReference)attr.Value).ToArray();
        }
    }
    
    internal static bool TryCreateMerges(AssemblyDefinition assemblyDefinition, out Merge[] merges)
    {
        merges = assemblyDefinition.CustomAttributes
            .Where(attr =>
                attr.AttributeType.FullName == typeof(RequiresFieldAttribute).FullName ||
                attr.AttributeType.FullName == typeof(RequiresPropertyAttribute).FullName ||
                attr.AttributeType.FullName == typeof(RequiresMethodAttribute).FullName)
            .Select(attr => new Merge(attr)).ToArray();
        
        return merges.Length > 0;
    }
}