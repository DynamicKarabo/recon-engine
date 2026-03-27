using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReconciliationEngine.API.Behaviors;
using ReconciliationEngine.API.Configuration;
using ReconciliationEngine.API.Middleware;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Infrastructure.Data;
using ReconciliationEngine.Infrastructure.Services;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ReconciliationEngine.API")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.Debug();
});

var jwtConfig = builder.Configuration.GetSection(JwtConfiguration.SectionName).Get<JwtConfiguration>()
    ?? throw new InvalidOperationException("JWT configuration is missing");

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = jwtConfig.Authority;
        options.Audience = jwtConfig.Audience;
        options.RequireHttpsMetadata = jwtConfig.RequireHttpsMetadata;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Authority,
            ValidAudience = jwtConfig.Audience
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Operator", policy => policy.RequireRole(Roles.Operator));
    options.AddPolicy("Admin", policy => policy.RequireRole(Roles.Admin));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddDbContext<ReconciliationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<IEncryptionService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AzureKeyVaultEncryptionService>>();
    var keyVaultUrl = builder.Configuration["KeyVault:Url"] ?? throw new InvalidOperationException("KeyVault:Url is required");
    var keyName = builder.Configuration["KeyVault:KeyName"] ?? throw new InvalidOperationException("KeyVault:KeyName is required");
    
    return new AzureKeyVaultEncryptionService(keyVaultUrl, keyName, logger);
});

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"] ?? httpContext.TraceIdentifier);
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
    };
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<ValidationExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
