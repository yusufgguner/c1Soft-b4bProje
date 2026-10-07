using System.Security.Claims;
using System.Text;
using c1Soft_b4bProje.Areas.Admin;
using c1Soft_b4bProje.Data;
using c1Soft_b4bProje.Hubs;
using c1Soft_b4bProje.Middleware;
using c1Soft_b4bProje.Services;
using c1Soft_b4bProje.Services.Arama;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.Configure<AramaAyarlari>(builder.Configuration.GetSection("Arama"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var redisAyari = ConfigurationOptions.Parse(builder.Configuration["Arama:RedisBaglanti"] ?? "localhost:6379");
    redisAyari.AbortOnConnectFail = false;
    redisAyari.ConnectTimeout = 2000;
    redisAyari.SyncTimeout = 500;
    redisAyari.AsyncTimeout = 500;
    return ConnectionMultiplexer.Connect(redisAyari);
});
builder.Services.AddSingleton<ProductSearchIndex>();
builder.Services.AddSingleton<SearchCache>();
builder.Services.AddSingleton<ProductIndexSync>();
builder.Services.AddSingleton<ProductIndexInterceptor>();
builder.Services.AddScoped<ProductSearchService>();
builder.Services.AddHostedService<AramaIndeksHazirlayici>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(connectionString).AddInterceptors(sp.GetRequiredService<ProductIndexInterceptor>()));

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<OturumService>();
builder.Services.AddSingleton<LogService>();

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                string? deger = context.Principal?.FindFirst(OturumService.OturumClaim)?.Value;
                var oturumService = context.HttpContext.RequestServices.GetRequiredService<OturumService>();

                if (!Guid.TryParse(deger, out Guid oturumAnahtari) || !await oturumService.AktifMiAsync(oturumAnahtari))
                {
                    context.Fail("Oturumunuz kapatıldı. Hesabınıza başka bir yerden giriş yapılmış olabilir, tekrar giriş yapın.");
                }
            },
            OnChallenge = async context =>
            {
                if (context.AuthenticateFailure == null)
                {
                    return;
                }

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { mesaj = context.AuthenticateFailure.Message });
            }
        };
    });

builder.Services.AddAuthentication()
    .AddCookie(AdminPanel.Sema, options =>
    {
        options.LoginPath = "/admin/hesap/giris";
        options.LogoutPath = "/admin/hesap/cikis";
        options.AccessDeniedPath = "/admin/hesap/giris";
        options.Cookie.Name = "b4b_admin";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Adresler").Get<string[]>() ?? Array.Empty<string>())
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-Search-Ms", "X-Search-Source"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "c1Soft B4B API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "POST /api/auth/login ile aldığınız token'ı girin."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

app.UseMiddleware<HataYakalamaMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseCors("frontend");

app.UseAuthentication();
app.UseMiddleware<IslemLogMiddleware>();
app.UseAuthorization();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "admin/{controller=Panel}/{action=Index}/{id?}");

app.MapControllers();

app.MapHub<SiparisHub>("/hubs/siparis");

app.Run();
