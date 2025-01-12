using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PracticeApi.Core.UserServices;
using PracticeApi.Persistent;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Net;
using PracticeApi.Domain.DTO;
using PracticeApi.Core.Todo;

var builder = WebApplication.CreateBuilder(args);

//Dependency Injection for user service 
builder.Services.AddScoped<UserService>();
//Dependency Injection for todo service 
builder.Services.AddScoped<TodoService>();
// Add services to the container.

//Db connection
builder.Services.AddDbContext<DataContext>(
    options => options.UseNpgsql(builder
    .Configuration.GetConnectionString("NeonDbConnection")));

builder.Services.AddControllers();
//builder.Services.AddScoped<TokenService>();

// Configure the JWT authentication middleware
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8
            .GetBytes(builder.Configuration.GetSection("JWT:Secret").Value)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        options.Events = new JwtBearerEvents()
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                var code = HttpStatusCode.Unauthorized;
                context.Response.StatusCode = (int)code;
                context.Response.ContentType = "application/json";
                var result = JsonConvert.SerializeObject(new GeneralResponse<string>("You are not Authorized", (int)code));
                return context.Response.WriteAsync(result);
            },
            OnForbidden = context =>
            {
                var code = HttpStatusCode.Forbidden;
                context.Response.StatusCode = (int)code;
                context.Response.ContentType = "application/json";
                var result = JsonConvert.SerializeObject(new GeneralResponse<string>("You are not authorized to access this resource", (int)code));
                return context.Response.WriteAsync(result);
            },
        };
    });



// Configure Swagger/OpenAPI
// Add JWT Authentication to Swagger
builder.Services.AddSwaggerGen(setupAction =>
{


    setupAction.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = $"Input your Bearer token in this format - Bearer token to access this API",
    });
    setupAction.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        }, new List<string>()
                    },
                });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
