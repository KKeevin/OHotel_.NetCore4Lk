using EasyCLib.NET.Sdk;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;
using OHotel.NETCoreMVC.Data;
using OHotel.NETCoreMVC.Helper;
using OHotel.NETCoreMVC.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 從設定檔讀取連線字串，若為空則在啟動時拋出明確錯誤
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";

// SQLite: 使用絕對路徑，確保 CreateFirstAdmin 與 Login 存取同一資料庫
if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase) && connectionString.Contains("ohotel.db"))
{
    var dbPath = Path.Combine(builder.Environment.ContentRootPath, "ohotel.db");
    connectionString = $"Data Source={dbPath}";
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = connectionString,
        ["ConnectionStrings:SQLCD_Read_OHotel"] = connectionString
    });
}

if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
    builder.Services.AddScoped<IDbFunction, SqliteDbFunction>();
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));
    builder.Services.AddScoped<IDbFunction, SqlServerDbFunction>();
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        if (builder.Environment.IsDevelopment())
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
            else
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    });
});
builder.Services.AddScoped<IVerifyHelper, VerifyHelper>();
builder.Services.AddSingleton<IEncryptDecrypt, EncryptDecrypt>();
builder.Services.AddSingleton<IToolsCLib, ToolsCLib>();

// 加入JWT 驗證（從 JwtSettings 讀取，避免硬編碼）
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<OHotel.NETCoreMVC.Models.JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not found in configuration.");
var signKey = jwtSettings.SignKey;
if (string.IsNullOrWhiteSpace(signKey) || signKey.Length < 16)
    throw new InvalidOperationException("JwtSettings:SignKey must be at least 16 characters for security.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = builder.Environment.IsProduction();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "sub",  // sub 存 STNo，供 GetUserInfo 與選單 API 使用
        RoleClaimType = "roles",
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey))
    };
});
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("BearerAuth", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "BearerAuth" } }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// Sqlite：啟動時自動初始化選單表與種子（若 ManageClass 為空）
if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IDbFunction>();
    var connStr = app.Configuration.GetConnectionString("SQLCD_Read_OHotel");
    if (!string.IsNullOrEmpty(connStr))
    {
        try { SqliteDbInitializer.EnsureTablesAndSeed(db, connStr); }
        catch { /* 忽略，可能資料庫尚未建立 */ }
    }
}

// 全域例外處理（須在最前）
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        options.RoutePrefix = "api/swagger";
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});
app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // API 屬性路由須優先於傳統路由

// Health Check 端點（供負載平衡器或監控使用）
app.MapHealthChecks("/health");

// 備援：最小 API 端點（若 Controller 路由 404 時可用）
app.MapPost("/api/account-login/create-first-admin", async (IDbFunction db, IConfiguration config) =>
{
    var connStr = config.GetConnectionString("SQLCD_Read_OHotel");
    if (string.IsNullOrEmpty(connStr)) return Results.BadRequest("Database not configured");
    var dbProvider = config["DatabaseProvider"] ?? "";
    try
    {
        db.DbConnect(connStr);
        if (string.Equals(dbProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var createSql = @"CREATE TABLE IF NOT EXISTS Staff (STNo INTEGER PRIMARY KEY AUTOINCREMENT, STName TEXT, LoginName TEXT, LoginPasswd TEXT, Tel TEXT, EMail TEXT, State INTEGER DEFAULT 0, AllPower INTEGER DEFAULT 0, LoginTime TEXT, LAmount INTEGER DEFAULT 0, CTime TEXT, MTime TEXT, MSNo INTEGER DEFAULT 0)";
            if (!db.AlterDb(createSql)) { db.DbClose(); return Results.StatusCode(500); }
            db.DbClose();
            OHotel.NETCoreMVC.Data.SqliteDbInitializer.EnsureTablesAndSeed(db, connStr);
            db.DbConnect(connStr);
        }
        if (!db.SelectDbDataView("SELECT COUNT(*) as cnt FROM Staff", "Staff")) { db.DbClose(); return Results.StatusCode(500); }
        if (db.SqlDataView.Count > 0 && Convert.ToInt32(db.SqlDataView[0]["cnt"]) > 0) { db.DbClose(); return Results.BadRequest("已有管理員"); }
        var enc = OHotelCLib.Alenher.EandD_Reply.StaffSet("admin123", 1, 1).Replace("'", "''");
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var ins = $"INSERT INTO Staff (STName, LoginName, LoginPasswd, Tel, EMail, State, AllPower, LoginTime, LAmount, CTime, MTime, MSNo) VALUES ('系統管理員', 'admin', '{enc}', '', '', 0, 1, '{now}', 0, '{now}', '{now}', 0)";
        if (!db.AlterDb(ins)) { db.DbClose(); return Results.StatusCode(500); }
        db.DbClose();
        return Results.Ok(new { success = true, loginName = "admin", password = "admin123", hint = "請至 /Sys/Login 登入" });
    }
    catch { db.DbClose(); return Results.StatusCode(500); }
});
app.MapControllerRoute(
    name: "client_route",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "FP-client" }
);

app.MapAreaControllerRoute(
    name: "dev_route",
    areaName: "Sys",
    pattern: "Sys/{controller=Welcome}/{action=Index}/{id?}",
    defaults: new { action = "Index" }
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();