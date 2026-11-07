using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Normora.Architecture.Tests;

public sealed class ModularMonolithArchitectureTests
{
    private static readonly Assembly ConversationsAssembly = typeof(Normora.Modules.Conversations.Domain.Conversation).Assembly;
    private static readonly Assembly DocumentsAssembly = typeof(Normora.Modules.Documents.Domain.Document).Assembly;
    private static readonly Assembly TenantsAssembly = typeof(Normora.Modules.Tenants.Domain.Tenant).Assembly;

    [Fact]
    public void EntityConfigurations_MustHaveParameterlessConstructors()
    {
        var assemblies = new[] { ConversationsAssembly, DocumentsAssembly, TenantsAssembly };

        var configTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
            .ToList();

        Assert.NotEmpty(configTypes);

        foreach (var configType in configTypes)
        {
            var defaultCtor = configType.GetConstructor(Type.EmptyTypes);
            Assert.True(
                defaultCtor != null,
                $"Entity configuration '{configType.FullName}' must have a parameterless constructor to allow auto-discovery via ApplyConfigurationsFromAssembly.");
        }
    }

    [Fact]
    public void Controllers_MustNeverInject_DbContexts()
    {
        var assemblies = new[] { ConversationsAssembly, DocumentsAssembly, TenantsAssembly };

        var controllerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .ToList();

        Assert.NotEmpty(controllerTypes);

        foreach (var controller in controllerTypes)
        {
            var constructors = controller.GetConstructors();
            foreach (var ctor in constructors)
            {
                foreach (var param in ctor.GetParameters())
                {
                    Assert.False(
                        typeof(DbContext).IsAssignableFrom(param.ParameterType),
                        $"Controller '{controller.FullName}' must not inject DbContext '{param.ParameterType.Name}'. Controllers must use MediatR contracts or module service abstractions.");
                }
            }
        }
    }

    [Fact]
    public void DocumentsModule_MustNotReference_ConversationsInternals()
    {
        var docTypes = DocumentsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in docTypes)
        {
            foreach (var ctor in type.GetConstructors())
            {
                foreach (var param in ctor.GetParameters())
                {
                    Assert.False(
                        param.ParameterType.Namespace?.StartsWith("Normora.Modules.Conversations") == true,
                        $"Documents type '{type.FullName}' directly references Conversations type '{param.ParameterType.FullName}'. Cross-module communication must use Normora.Shared.Contracts.");
                }
            }
        }
    }

    [Fact]
    public void ConversationsModule_MustNotReference_DocumentsInternals()
    {
        var convTypes = ConversationsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var type in convTypes)
        {
            foreach (var ctor in type.GetConstructors())
            {
                foreach (var param in ctor.GetParameters())
                {
                    Assert.False(
                        param.ParameterType.Namespace?.StartsWith("Normora.Modules.Documents") == true,
                        $"Conversations type '{type.FullName}' directly references Documents type '{param.ParameterType.FullName}'. Cross-module communication must use Normora.Shared.Contracts.");
                }
            }
        }
    }
}

