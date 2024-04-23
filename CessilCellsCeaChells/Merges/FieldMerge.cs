using System;
using System.Linq;
using CessilCellsCeaChells.CeaChore;
using Mono.Cecil;

namespace CessilCellsCeaChells.Merges;

internal class FieldMerge : CessilMerge {
    private readonly string FieldName;
    private readonly TypeReference FieldType;

    public FieldMerge(CustomAttribute attribute)
    {
        TargetTypeRef = (TypeReference)attribute.ConstructorArguments[0].Value;
        FieldName = (string)attribute.ConstructorArguments[1].Value;
        FieldType = (TypeReference)attribute.ConstructorArguments[2].Value;
    }

    internal override CustomAttribute ConvertToAttribute(AssemblyDefinition into)
    {
        var typeTypeRef = GetOrImportTypeReference(into.MainModule, typeof(Type));
        var targetTypeRef = GetOrImportTypeReference(into.MainModule, TargetTypeRef);
        var fieldTypeTypeRef = GetOrImportTypeReference(into.MainModule, FieldType);
        return new CustomAttribute(into.MainModule.ImportReference(typeof(RequiresFieldAttribute).GetConstructors().First())) {
            ConstructorArguments = {
                new CustomAttributeArgument(typeTypeRef, targetTypeRef),
                new CustomAttributeArgument(into.MainModule.TypeSystem.String, FieldName),
                new CustomAttributeArgument(typeTypeRef, fieldTypeTypeRef)
            }
        };
    }

    internal override bool TryMergeInto(TypeDefinition typeDefinition, out IMemberDefinition? memberDefinition)
    {
        memberDefinition = default;
        var didCreate = CessilHelper.TryCreateField(typeDefinition, FieldName, GetOrImportTypeReference(typeDefinition.Module, FieldType), out var fieldDefinition);
        if (didCreate)
            memberDefinition = fieldDefinition;
        return didCreate;
    }
}