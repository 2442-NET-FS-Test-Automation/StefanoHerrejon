using Library.Data;
using Microsoft.EntityFrameworkCore;
using Library.ControllerApi.Services;
using Library.ControllerApi.Mapping;
using Serilog;
using Library.Data.Enums;
using Library.ControllerApi.Middleware;
using Library.ControllerApi.Filters;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//Adding connection string
var conn_string ="Server=localhost,1433;Database=LibraryMinimalDb;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()//Write to console, and write to a file - starting a new file each day
    .WriteTo.File("log/fullfillment-log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog(); //Tell the builder to use Serilog for logging

builder.Services.AddDbContextFactory<LibraryDbContext>(o => o.UseSqlServer(conn_string));

//Registering our customer Repo and Service layer methods like we did before
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>(); //Could later swap for inventoryMongoRepo
builder.Services.AddScoped<IInventoryService, InventoryService>(); //Inventory service (Service Layer)

//Adding our mapping profile for AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MappingProfile).Assembly));

//Having our filter apply to every controller
builder.Services.AddControllers(o => o.Filters.Add<TimingFilter>());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Adding swagger back
//Swagger stuff added to builder
builder.Services.AddSwaggerGen();

var app = builder.Build();

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

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
