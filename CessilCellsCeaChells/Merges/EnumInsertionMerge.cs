using System;
using System.Linq;
using CessilCellsCeaChells.CeaChore;
using CessilCellsCeaChells.Internal;
using Mono.Cecil;

namespace CessilCellsCeaChells.Merges;

internal class EnumInsertionMerge(CustomAttribute attribute) : CessilMerge((TypeReference)attribute.ConstructorArguments[0].Value) {
    private readonly string EnumEntryName =
        (string)attribute.ConstructorArguments[1].Value;

    internal override CustomAttribute ConvertToAttribute(AssemblyDefinition into)
    {
        var typeTypeRef = GetOrImportTypeReference(into.MainModule, typeof(Type));
        var targetTypeRef = GetOrImportTypeReference(into.MainModule, TargetTypeRef);
        return new CustomAttribute(into.MainModule.ImportReference(typeof(RequiresEnumInsertionAttribute).GetConstructors().First())) {
            ConstructorArguments = {
                new CustomAttributeArgument(typeTypeRef, targetTypeRef),
                new CustomAttributeArgument(into.MainModule.TypeSystem.String, EnumEntryName),
            }
        };
    }

    internal override bool TryMergeInto(TypeDefinition typeDefinition, out IMemberDefinition? memberDefinition)
    {
        memberDefinition = default;
        var didCreate = CessilHelper.TryCreateField(typeDefinition, EnumEntryName, typeDefinition, out var fieldDefinition,  
            FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.Public | FieldAttributes.HasDefault);
        long curHighest = -1;
        foreach (var typeDefinitionField in typeDefinition.Fields.Where(field => field.Name != "value__"))
        {
            if (Convert.ToInt64(typeDefinitionField.Constant) > Convert.ToInt64(curHighest))
                curHighest = Convert.ToInt64(typeDefinitionField.Constant);
        }
        
        if (didCreate)
            fieldDefinition!.Constant = Convert.ChangeType(
                curHighest + 1, Type.GetType(typeDefinition.Fields.First(field => field.Name == "value__").FieldType.FullName) ?? 
                                throw new InvalidOperationException());
        
        if (didCreate)
            memberDefinition = fieldDefinition;
        return didCreate;
    }
}