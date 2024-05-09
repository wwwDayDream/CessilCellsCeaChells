using System;
using System.Collections.Generic;
using System.Linq;
using CessilCellsCeaChells.CeaChore;
using Mono.Cecil;

namespace CessilCellsCeaChells.Merges;

internal abstract class CessilMerge(TypeReference targetTypeRef) {
    internal string TargetAssemblyName => TargetTypeRef.Resolve().Module.Assembly.Name.Name + ".dll";
    internal readonly TypeReference TargetTypeRef = targetTypeRef;

    private static readonly Dictionary<string, Type> CessilMergeTypes = new Dictionary<string, Type>() {
        { typeof(RequiresFieldAttribute).FullName, typeof(FieldMerge) },
        { typeof(RequiresPropertyAttribute).FullName, typeof(PropertyMerge) },
        { typeof(RequiresMethodAttribute).FullName, typeof(MethodMerge) },
        { typeof(RequiresMethodDefaultsAttribute).FullName, typeof(MethodMerge) },
        { typeof(RequiresEnumInsertionAttribute).FullName, typeof(EnumInsertionMerge)}
    };
    internal static bool TryCreateMerges(AssemblyDefinition assembly, out CessilMerge[] merges)
    {
        merges = assembly.CustomAttributes
            .Select(customAttribute => new KeyValuePair<bool, CessilMerge?>(TryConvertFrom(customAttribute, out var merge), merge))
            .Where(tuple => tuple.Key)
            .Select(tuple => tuple.Value!).ToArray();
        return merges.Length > 0;
    }

    private static bool TryConvertFrom(CustomAttribute attribute, out CessilMerge? merge)
    {
        merge = default;
        if (!CessilMergeTypes.TryGetValue(attribute.AttributeType.FullName, out var mergeType)) return false;
        
        merge = (CessilMerge)Activator.CreateInstance(mergeType, attribute);
        return merge != null;
    }
    internal static TypeReference GetOrImportTypeReference(ModuleDefinition into, Type type)
    {
        if (!into.TryGetTypeReference(type.FullName, out var typeRef))
            typeRef = into.ImportReference(type);
        return typeRef;
    }
    internal static TypeReference GetOrImportTypeReference(ModuleDefinition into, TypeReference type)
    {
        if (!into.TryGetTypeReference(type.FullName, out var typeRef))
            typeRef = into.ImportReference(type);
        return typeRef;
    }

    internal abstract CustomAttribute ConvertToAttribute(AssemblyDefinition into);
    internal abstract bool TryMergeInto(TypeDefinition typeDefinition, out IMemberDefinition? memberDefinition);
}