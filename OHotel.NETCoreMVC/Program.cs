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
    //�쥻�O JsonNamingPolicy.CamelCase�A�j���Y��r��p�g�A�ڰ��n������ˡA�]��null
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    //model josn �s���`�׶}��,�b �@��h�����p�U �n���o�U�hmodel��ƨϥήɶ}��
    //�p�]�w options.JsonSerializerOptions.ReferenceHandler=ReferenceHandler.Preserve json �榡�|�h�X $id $value 
    //options.JsonSerializerOptions.ReferenceHandler = null;
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<EasyCLib.NET.Sdk.IDbFunction, SqlServerDbFunction>();//-EasyCLib.NET.Sdk ��Ʈw�s�� �̩ۨʪ`�J
builder.Services.AddScoped<IVerifyHelper, VerifyHelper>();//-EasyCLib.NET.Sdk ��Ʈw�s�� �̩ۨʪ`�J
builder.Services.AddSingleton<IEncryptDecrypt, EncryptDecrypt>();//�̩ۨʪ`�J EncryptDecrypt
builder.Services.AddSingleton<IToolsCLib, ToolsCLib>();//�̩ۨʪ`�J ToolsCLib

// �[�JJWT ����
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        // �z�L�o���ŧi�A�N�i�H�q "sub" ���Ȩó]�w�� User.Identity.Name
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        RoleClaimType = "roles",        // �z�L�o���ŧi�A�N�i�H�q "roles" ���ȡA�åi�� [Authorize] �P�_����
        ValidateIssuer = true,        // ���ҵo���
        ValidateAudience = false,        // �q�`���ӻݭn���� Audience
        ValidateLifetime = true,        //���� Token �����Ĵ���
        ValidateIssuerSigningKey = false,        // �p�G Token ���]�t key �~�ݭn���ҡA�@�볣�u��ñ���Ӥw
        ValidIssuer = "JWT.JXINFO",        // ValidAudience = "JwtAuthDemo", // �����ҴN���ݭn��g
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("KsQvD2ROnqFOT6W4"))
    };
});
//�[�J���ҪA��
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
    app.UseSwagger(); // ���F��K�ާ@API�[�J
    app.UseSwaggerUI(options => // ���F��K�ާ@API�ӥ[�J�ϥΪ�UI����
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        options.RoutePrefix = "api/swagger"; /*�o��]�w�s��*/
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles(); //���\�ɮ�Ū��

// If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseCors() must go between them.
app.UseRouting();
app.UseHttpsRedirection();
app.UseCors(options =>
{
    options.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
}); //�ϥθ���� // ���FAPI�[�J

app.UseAuthentication(); //����
app.UseAuthorization(); //���v

//app.MapAreaControllerRoute( // �ζH�x��  (�¼g�k �|�d FP-client)
//    name: "client_route",
//    areaName: "FP-client",
//    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}",
//    defaults: new { action = "Index" }
//);

app.MapControllerRoute( // �ζH�x��
    name: "client_route",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "FP-client" }
);

app.MapAreaControllerRoute( // �޲z�̤���
    name: "dev_route",
    areaName: "Sys",
    pattern: "Sys/{controller=Welcome}/{action=Index}/{id?}",
    defaults: new { action = "Index" }
);

app.MapControllerRoute( // �q�{
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();