using System;
using System.Collections.Generic;
using System.Linq;
using CessilCellsCeaChells.CeaChore;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using MonoMod.Utils;

namespace CessilCellsCeaChells;

internal class Merge {
    internal enum MergeType { Field, Property, Method }
    internal TypeReference TargetTypeReference;
    internal string TargetAssemblyName => TargetTypeReference.Resolve().Module.Assembly.Name.Name + ".dll";
    
    internal string Identifer;
    internal TypeReference IdentifierType;
    internal MergeType Type;

    internal TypeReference[] AdditionalTypeParameters = [];

    internal Merge(CustomAttribute attribute)
    {
        TargetTypeReference = (TypeReference)attribute.ConstructorArguments[0].Value;
        Identifer = (string)attribute.ConstructorArguments[1].Value;
        IdentifierType = (TypeReference)attribute.ConstructorArguments[2].Value;
        AdditionalTypeParameters = [ ];
        
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

    public static TypeReference GetOrImportTypeReference(AssemblyDefinition into, Type type)
    {
        if (!into.MainModule.TryGetTypeReference(type.FullName, out var typeRef))
            typeRef = into.MainModule.ImportReference(type);
        return typeRef;
    }
    public static TypeReference GetOrImportTypeReference(AssemblyDefinition into, TypeReference type)
    {
        if (!into.MainModule.TryGetTypeReference(type.FullName, out var typeRef))
            typeRef = into.MainModule.ImportReference(type);
        return typeRef;
    }
    
    public CustomAttribute ConvertToAttribute( AssemblyDefinition into)
    {
        MethodReference attributeConstructor;
        var typeTypeRef = GetOrImportTypeReference(into, typeof(Type));
        var targetTypeRef = GetOrImportTypeReference(into, TargetTypeReference);
        var identifierTypeRef = GetOrImportTypeReference(into, IdentifierType);
        
        List<CustomAttributeArgument> constructorArguments = [ 
            new CustomAttributeArgument(typeTypeRef, targetTypeRef),
            new CustomAttributeArgument(into.MainModule.TypeSystem.String, Identifer),
            new CustomAttributeArgument(typeTypeRef, identifierTypeRef)
        ];
        switch (Type)
        {
            case MergeType.Field:
                GetOrImportTypeReference(into, typeof(RequiresFieldAttribute));
                var methodRef = into.MainModule.ImportReference(typeof(RequiresFieldAttribute).GetConstructors().First());
                attributeConstructor = methodRef!;
                break;
            case MergeType.Property:
                GetOrImportTypeReference(into, typeof(RequiresPropertyAttribute));
                methodRef = into.MainModule.ImportReference(typeof(RequiresPropertyAttribute).GetConstructors().First());
                attributeConstructor = methodRef!;
                break;
            case MergeType.Method:
                GetOrImportTypeReference(into, typeof(RequiresMethodAttribute));
                methodRef = into.MainModule.ImportReference(typeof(RequiresMethodAttribute).GetConstructors().First());
                attributeConstructor = methodRef!;
                constructorArguments.Add(new CustomAttributeArgument(typeTypeRef.MakeArrayType(), AdditionalTypeParameters.Select(tRef => 
                        new CustomAttributeArgument(typeTypeRef, GetOrImportTypeReference(into, tRef))).ToArray()));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        var customAttr = new CustomAttribute(attributeConstructor);
        customAttr.ConstructorArguments.AddRange(constructorArguments);
        return customAttr;
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