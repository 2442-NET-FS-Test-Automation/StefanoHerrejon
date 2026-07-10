README for Stefano's Proyect. Tickets

Order Fulfillment Service — a Minimal-API app that takes a burst of incoming orders and fulfills them concurrently against a shared, limited inventory stored in a real SQL database through EF Core. The project takes the role of a Ticket selling website. 

Orders can have multiple Orderlines (Meaning 1 order for many tickets wanted) but for examples/benchmarks only one orderline is created per order
Non-key index sku serves to searches for tickets by their sku
ACID/isolation reasoning for the one-transaction-per-order fulfillment. In order to have data consistency and avoid rewrites, overwrites if multiplle orders are being work on simultaneously
The token-vs-lock contrast: EF optimistic concurrency vs lock/Interlocked, and when each fits. For the project is used Row Version becouse the inventory lives on sql server, shared values inside the program that then comapre to the db. 


Packages -> MicroSoftSqlServer, Swagger, Serilog

Log -> Logs are generated with Serilog and stored one per execution with named with timestamp => 

DB -> (See Diagram : DBDiagram.png)
    Customer (id PK, name, email, List<Orders>)
    Ticket (id, PK Sku, name, price)
    TicketItem (id PK, 1:1 to Ticket, QuantityOnHand, byte[] RowVersion)
    Order (id PK, FK CustomerId, FK List<OrderLines>, FK FUlfillmentEvent, Priority, Status, CreatedUtc, CompletedAt)
    OrderLines (id PK, FK OrderId, FK TickedId, quantity)
    FulfillmentEvent (id PK, FK OrderId, type, message, FulfilledAutUtc) 


ToRun ->
    Docker -> Open and with DB
    Dotnet Run on PROJECTSTEFANOHERREON/Fulfillment.Api/
    Go to LocalHost####/swagger


Acceptance probes -> End

    Probe -> Endpoint that fulfills the criteria

    P1 — Seed and inspect -> /inventory and /inventpry/reset

    P2 — Burst without blocking -> /orders/burst and any other endpoint

    P3 — No oversell (the headline) -> /orders/burst -> /inventory

    P4 — Expedited first (Target) -> /benchmark-PriorityQueue -> 
        Check log for "Starting order = {OrderId} at " and "Finish Order {orderId} " and compare them to orders that get return from endpoint
        *Orders with expedited Priority are set to start first but not necessarely end first, that depends on factors such as the demand the ticket they want to buy

    P5 — Benchmark (Target) -> /benchmark-SeqVsBurst -> Get the times of sequential vs burst

    P6 — Graceful stop (Target) -> Make a burst and look for log and reset the program and check inventory /inventory

    P7 — Reports -> /benchmark-Reports -> Input the rank you want to get, return the 


EndPoints ->
    endpoint -> What it does
    Inputs
    Service/Interface(Function) -> Return
    Return & Possible Status code
    Big O Notation *(Only for priority queue, lookups, your report sort)


"/" -> The landing page for the app.
returns Results.Ok("Welcome to Stefano's DEMO!!!"); 200 OK

"/inventory" -> Endpoint that gets the whole inventory on the db
    returns a query with all the inventory - 200 ok if found, Not found 404
    Big O Notation O(n)

"/inventory/reset" -> Endpoint that resets the inventory
    returns "Stock reset" - 200 Ok

"/orders-all-time" -> Endpoint that gets all time Orders g
    IOrderService(OrdersAllTime) -> List<OrdersRecord>
    returns report of all orders group by Status and their count - 200 ok if found, Not found 404
    Big O Notation O(n)

"/orders-today" -> Endpoint that gets the orders from today
    IOrderService(OrdersToday) -> List<OrdersRecord>
    returns Orders group by Status and their count - 200 ok if found, Not found 404
    Big O Notation O(n)

"/orders-by-client" -> Endpoint that gets the orders info for each client
    IOrderService(OrdersByClient) -> List<OrdersClient>
    returns Orders from client group by Status and their count - 200 ok if found, Not found 404
    Big O Notation O(n)

"/orders-history-client" -> Endpoint that gets the clients order history
    Input -> ClientsId
    IOrderService(OrderHistory) -> List<OrdersSingleClient>
    Returns All orders from client - 200 ok if found, Not found 404
    Big O Notation O(n)

"orders/CountPending" -> Endpoint that gets Count of orders that are pending
    Returns Count of pending orders - 200 ok
    Big O Notation O(n)

"orders/deletePending" -> Endpoint that deletes the orders that are pending
    returns Count of orders eliminated - 204 Delete successful - 404 if nothing do delete

"/orders/create" -> Creates and fulfilles Order
    Input -> OrderPayLoad(TicketId, Quantity, CustomerId)
    returns result of order - 200 OK
    IFulfillmentService(FulfillOneAsync) -> Completes order

"/orders/burst" -> Creates a burst of orders, and fulfills them
    ISeeder(SeeOrders) -> Creates orders - Ok Orders complete
    FulfillBurstAsync -> Send all orders to be completed via FulfillOneAsync

"/TopSellingProducts" -> Gets the tickets and their total sales from all time
    Returns TicketId, TotalSold - 200 Ok If found any product - 404 if not found any
    Big O Notation O(n)

"/TopSellingProducts-NewOrders" -> Gets the tickets and their total sales from orders made
    Input -> n number of orders to run
    ISeeder(ResetAndCreateOrders) -> Resets inventory and creates orders
    IFulfillmentService(FulfillBurstAsync) -> Process Orders burst
    returns TopProducts(TicketId, TotalSold, Rank) - 200 ok - 404 not found
    Big O Notation O(n)

"/benchmark-SeqVsBurst" -> Times the differences between normal order and burst
    Input -> Quantity of orders (For both Sequantial and Burst)
    Times the time to complete n orders, and n BurstOrders
    Return sequantial time, burst time, and speedup - 200 ok - Results
    Big O Notation O(n)

"/benchmark-PriorityQueue" -> Makes n orders to check the function of PriorityQueue
    Input -> number of orders to be tested
    ISeeder(ResetAndCreateOrders) -> Resets inventory and creates orders
    IFulfillmentService(FulfillBurstAsync) -> Process Orders burst
    returns the orders information - 200 ok
    BigONotation -> O(n log n) -> n for deque, log n for enqueue

"/benchmark-Reports" -> Gets the n rank ticket based on sales (BinarySearch)
    Input -> Rank
    Returns the n rank ticket - 200 ok , 404 not found
    Big O Notation O(Log n)
