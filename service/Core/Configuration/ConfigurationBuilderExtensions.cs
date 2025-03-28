﻿// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;

#pragma warning disable IDE0130 // reduce number of "using" statements
// ReSharper disable once CheckNamespace - reduce number of "using" statements
namespace Microsoft.KernelMemory;

public static partial class ConfigurationBuilderExtensions
{
    // ASP.NET env var
    private const string AspNetCoreEnvVar = "ASPNETCORE_ENVIRONMENT";

    // .NET env var
    private const string DotNetEnvVar = "DOTNET_ENVIRONMENT";

    public static void AddKernelMemoryConfigurationSources(
        this IConfigurationBuilder builder,
        bool useAppSettingsFiles = true,
        bool useEnvVars = true,
        bool useSecretManager = true,
        string? settingsDirectory = null)
    {
        // ASPNETCORE_ENVIRONMENT env var takes precedence. Env should be either Development or Production.
        var env = Environment.GetEnvironmentVariable(AspNetCoreEnvVar) ?? Environment.GetEnvironmentVariable(DotNetEnvVar) ?? string.Empty;

        // Detect the folder containing configuration files
        settingsDirectory ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? Directory.GetCurrentDirectory();
        builder.SetBasePath(settingsDirectory);

        // Add configuration files as sources
        if (useAppSettingsFiles)
        {
            // Add appsettings.json, typically used for default settings, without credentials
            var main = Path.Join(settingsDirectory, "appsettings.json");
            if (!File.Exists(main))
            {
                throw new ConfigurationException($"appsettings.json not found. Directory: {settingsDirectory}");
            }

            builder.AddJsonFile(main, optional: false);

            // Add appsettings.Development.json, used for local overrides and credentials
            if (env.Equals("development", StringComparison.OrdinalIgnoreCase))
            {
                var f1 = Path.Join(settingsDirectory, "appsettings.development.json");
                var f2 = Path.Join(settingsDirectory, "appsettings.Development.json");
                if (File.Exists(f1))
                {
                    builder.AddJsonFile(f1, optional: false);
                }
                else if (File.Exists(f2))
                {
                    builder.AddJsonFile(f2, optional: false);
                }
            }

            // Add appsettings.production.json, used for production settings and credentials
            if (env.Equals("production", StringComparison.OrdinalIgnoreCase))
            {
                var f1 = Path.Join(settingsDirectory, "appsettings.production.json");
                var f2 = Path.Join(settingsDirectory, "appsettings.Production.json");
                if (File.Exists(f1))
                {
                    builder.AddJsonFile(f1, optional: false);
                }
                else if (File.Exists(f2))
                {
                    builder.AddJsonFile(f2, optional: false);
                }
            }
        }

        // Add Secret Manager as source
        if (useSecretManager)
        {
            // GetEntryAssembly method can return null if the library is loaded
            // from an unmanaged application, in which case UserSecrets are not supported.
            var entryAssembly = Assembly.GetEntryAssembly();

            // Support for user secrets. Secret Manager doesn't encrypt the stored secrets and
            // shouldn't be treated as a trusted store. It's for development purposes only.
            // see: https://learn.microsoft.com/aspnet/core/security/app-secrets?#secret-manager
            if (entryAssembly != null && env.Equals("development", StringComparison.OrdinalIgnoreCase))
            {
                builder.AddUserSecrets(entryAssembly, optional: true);
            }
        }

        // Add environment variables as source.
        // Environment variables can override all the settings provided by the previous sources.
        if (useEnvVars)
        {
            // Support for environment variables overriding the config files
            builder.AddEnvironmentVariables();
        }
    }
}
