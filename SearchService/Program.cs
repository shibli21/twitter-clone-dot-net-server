
using Core.Extensions;
using Core.Interfaces;
using Infrastructure.Config;
using Infrastructure.Middlewares;
using Infrastructure.Services;
using JWTAuthenticationManager;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomSerilog(builder.Environment);

builder.Services.Configure<TwitterCloneDbConfig>(
    builder.Configuration.GetSection("TwitterCloneDatabaseSettings")
);

builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration.GetValue<string>("TwitterCloneDatabaseSettings:ConnectionString")));


builder.Services.AddSingleton<ISearchingService, SearchingService>();

builder.Services.AddControllers();

builder.Services.AddSingleton<JwtTokenHandler>();
builder.Services.AddCustomJwtAuthentication();

builder.Services.AddHttpContextAccessor();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

app.UseSwaggerDocumentation();

app.UseAuthentication();

app.UseAuthorization();

app.UseUserBlockedMiddleware();

app.MapControllers();

app.Run();
