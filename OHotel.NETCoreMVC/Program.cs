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

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    //原本是 JsonNamingPolicy.CamelCase，強制頭文字轉小寫，我偏好維持原樣，設為null
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    //model josn 連結深度開啟,在 一對多的情況下 要取得下層model資料使用時開啟
    //如設定 options.JsonSerializerOptions.ReferenceHandler=ReferenceHandler.Preserve json 格式會多出 $id $value 
    //options.JsonSerializerOptions.ReferenceHandler = null;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<EasyCLib.NET.Sdk.IDbFunction, SqlServerDbFunction>();//-EasyCLib.NET.Sdk 資料庫連結 相依性注入
builder.Services.AddScoped<IVerifyHelper, VerifyHelper>();//-EasyCLib.NET.Sdk 資料庫連結 相依性注入
builder.Services.AddSingleton<IEncryptDecrypt, EncryptDecrypt>();//相依性注入 EncryptDecrypt
builder.Services.AddSingleton<IToolsCLib, ToolsCLib>();//相依性注入 ToolsCLib

// 加入JWT 驗證
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        // 透過這項宣告，就可以從 "sub" 取值並設定給 User.Identity.Name
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        RoleClaimType = "roles",        // 透過這項宣告，就可以從 "roles" 取值，並可讓 [Authorize] 判斷角色
        ValidateIssuer = true,        // 驗證發行者
        ValidateAudience = false,        // 通常不太需要驗證 Audience
        ValidateLifetime = true,        //驗證 Token 的有效期間
        ValidateIssuerSigningKey = false,        // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
        ValidIssuer = "JWT.JXINFO",        // ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("KsQvD2ROnqFOT6W4"))
    };
});
//加入驗證服務
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
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "BearerAuth"}
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger(); // 為了方便操作API加入
    app.UseSwaggerUI(options => // 為了方便操作API而加入使用者UI介面
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        options.RoutePrefix = "api/swagger"; /*這邊設定連結*/
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(); //允許檔案讀取

// If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseCors() must go between them.
app.UseRouting();
app.UseHttpsRedirection();
app.UseCors(options =>
{
    options.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
}); //使用跨網域 // 為了API加入

app.UseAuthentication(); //驗證
app.UseAuthorization(); //授權

//app.MapAreaControllerRoute( // 形象官網  (舊寫法 會留 FP-client)
//    name: "client_route",
//    areaName: "FP-client",
//    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}",
//    defaults: new { action = "Index" }
//);

app.MapControllerRoute( // 形象官網
    name: "client_route",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "FP-client" }
);

app.MapAreaControllerRoute( // 管理者介面
    name: "dev_route",
    areaName: "Sys",
    pattern: "Sys/{controller=Welcome}/{action=Index}/{id?}",
    defaults: new { action = "Index" }
);

app.MapControllerRoute( // 默認
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();