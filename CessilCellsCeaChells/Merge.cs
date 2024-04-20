using System.Linq;
using CessilCellsCeaChells.CeaChore;
using Mono.Cecil;

namespace CessilCellsCeaChells;

public class Merge {
    public enum MergeType { Field, Property, Method }
    public TypeReference TargetTypeReference;
    public string TargetAssemblyName => TargetTypeReference.Resolve().Module.Assembly.Name.Name + ".dll";
    
    public string Identifer;
    public TypeReference IdType;
    public MergeType Type;

    public TypeReference[] AdditionalTypeParameters = [];

    public Merge(CustomAttribute attribute)
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
    
    public static bool TryCreateMerges(AssemblyDefinition assemblyDefinition, out Merge[] merges)
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