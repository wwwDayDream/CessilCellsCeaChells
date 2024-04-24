using System;
using System.Linq;
using CessilCellsCeaChells.CeaChore;
using CessilCellsCeaChells.Internal;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace CessilCellsCeaChells.Merges;

internal class MethodMerge : CessilMerge {
    private readonly string MethodName;
    private readonly TypeReference MethodType;
    private readonly TypeReference[] MethodParameters;
    private readonly object[] MethodDefaults;

    public MethodMerge(CustomAttribute attribute)
    {
        TargetTypeRef = (TypeReference)attribute.ConstructorArguments[0].Value;
        MethodName = (string)attribute.ConstructorArguments[1].Value;
        MethodType = (TypeReference)attribute.ConstructorArguments[2].Value;
        MethodParameters = ((CustomAttributeArgument[])attribute.ConstructorArguments[3].Value)
            .Select(attr => (TypeReference)attr.Value).ToArray();
        MethodDefaults = [ ];
        if (attribute.ConstructorArguments.Count == 4) return;
        MethodDefaults = ((CustomAttributeArgument[])attribute.ConstructorArguments[4].Value)
            .Select(attr => attr.Value).ToArray();
    }

    internal override CustomAttribute ConvertToAttribute(AssemblyDefinition into)
    {
        var typeTypeRef = GetOrImportTypeReference(into.MainModule, typeof(Type));
        var targetTypeRef = GetOrImportTypeReference(into.MainModule, TargetTypeRef);
        var fieldTypeTypeRef = GetOrImportTypeReference(into.MainModule, MethodType);

        var requiresAttribute = MethodDefaults.Length > 0 ? typeof(RequiresMethodDefaultsAttribute) : typeof(RequiresMethodAttribute);
        var attr = new CustomAttribute(into.MainModule.ImportReference(requiresAttribute.GetConstructors().First())) {
            ConstructorArguments = {
                new CustomAttributeArgument(typeTypeRef, targetTypeRef),
                new CustomAttributeArgument(into.MainModule.TypeSystem.String, MethodName),
                new CustomAttributeArgument(typeTypeRef, fieldTypeTypeRef),
                new CustomAttributeArgument(typeTypeRef.MakeArrayType(), MethodParameters.Select(tRef => 
                    new CustomAttributeArgument(typeTypeRef, GetOrImportTypeReference(into.MainModule, tRef))).ToArray())
            }
        };
        if (MethodDefaults.Length > 0)
            attr.ConstructorArguments.Add(new CustomAttributeArgument(into.MainModule.TypeSystem.Object.MakeArrayType(), MethodDefaults.Select(tObj => 
                new CustomAttributeArgument(into.MainModule.TypeSystem.Object, tObj)).ToArray()));
        return attr;
    }

    internal override bool TryMergeInto(TypeDefinition typeDefinition, out IMemberDefinition? memberDefinition)
    {
        memberDefinition = default;
        var paramsResolvedAndImported = MethodParameters.Select(param => typeDefinition.Module.ImportReference(param.Resolve())).ToArray();
        var didCreate = CessilHelper.TryCreateMethod(typeDefinition, MethodName, GetOrImportTypeReference(typeDefinition.Module, MethodType),
            paramsResolvedAndImported, MethodDefaults.Select(obj => (CustomAttributeArgument)obj).ToArray(), out var MethodDefinition);
        if (didCreate)
            memberDefinition = MethodDefinition;
        return didCreate;
    }
}