using Microsoft.EntityFrameworkCore;
using Fulfillment.Data.Entities;
using Fulfillment.Data.Enums;

namespace Fulfillment.Data;

//Code that does the actual SQL generation, DB connection
//CRUD, updating db to my models

public class FulfillmentDBContext : DbContext
{
    //Needs a constructor and arguments

    //Constructor, we dont call this constructor ASP.NET DI Container will do it
    public FulfillmentDBContext(DbContextOptions<FulfillmentDBContext> options) : base(options) { }

    //What c# classes are tracking as Entities?
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketItem> Inventory => Set<TicketItem>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderLines> OrderLines => Set<OrderLines>();

    public DbSet<FUlfillmentEvent> FUlfillmentEvents {get;set;}

    //OnModelCreating, for constraints, mapping column name and types
    //inside of here using something called Fluent API. EF core lest you do config in 3 ways. 
    // Convention <Data Annotations <FluentAPI in onModelCreating
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Ticket>(e =>
        {
            e.HasIndex(p=>p.Sku).IsUnique();//SKU is unique e Indice de busqueda
            e.Property(p=>p.Price).HasColumnType("decimal(10,2)");//Price is decimal
            e.HasOne(p=>p.Inventory)
                .WithOne(i=>i.Ticket)
                .HasForeignKey<TicketItem>(i=>i.TicketId);
        });

        //Setting up Concurrency token, RowVersion EF Core
        mb.Entity<TicketItem>().Property(i=>i.RowVersion).IsRowVersion();

        mb.Entity<Customer>().Property(c=>c.Email).HasMaxLength(256); //Length of email on Customer
        mb.Entity<Customer>().HasIndex(c=>c.Email).IsUnique();//Customer>Email is unique

        //Seed Data
        mb.Entity<Ticket>().HasData(//Id, Sku, Name, Price, 
            new Ticket{Id=1,Sku="TKT-1001", Name="Guns&Roses", Price=3500.99M},
            new Ticket{Id=2,Sku="TKT-1002", Name="Metallica", Price=1250M},
            new Ticket{Id=3,Sku="TKT-1003", Name="Post Malone", Price=1000.99M}
        );

        mb.Entity<TicketItem>().HasData(
            new TicketItem{Id=1,TicketId=1,QuantityOnHand=15},
            new TicketItem{Id=2,TicketId=2,QuantityOnHand=5},
            new TicketItem{Id=3,TicketId=3,QuantityOnHand=3}
        );

        mb.Entity<Customer>().HasData(
            new Customer{Id=1,Name="Stefano Herrejon", Email="stefano@email.com"},
            new Customer{Id=2,Name="Juan Pablo Z", Email="Juan.Pablo@email.com"},
            new Customer{Id=3,Name="Javier Perez", Email="Javis@gmail.com"}
        );
    }

}