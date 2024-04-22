using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CessilCellsCeaChells;

internal static class CessilMerger {
    internal static void PatchTargetAssembly(AssemblyDefinition assembly)
    {
        CessilCellsCeaChellsDownByTheCeaChore.Logger.LogInfo($"Patching '{assembly.Name.Name}'..");

        foreach (var merge in CessilCellsCeaChellsDownByTheCeaChore.Merges.Where(merge => merge.TargetAssemblyName == assembly.Name.Name + ".dll"))
        {
            var targetType = assembly.MainModule.GetType(merge.TargetTypeReference.FullName);
            if (targetType == null)
            {
                CessilCellsCeaChellsDownByTheCeaChore.Logger.LogWarning($"Disregarding merge as it's target type doesn't exist!");
                continue;
            }

            var resolvedAndImported = targetType.Module.ImportReference(merge.IdentifierType.Resolve());
            switch (merge.Type)
            {
                case Merge.MergeType.Field:
                    if (TryCreateField(targetType, merge.Identifer, resolvedAndImported, out var fieldDef))
                    {
                        CessilCellsCeaChellsDownByTheCeaChore.Logger.LogDebug($"Successfully merged field '{fieldDef?.FullName}'");
                    }
                    break;
                case Merge.MergeType.Property:
                    if (TryCreateProperty(targetType, merge.Identifer, resolvedAndImported, out var propDef))
                    {
                        CessilCellsCeaChellsDownByTheCeaChore.Logger.LogDebug($"Successfully merged property '{propDef?.FullName}'");
                    }
                    break;
                case Merge.MergeType.Method:
                    var paramsResolvedAndImported = merge.AdditionalTypeParameters.Select(param => targetType.Module.ImportReference(param.Resolve())).ToArray();
                    if (TryCreateMethod(targetType, merge.Identifer, resolvedAndImported, paramsResolvedAndImported, out var methodDef))
                    {
                        CessilCellsCeaChellsDownByTheCeaChore.Logger.LogDebug($"Successfully merged method '{methodDef?.FullName}'");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        if (!Directory.Exists(Paths.CachePath)) Directory.CreateDirectory(Paths.CachePath);

        var outputPath = Path.Combine(Paths.CachePath, $"Cessil." + assembly.Name.Name + ".dll");
        assembly.Write(outputPath);

        CessilCellsCeaChellsDownByTheCeaChore.Logger.LogInfo($"Patching '{assembly.Name.Name}' done! Cached to '{outputPath.Replace(Paths.GameRootPath, ".")}'");
    }

    private static bool TryCreateField(TypeDefinition typeDef, string name, TypeReference importedFieldType, out FieldDefinition? fieldDefinition)
    {
        fieldDefinition = default;
        if (typeDef.Fields.Any(field => field.Name == name)) return false;

        typeDef.Fields.Add(fieldDefinition = new FieldDefinition(name, FieldAttributes.Private, importedFieldType));
        return true;
    }

    private static bool TryCreateProperty(TypeDefinition typeDef, string name, TypeReference importedPropType, out PropertyDefinition? propertyDefinition)
    {
        propertyDefinition = default;
        if (typeDef.Properties.Any(prop => prop.Name == name)) return false;
        if (!TryCreateField(typeDef, $"<{name}>k__BackingField", importedPropType, out var fieldDef)) return false;
        
        var getter = new MethodDefinition("get_" + name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            importedPropType);
        typeDef.Methods.Add(getter);
        getter.Body = new MethodBody(getter);
        var ilProcessor = getter.Body.GetILProcessor();
        ilProcessor.Emit(OpCodes.Ldarg_0);
        ilProcessor.Emit(OpCodes.Ldfld, fieldDef);
        ilProcessor.Emit(OpCodes.Ret);
        
        var setter = new MethodDefinition("set_" + name,
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
            typeDef.Module.Assembly.MainModule.TypeSystem.Void);
        typeDef.Methods.Add(setter);
        setter.Body = new MethodBody(setter);
        setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, importedPropType));
        ilProcessor = setter.Body.GetILProcessor();
        ilProcessor.Emit(OpCodes.Ldarg_0);
        ilProcessor.Emit(OpCodes.Ldarg_1);
        ilProcessor.Emit(OpCodes.Stfld, fieldDef);
        ilProcessor.Emit(OpCodes.Ret);
        
        typeDef.Properties.Add(propertyDefinition = new PropertyDefinition(name, PropertyAttributes.None, importedPropType) { GetMethod = getter, SetMethod = setter });
        return true;
    }

    private static bool TryCreateMethod(TypeDefinition typeDef, string name, TypeReference importedReturnType, TypeReference[] importedParamReferences, out MethodDefinition? methodDefinition)
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
        foreach (var argument in importedParamReferences)
        {
            var key = argument.IsGenericInstance
                ? argument.Name.Substring(0, argument.Name.IndexOf('`'))
                : argument.Name;
            var count = paramTypeCounts[key];
            var index = paramTypeCounter[key]++;
            var paramName = "p" + key + (count > 1 ? "_" + index : "");
            methodDefinition.Parameters.Add(new ParameterDefinition(paramName, ParameterAttributes.None, argument));
        }

        typeDef.Methods.Add(methodDefinition);
        return true;
    }

    private static bool ParameterTypesMatch(TypeReference[] parametersFrom, TypeReference[] parametersTo) =>
        parametersFrom.Select(typ => typ.FullName).SequenceEqual(parametersTo.Select(typ => typ.FullName));
}