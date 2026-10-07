using System.Text;
using EventTicketing.Api.Data;
using EventTicketing.Api.Filters;
using EventTicketing.Api.Middleware;
using EventTicketing.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) => config
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddControllers(options => options.Filters.Add<EnvelopeResultFilter>())
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = ValidationErrorResponse.Create);
builder.Services.AddExceptionHandler<EnvelopeExceptionHandler>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        };
        // Cookie takes priority (browser/frontend); fall back to the
        // Authorization header so Swagger UI and API clients work too.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var cookieToken = ctx.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(cookieToken))
                    ctx.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(name: "database", tags: ["ready"]);
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Ticketing API",
        Version = "v1",
        Description = "REST API for browsing events, booking tickets, and managing event listings."
    }));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    UserSeeder.Seed(db);
}

app.UseExceptionHandler(_ => { });
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Event Ticketing API v1");
    options.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
