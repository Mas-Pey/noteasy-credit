using System.Text;
using EasyCreditApi.Application.Interfaces;
using EasyCreditApi.Infrastructure;
using EasyCreditApi.Infrastructure.Security;
using EasyCreditApi.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using BCrypt;
using EasyCreditApi.Application.Services;
using EasyCreditApi.Infrastructure.Oracle;
using Dapper;
using EasyCreditApi.Application.DTO;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using EasyCreditApi.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Secret lokal (connection string, API key, JWT key) — file ini di-gitignore.
// Alternatif: environment variable (ConnectionStrings__Oracle, ApiKey, Jwt__Key).
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// PostgreSQL connection string dari appsettings.json
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' tidak ditemukan.");

// Serilog: sink & level diatur di section "Serilog" pada appsettings.json.
builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


/// JWT
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Konfigurasi 'Jwt' tidak ditemukan");

// Repo tidak menyimpan secret, in dev, use key lokal supaya app bisa di run tanpa appsettings.Local.json
// Di luar dev, key wajib diisi
if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Konfigurasi 'Jwt:Key' wajib diisi.");

    jwtSettings.Key = "JwtKunciRahasia2026HarishGanteng";
    builder.Configuration["Jwt:Key"] = jwtSettings.Key;
}

// if (string.IsNullOrWhiteSpace(ApiKey)



builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            RoleClaimType = "role",
            NameClaimType = "name",
            ClockSkew = TimeSpan.Zero
        };

        // 401/403 dari JWT juga memakai format ApiResponse.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Unauthorized: token tidak valid atau tidak ada."));
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                await ctx.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Forbidden: akses ditolak."));
            }
        };
    });

builder.Services.AddAuthorization();

// ── DEPENDENCY INJECTION ──
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
// builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<OracleConnectionFactory>();
// builder.Services.AddScoped<ICustomerRepository, OracleCustomerRepository>();
builder.Services.AddScoped<OracleTransactionDemo>();
builder.Services.AddScoped<ICustomerSpRepository, OracleCustomerSpRepository>();
builder.Services.AddScoped<ICustomerSpService, CustomerSpService>();
builder.Services.AddScoped<ICreditApplicationSpRepository, OracleCreditApplicationSpRepository>();
builder.Services.AddScoped<ICreditApplicationSpService, CreditApplicationSpService>();
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ICustomerRepository, EfCustomerRepository>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(postgresConnectionString);
});

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Error validasi model -> 400 dengan format ApiResponse.
        options.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => e.Key,
                    e => e.Value!.Errors.Select(x => x.ErrorMessage).ToArray());

            return new BadRequestObjectResult(
                ApiResponse<Dictionary<string, string[]>>.Fail("Validasi gagal.", errors));
        };
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Endpoint lab: hanya aktif di Development.
    app.MapGet("/_dbping", async (OracleConnectionFactory factory) =>
    {
        await using var conn = factory.Create();
        var ok = await conn.ExecuteScalarAsync<int>("SELECT 1 FROM dual");

        return ok == 1
            ? Results.Ok(ApiResponse<object>.Success(new { database = "oracle", schema = "PEY_NET", ok = true }))
            : Results.Json(ApiResponse<object>.Fail("Oracle tidak merespons dengan benar."), statusCode: 500);
    });

    app.MapPost("/_tx-demo", async (OracleTransactionDemo demo, bool fail = true) =>
    {
        try
        {
            var id = await demo.InsertTwoAsync(fail);
            return Results.Ok(ApiResponse<object>.Success(new { committed = true, firstId = id }));
        }
        catch (Exception ex)
        {
            return Results.Json(ApiResponse<object>.Fail(ex.Message), statusCode: 500);
        }
    });

    // Helper lab Day 6: kembalikan application ke SUBMITTED agar smoke test bisa diulang.
    app.MapPost("/_reset-app/{id:int}", async (int id, OracleConnectionFactory factory) =>
    {
        await using var conn = factory.Create();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        var rows = await conn.ExecuteAsync(
            "UPDATE EASYCREDIT.CREDIT_APPLICATIONS SET STATUS = 'SUBMITTED', APPROVED_BY = NULL, APPROVED_AT = NULL WHERE ID = :Id",
            new { Id = id }, tx);

        tx.Commit();
        return Results.Ok(new { reset = rows });
    });
}

// Urutan: Serilog request log (paling luar) -> correlation/request log -> exception -> API key.
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        if (http.Items.TryGetValue(RequestLoggingMiddleware.ItemKey, out var id))
            diag.Set(RequestLoggingMiddleware.ItemKey, id);
    };
});

app.UseRequestLogging();

app.UseCustomExceptionHandler();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyMiddleware>());

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();