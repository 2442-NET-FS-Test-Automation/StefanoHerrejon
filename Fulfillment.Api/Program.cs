using Microsoft.EntityFrameworkCore;
using Fulfillment.Data;
//API
//Register things with the builder

//Configurings thigns on the app

//API Calls and RUN method
var builder = WebApplication.CreateBuilder(args);

//1) Give our builder a connection string to our database
var conn_string ="Server=localhost,1433;Database=SHFullFillment;User Id=sa; Password=LibraryPassword1; TrustServerCertificate=true";

//Builder => use FulfillmentDBContext and string above to connecto to the db
//Managing of creating, destroying is handed off 
builder.Services.AddDbContext<FulfillmentDBContext>(options => options.UseSqlServer(conn_string));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
