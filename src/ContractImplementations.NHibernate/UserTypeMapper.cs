using System;
using System.Collections.Generic;
using NHibernate.Type;
using NHibernate.UserTypes;

namespace IOKode.OpinionatedFramework.ContractImplementations.NHibernate;

public struct UserTypeMap
{
    public Type ApplicationType { get; init; }
    public Type NhUserType { get; init; }
}

public static class UserTypeMapper
{
    private static readonly List<UserTypeMap> maps = new();

    public static IReadOnlyList<UserTypeMap> Maps => maps;

    public static void Add(Type applicationType, Type nhUserType)
    {
        maps.Add(new UserTypeMap
        {
            ApplicationType = applicationType,
            NhUserType = nhUserType
        });
    }

    public static void AddType<TApplicationType, TNhUserType>() where TNhUserType : IType
    {
        Add(typeof(TApplicationType), typeof(TNhUserType));
    }

    public static void AddUserType<TApplicationType, TNhUserType>() where TNhUserType : IUserType
    {
        Add(typeof(TApplicationType), typeof(TNhUserType));
    }

    public static void AddUserCollectionType<TApplicationType, TNhUserType>() where TNhUserType : IUserCollectionType
    {
        Add(typeof(TApplicationType), typeof(TNhUserType));
    }

    public static Type? GetNhUserType(Type applicationType)
    {
        return maps.Find(x => x.ApplicationType == applicationType).NhUserType;
    }
}