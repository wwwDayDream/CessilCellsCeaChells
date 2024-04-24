using System;
using System.Linq;
using CessilCellsCeaChells.CeaChore;
using CessilCellsCeaChells.Internal;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace CessilCellsCeaChells.Merges;

internal class PropertyMerge : CessilMerge {
    private readonly string PropertyName;
    private readonly TypeReference PropertyType;
    private readonly bool InitializeOnAccess;

    public PropertyMerge(CustomAttribute attribute)
    {
        TargetTypeRef = (TypeReference)attribute.ConstructorArguments[0].Value;
        PropertyName = (string)attribute.ConstructorArguments[1].Value;
        PropertyType = (TypeReference)attribute.ConstructorArguments[2].Value;
        InitializeOnAccess = (bool)attribute.ConstructorArguments[3].Value;
    }

    internal override CustomAttribute ConvertToAttribute(AssemblyDefinition into)
    {
        var typeTypeRef = GetOrImportTypeReference(into.MainModule, typeof(Type));
        var targetTypeRef = GetOrImportTypeReference(into.MainModule, TargetTypeRef);
        var fieldTypeTypeRef = GetOrImportTypeReference(into.MainModule, PropertyType);
        return new CustomAttribute(into.MainModule.ImportReference(typeof(RequiresPropertyAttribute).GetConstructors().First())) {
            ConstructorArguments = {
                new CustomAttributeArgument(typeTypeRef, targetTypeRef),
                new CustomAttributeArgument(into.MainModule.TypeSystem.String, PropertyName),
                new CustomAttributeArgument(typeTypeRef, fieldTypeTypeRef),
                new CustomAttributeArgument(into.MainModule.TypeSystem.Boolean, InitializeOnAccess)
            }
        };
    }

    internal override bool TryMergeInto(TypeDefinition typeDefinition, out IMemberDefinition? memberDefinition)
    {
        memberDefinition = default;
        var importedPropertyType = GetOrImportTypeReference(typeDefinition.Module, PropertyType);
        if (!CessilHelper.TryCreateProperty(typeDefinition, PropertyName, importedPropertyType, out var fieldDef, out var propertyDefinition)) return false;
        if (InitializeOnAccess)
            AddSingletonCheckToProperty(fieldDef!, PropertyType.Resolve(), propertyDefinition!);
        memberDefinition = propertyDefinition;
        return true;
    }
    
     private static void AddSingletonCheckToProperty(FieldDefinition backingField, TypeDefinition resolvedAndImported, PropertyDefinition singletonPropDef)
     {
         var newMethod = resolvedAndImported.Methods.FirstOrDefault(method => method.Name == ".ctor" && method.Parameters.Count == 0);
         if (newMethod == null) return;
         var newMethodImported = backingField.Module.ImportReference(newMethod);

         var getterMethod = singletonPropDef.GetMethod;
         getterMethod.Body.Instructions.Clear();
         
         var ilProcessor = getterMethod.Body.GetILProcessor();
         ilProcessor.Emit(OpCodes.Ldarg_0);
         ilProcessor.Emit(OpCodes.Ldfld, backingField);
         ilProcessor.Emit(OpCodes.Ldnull);
         ilProcessor.Emit(OpCodes.Ceq);
         var continuePoint = ilProcessor.Create(OpCodes.Nop);
         ilProcessor.Emit(OpCodes.Brfalse, continuePoint);
         // Set it if it's null
         ilProcessor.Emit(OpCodes.Ldarg_0);
         ilProcessor.Emit(OpCodes.Newobj, newMethodImported);
         ilProcessor.Emit(OpCodes.Call, singletonPropDef.SetMethod);
         ilProcessor.Append(continuePoint);
         
         getterMethod.Body.OptimizeMacros();
         
         ilProcessor.Emit(OpCodes.Ldarg_0);
         ilProcessor.Emit(OpCodes.Ldfld, backingField);
         ilProcessor.Emit(OpCodes.Ret);
     }
}