using Clean.Architecture.Mediator.API.Middlewares;
using Clean.Architecture.Mediator.Application;
using Clean.Architecture.Mediator.Data;
using Clean.Architecture.Mediator.Shared.Configuration;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Clean.Architecture.Mediator.API {
    public static class DependencyInjection {
        /// <summary>
        /// Configure <see cref="ConfigurationManager"/>
        /// </summary>
        /// <param name="config"></param>
        public static void ConfigureConfiguration(this ConfigurationManager config) {
            config.AddEnvironmentVariables();
        }

        /// <summary>
        /// Configure <see cref="ConfigureHostBuilder"/>
        /// </summary>
        /// <param name="host"></param>
        /// <param name="config"></param>
        public static void ConfigureHost(this ConfigurationManager config, ConfigureHostBuilder host) {
            var seqConfig = config.GetSection(nameof(Seq));

            if (!seqConfig.GetValue<bool>(nameof(Seq.Enabled))) {
                host.UseSerilog((_, conf) => conf
                    .MinimumLevel.Debug()
                    .WriteTo.Async(write => write.Console()));
            } else {
                host.UseSerilog((_, conf) => conf
                    .Enrich.WithProperty("AppSource", seqConfig.GetValue<string>(nameof(Seq.ApplicationName)))
                    .WriteTo.Async(write => write.Console())
                    .WriteTo.Async(write => write
                        .Seq(seqConfig.GetValue<string>(nameof(Seq.Uri))!,
                            apiKey: seqConfig.GetValue<string>(nameof(Seq.ApiKey)))
                        .MinimumLevel.Debug()));
            }
        }

        /// <summary>
        /// Configure <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="services"></param>
        /// <param name="config"></param>
        public static void ConfigureServices(this ConfigurationManager config, IServiceCollection services) {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddHttpContextAccessor();
            services.AddOpenTelemetry(config);

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MediatRAnchor).Assembly));
            services.AddValidatorsFromAssembly(typeof(MediatRAnchor).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(FluentValidationBehaviour<,>));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("Mediator")!));

            services.AddCors(cors =>
                cors.AddPolicy(nameof(Cors.DefaultCorsPolicy),
                    policy => policy.WithOrigins(config.GetSection(nameof(Cors))
                        .GetSection(nameof(Cors.AllowedOrigins))
                        .Get<string[]>()!
                        .Select(_ => _.Trim().TrimEnd('/'))
                        .ToArray())
                .AllowAnyHeader()
                .AllowCredentials()
                .AllowAnyMethod()
                .WithExposedHeaders("Content-Disposition")));
        }

        private static IServiceCollection AddOpenTelemetry(this IServiceCollection services, IConfiguration config) {
            var openTelemetryConfig = config
                .GetSection(nameof(Shared.Configuration.OpenTelemetry));

            if (!openTelemetryConfig.Exists()) {
                Log.Warning("OpenTelemetry configuration section not found.");
                return services;
            }

            var openTelemetryTracingEndpoint = openTelemetryConfig
                .GetValue<string>(nameof(Shared.Configuration.OpenTelemetry.TracingEndpoint));

            if (string.IsNullOrWhiteSpace(openTelemetryTracingEndpoint)
                || !Uri.TryCreate(openTelemetryTracingEndpoint, UriKind.Absolute, out _)) {
                Log.Warning("OpenTelemetry TracingEndpoint not found or invalid.");
                return services;
            }

            var openTelemetryServiceName = openTelemetryConfig
                .GetValue<string>(nameof(Shared.Configuration.OpenTelemetry.ServiceName));

            if (string.IsNullOrWhiteSpace(openTelemetryServiceName)) {
                Log.Warning("OpenTelemetry ServiceName not found or invalid.");
                return services;
            }

            services
                .AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(openTelemetryServiceName))
                .WithTracing(tracing => {
                    tracing.AddAspNetCoreInstrumentation();
                    tracing.AddHttpClientInstrumentation();
                    tracing.AddRedisInstrumentation();
                    tracing.AddEntityFrameworkCoreInstrumentation();
                    tracing.AddSqlClientInstrumentation();
                    tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(openTelemetryTracingEndpoint));
                });

            Log.Information("OpenTelemetry configured.");

            return services;
        }
    }
}
