using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CessilCellsCeaChells.Internal;

internal static class CessilHelper {
    internal static bool TryCreateField(TypeDefinition typeDef, string name, TypeReference importedFieldType, out FieldDefinition? fieldDefinition)
    {
        fieldDefinition = default;
        if (typeDef.Fields.Any(field => field.Name == name)) return false;

        typeDef.Fields.Add(fieldDefinition = new FieldDefinition(name, FieldAttributes.Private, importedFieldType));
        return true;
    }
    
    internal static bool TryCreateProperty(TypeDefinition typeDef, string name, TypeReference importedPropType, out FieldDefinition? backingField, out PropertyDefinition? propertyDefinition, 
        string fieldNameTemplate = "<{0}>k__BackingField", string methodNameTemplate = "{0}_{1}")
    {
        propertyDefinition = default;
        backingField = default;
        if (typeDef.Properties.Any(prop => prop.Name == name)) return false;
        if (!TryCreateField(typeDef, string.Format(fieldNameTemplate, name), importedPropType, out backingField)) return false;
        
        if (!typeDef.Module.TryGetTypeReference(typeof(CompilerGeneratedAttribute).FullName, out var compilerGeneratedAttrRef))
            compilerGeneratedAttrRef = typeDef.Module.ImportReference(typeof(CompilerGeneratedAttribute));
        var compilerGeneratedConstructor = typeDef.Module.ImportReference(
            compilerGeneratedAttrRef.Resolve().Methods.First(method => method.Name == ".ctor"));
        
        backingField!.CustomAttributes.Add(new CustomAttribute(compilerGeneratedConstructor));
        
        var getter = new MethodDefinition(string.Format(methodNameTemplate, "get", name),
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            importedPropType);
        typeDef.Methods.Add(getter);
        getter.Body = new MethodBody(getter);
        getter.CustomAttributes.Add(new CustomAttribute(compilerGeneratedConstructor));
        var ilProcessor = getter.Body.GetILProcessor();
        ilProcessor.Emit(OpCodes.Ldarg_0);
        ilProcessor.Emit(OpCodes.Ldfld, backingField);
        ilProcessor.Emit(OpCodes.Ret);
        
        var setter = new MethodDefinition(string.Format(methodNameTemplate, "set", name),
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeDef.Module.Assembly.MainModule.TypeSystem.Void);
        typeDef.Methods.Add(setter);
        setter.Body = new MethodBody(setter);
        setter.CustomAttributes.Add(new CustomAttribute(compilerGeneratedConstructor));
        setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, importedPropType));
        ilProcessor = setter.Body.GetILProcessor();
        ilProcessor.Emit(OpCodes.Ldarg_0);
        ilProcessor.Emit(OpCodes.Ldarg_1);
        ilProcessor.Emit(OpCodes.Stfld, backingField);
        ilProcessor.Emit(OpCodes.Ret);
        
        typeDef.Properties.Add(propertyDefinition = new PropertyDefinition(name, PropertyAttributes.None, importedPropType) { GetMethod = getter, SetMethod = setter });
        return true;
    }

    internal static bool TryCreateMethod(TypeDefinition typeDef, string name, TypeReference importedReturnType, TypeReference[] importedParamReferences, CustomAttributeArgument[] constants, out MethodDefinition? methodDefinition)
    {
        methodDefinition = default;
        if (typeDef.Methods.Any(method => method.Name == name && ParameterTypesMatch(
                method.Parameters.Select(param => param.ParameterType).ToArray(),
                importedParamReferences))) return false;

        methodDefinition = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.HideBySig, importedReturnType);
        methodDefinition.Body.InitLocals = true;
        methodDefinition.Body.GetILProcessor().Emit(OpCodes.Ret);
        
        var paramTypeCounts = new Dictionary<string, int>();
        var paramTypeCounter = new Dictionary<string, int>();
        foreach (var argument in importedParamReferences)
        {
            var key = argument.IsGenericInstance ? argument.Name
                .Substring(0, argument.Name.IndexOf('`')) : argument.Name;
            if (!paramTypeCounter.ContainsKey(key))
                paramTypeCounter.Add(key, 0);
            if (!paramTypeCounts.ContainsKey(key))
                paramTypeCounts.Add(key, 0);
            paramTypeCounts[key]++;
        }
        for (var idx = 0; idx < importedParamReferences.Length; idx++)
        {
            var argument = importedParamReferences[idx];
            var key = argument.IsGenericInstance
                ? argument.Name.Substring(0, argument.Name.IndexOf('`'))
                : argument.Name;
            var count = paramTypeCounts[key];
            var index = paramTypeCounter[key]++;
            var paramName = "p" + key + (count > 1 ? "_" + index : "");

            var constantParamCountDiff = importedParamReferences.Length - constants.Length;
            var constantIdx = idx - constantParamCountDiff;
            var isOptional = constants.Length > 0 && idx >= constantParamCountDiff && constants[constantIdx].Type.FullName == argument.FullName;
            var newParam = new ParameterDefinition(paramName, isOptional ? ParameterAttributes.Optional : ParameterAttributes.None, argument);
            if (constantIdx >= 0 && constants.Length > constantIdx && constants[constantIdx].Type.FullName != argument.FullName)
                CessilCellsCeaChellsDownByTheCeaChore.Logger.LogWarning($"Ignoring parameter constant @ {idx + 1}, and all preceding parameters, for " +
                            $"'{typeDef.FullName}::{methodDefinition.Name}' because '{constants[constantIdx].Type.FullName}' is not the correct type! It should be '{argument.FullName}'.");
            if (isOptional)
                newParam.Constant = constants[constantIdx].Value;
            else
            {
                foreach (var soFarParam in methodDefinition.Parameters)
                {
                    soFarParam.Constant = null;
                    soFarParam.Attributes = ParameterAttributes.None;
                }
            }
            methodDefinition.Parameters.Add(newParam);
        }

        typeDef.Methods.Add(methodDefinition);
        return true;
    }

    private static bool ParameterTypesMatch(TypeReference[] parametersFrom, TypeReference[] parametersTo) =>
        parametersFrom.Select(typ => typ.FullName).SequenceEqual(parametersTo.Select(typ => typ.FullName));
}