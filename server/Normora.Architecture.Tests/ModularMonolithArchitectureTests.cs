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
    private static readonly Assembly WorkerAssembly = typeof(Normora.Worker.WorkerAssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Normora.Infrastructure.Extensions.ApplicationServiceExtensions).Assembly;

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

    [Fact]
    public void BusinessModules_MustNotReferenceEachOther()
    {
        var moduleAssemblies = new[] { TenantsAssembly, DocumentsAssembly, ConversationsAssembly };

        foreach (var module in moduleAssemblies)
        {
            var moduleReferences = module.GetReferencedAssemblies()
                .Where(reference => reference.Name?.StartsWith("Normora.Modules.", StringComparison.Ordinal) == true)
                .Select(reference => reference.Name)
                .ToList();

            Assert.Empty(moduleReferences);
        }

    }

    [Fact]
    public void DocumentsModule_MustNotReferenceHangfire()
    {
        var hangfireReferences = DocumentsAssembly.GetReferencedAssemblies()
            .Where(reference => reference.Name?.StartsWith("Hangfire", StringComparison.Ordinal) == true)
            .Select(reference => reference.Name)
            .ToList();

        Assert.Empty(hangfireReferences);
    }

    [Fact]
    public void BusinessModules_MustNotContainExternalProviderAdapters()
    {
        var providerAdapterTypes = new[] { ConversationsAssembly, DocumentsAssembly, TenantsAssembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type =>
                type.Name.StartsWith("Gemini", StringComparison.Ordinal) ||
                type.Name.StartsWith("Minio", StringComparison.Ordinal) ||
                string.Equals(type.Name, "TikaDocumentTextExtractor", StringComparison.Ordinal))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(providerAdapterTypes);
    }

    [Fact]
    public void ExternalProviderAdapters_MustBeOwnedByInfrastructure()
    {
        var providerAdapterTypes = new[]
        {
            typeof(Normora.Infrastructure.Providers.DocumentExtraction.TikaDocumentTextExtractor),
            typeof(Normora.Infrastructure.Providers.Gemini.GeminiEmbeddingService),
            typeof(Normora.Infrastructure.Providers.Gemini.GeminiTextGenerationService),
            typeof(Normora.Infrastructure.Providers.Gemini.GeminiQueryRewriterService),
            typeof(Normora.Infrastructure.Providers.Gemini.GeminiSuggestionsService),
            typeof(Normora.Infrastructure.Providers.Gemini.GeminiOptions),
            typeof(Normora.Infrastructure.Providers.Storage.MinioDocumentStorageService),
            typeof(Normora.Infrastructure.Providers.Storage.MinioBrandingStorageService),
            typeof(Normora.Infrastructure.Providers.Storage.MinioOptions),
            typeof(Normora.Infrastructure.Providers.DocumentExtraction.TikaOptions)
        };

        Assert.All(providerAdapterTypes, adapterType => Assert.Same(InfrastructureAssembly, adapterType.Assembly));
    }

    [Fact]
    public void Worker_MustNotReferenceApiHost()
    {
        Assert.DoesNotContain(
            WorkerAssembly.GetReferencedAssemblies(),
            reference => string.Equals(reference.Name, "Normora.Api", StringComparison.Ordinal));
    }

    [Fact]
    public void Infrastructure_MustNotReferenceExecutableHosts()
    {
        var hostReferences = InfrastructureAssembly.GetReferencedAssemblies()
            .Where(reference =>
                string.Equals(reference.Name, "Normora.Api", StringComparison.Ordinal) ||
                string.Equals(reference.Name, "Normora.Worker", StringComparison.Ordinal))
            .Select(reference => reference.Name)
            .ToList();

        Assert.Empty(hostReferences);
    }
}
