using Library.Data;
using Microsoft.EntityFrameworkCore;
using Library.ControllerApi.Services;
using Library.ControllerApi.Mapping;
using Serilog;
using Library.Data.Enums;
using Library.ControllerApi.Middleware;
using Library.ControllerApi.Filters;
using System.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Library.Data.Entities;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//Adding connection string
var conn_string ="Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()//Write to console, and write to a file - starting a new file each day
    .WriteTo.File("log/fullfillment-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); //Tell the builder to use Serilog for logging

//Adding CORs
const string SpaCorsPolicy = "spa"; //string name for our policy

builder.Services.AddCors(o=> o.AddPolicy(SpaCorsPolicy, p=>p
    .WithOrigins("http://localhost:3000")
    .AllowAnyHeader()
    .AllowAnyMethod()

));

//Validation side of JWT. Issuance llives in TokenService
var jwtKey = builder.Configuration["Jwt:Key"]; //from appsettings.Development.json

//Hardcoding the issuer and audience - these have to match the ones we set on the token
const string jwtIssuer = "library-fulfillment";
const string jwtAudience = "library-fulfillment-clients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o=>o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwtIssuer,
        ValidateAudience = true, ValidAudience = jwtAudience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true
    });

builder.Services.AddAuthorization(); //ges after authorization

//Token Issuance is a plain injectable service. Its stateless so we can use a singleton
builder.Services.AddSingleton<ITokenService, TokenService>();

//Adding our httpClient
builder.Services.AddHttpClient<ISupplierclient, ISupplierclient>(c => 
    c.BaseAddress = new Uri("https://dummyjson.com/") // all calls append to this URL
);

builder.Services.AddDbContextFactory<LibraryDbContext>(o => o.UseSqlServer(conn_string));

// Adding the password hasher
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

//Adding out HTTPClient
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>(); //Could later swap for InventotyMagoRepo
builder.Services.AddScoped<IInventoryService, InventoryService>(); 
builder.Services.AddScoped<IUserService, UserService>();

//Adding password hasher
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

//Adding our mapping profile for AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

//Having our filter apply to every controller
builder.Services.AddControllers(o => o.Filters.Add<TimingFilter>());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Adding swagger back
//Swagger stuff added to builder
builder.Services.AddSwaggerGen();

//Adding caching
builder.Services.AddMemoryCache(); //Adding cache-ing to our server
builder.Services.AddResponseCaching(); //adding response cache-ing asking the front end to save request results

var app = builder.Build();

//Seeding admins - cont do a plain INSERT INTO using SQL becouse I tont havea a hashed pasword
//might be able to do it in libraryDBContext - would have to check how to do that
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

    //We want this to be idempotent. This block of code runs EVERY time the app stars
    //BUT we only want to seed our admin once
    if(!db.Users.Any(u => u.Role == "admin"))
    {
        var hasher = new PasswordHasher<User>();
        var admin = new User {UserName = "ada", Role = "admin"};

        //I should put that password  inside of some secret (non GH committed) file
        admin.PasswordHash = hasher.HashPassword(admin, "pass123"); //Put this in a config file pls!

        db.Users.Add(admin);
        db.SaveChanges();

    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();//Wraps all middleware below it, catches their exceptions

//Swagger stuff added to the app, to visualize and access endppoints
app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//This is a simple diagnostic middleware. All it do is time our request for us and log that
//It takes in ctx (HTTpcONTEXT -> everything about the request AND the response)
//next - represents a call to the subsequent middleware
app.Use(async (ctx, next) =>
{
    var sw = System.Diagnostics.Stopwatch.StartNew();

    await next();//calling the next middleware in the chain - whatever that is

    sw.Stop();

    Log.Information("{Method} {Path} -> {StatusCode} in time {Elapsed} ms",
        ctx.Request.Method, ctx.Request.Path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
});

app.Use(async(ctx, next) =>
{
    if(ctx.Request.Headers.ContainsKey("X-Maintenance"))
    {
        ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await ctx.Response.WriteAsync("Down for maintenance");
        return; //Dont call next . neces hits controller
    }

    await next(ctx);
});

app.UseResponseCaching(); //Using the response cache middleware

app.UseCors(SpaCorsPolicy); //using our policy, with the CORS middleware

//Must be in this ordr for Auth/Author
app.UseAuthentication(); //read and validate the tokens -> set User
app.UseAuthorization(); //enforces the [Authorize] / RequireAuthorization() decorators on endpoints

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

Log.CloseAndFlush(); //Remember to close and flush the logs (serilog)