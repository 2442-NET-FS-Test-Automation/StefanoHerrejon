-- Parking Lot*******
-- *                *
-- *                *
--- *****************



-- Comment can be done single line with --
-- Comment can be done multi line with /* */

/*
DQL - Data Query Language
Keywords:

SELECT - retrieve data, select the columns from the resulting set
FROM - the table(s) to retrieve data from
WHERE - a conditional filter of the data
GROUP BY - group the data based on one or more columns
HAVING - a conditional filter of the grouped data
ORDER BY - sort the data
*/
use Chinook_AutoIncrement;

-- BASIC CHALLENGES
-- List all customers (full name, customer id, and country) who are not in the USA
SELECT
    FirstName + ' ' + LastName AS FullName,
    CustomerId,
    Country
FROM dbo.Customer
WHERE Country != 'USA';

-- List all customers from Brazil

SELECT 
    *
FROM dbo.Customer
Where Country = 'Brazil';


-- List all sales agents

SELECT 
    *
FROM dbo.Employee
WHERE Title like 'Sales%'


-- SELECT * FROM employee WHERE title LIKE '%Agent%;
SELECT 
    *
FROM Employee
WHERE Title like '%Agent%'

-- Retrieve a list of all countries in billing addresses on invoices

SELECT 
    DISTINCT BillingCountry
FROM dbo.Invoice

-- Retrieve how many invoices there were in 2022, and what was the sales total for that year?

SELECT
    YEAR(InvoiceDate) AS Year,
    COUNT(*) as TotalInvoices,
    SUM(Total) as YearSales
FROM dbo.Invoice
WHERE YEAR(InvoiceDate) = 2021
GROUP BY YEAR(InvoiceDate);



-- (challenge: find the invoice count sales total for every year using one query)

SELECT
    YEAR(InvoiceDate)AS Year,
    COUNT(*) as TotalInvoices
FROM dbo.Invoice
GROUP BY YEAR(InvoiceDate);


-- how many line items were there for invoice #37

SELECT 
    COUNT(*) AS Count,
    i.InvoiceId as InvoiceNumber
FROM dbo.Invoice i
LEFT JOIN dbo.InvoiceLine il ON i.InvoiceId = il.InvoiceId
WHERE i.InvoiceId = 37
GROUP BY i.InvoiceId; 

-- how many invoices per country? BillingCountry  # of invoices 

SELECT
    BillingCountry as Country,
    COUNT(*) AS Count
FROM dbo.Invoice
GROUP BY BillingCountry

-- Retrieve the total sales per country, ordered by the highest total sales first.

SELECT
    BillingCountry as Country,
    SUM(Total) as TotalSales
FROM dbo.Invoice
GROUP BY BillingCountry
ORDER BY TotalSales DESC;

-- JOINS CHALLENGES
-- Every Album by Artist

SELECT
    a.Name,
    al.Title
FROM dbo.Artist a
JOIN dbo.Album al ON a.ArtistId = al.ArtistId;

-- (inner keyword is optional for inner join)

-- All songs of the rock genre

SELECT
    t.Name,
    g.Name as Genre
FROM dbo.Track t
JOIN dbo.Genre g ON t.GenreId = g.GenreId
WHERE g.Name = 'Rock';

-- Show all invoices of customers from brazil (mailing address not billing)

SELECT
    i.InvoiceId,
    i.Total,
    c.Country
FROM dbo.Invoice i 
JOIN dbo.Customer c ON i.CustomerId = c.CustomerId
WHERE c.Country = 'Brazil';

-- Show all invoices together with the name of the sales agent for each one

SELECT
    I.InvoiceId,
    e.EmployeeId,
    e.FirstName
FROM dbo.Invoice i
LEFT JOIN dbo.Customer c ON i.CustomerId = c.CustomerId
LEFT JOIN dbo.Employee e ON c.SupportRepId = e.EmployeeId
ORDER BY e.EmployeeId ASC;

-- Which sales agent made the most sales in 2024?

SELECT 
    e.EmployeeId,
    e.FirstName+ ' '+ e.LastName as Name,
    SUM(i.Total) as TotalMoney,
    COUNT(*) as TotalSales
FROM dbo.Employee e
JOIN dbo.Customer c ON e.EmployeeId = c.SupportRepId
JOIN dbo.Invoice i ON c.CustomerId = i.CustomerId
WHERE (e.Title LIKE '%Sales%' OR e.TITLE LIKE '%agent%') AND Year(i.InvoiceDate) = 2024
GROUP BY e.EmployeeId, e.FirstName, e.LastName
ORDER BY TotalSales DESC


-- How many customers are assigned to each sales agent?

SELECT
    e.EmployeeId,
    COUNT(*) AS CustomersAssigned
FROM dbo.Employee e  
JOIN dbo.Customer c ON e.EmployeeId = c.SupportRepId
GROUP BY e.EmployeeId;

-- Which track was purchased the most in 2023?

SELECT 
    t.TrackId,
    SUM(il.Quantity) AS Sales
FROM dbo.Track t
JOIN dbo.InvoiceLine il ON t.TrackId = il.TrackId
JOIN dbo.Invoice i ON il.InvoiceId = i.InvoiceId
WHERE YEAR(i.InvoiceDate) = 2021
GROUP BY t.TrackId
ORDER BY Sales DESC

SELECT
    *
from dbo.Track
WHERE TrackId = 3249

-- Show the top three best selling artists.

SELECT TOP 3
    A.ArtistId,
    sum(il.Quantity) as SALES
FROM dbo.Artist a
JOIN dbo.Album al ON a.ArtistId = al.ArtistId
JOIN dbo.Track t ON al.AlbumId = t.AlbumId
JOIN dbo.InvoiceLine il ON il.TrackId = t.TrackId
GROUP BY a.ArtistId
ORDER BY SALES DESC;

SELECT  
    *
FROM Artist
WHERE ArtistId IN (90,150,50)

-- Which customers have the same initials as at least one other customer?



-- Which countries have the most invoices?

SELECT 
    BillingCountry,
    COUNT(*) TotalInvoices
FROM dbo.Invoice
GROUP BY BillingCountry
ORDER BY TotalInvoices DESC

-- Which city has the customer with the highest sales total?

SELECT TOP 1
    c.City,
    c.CustomerId,
    SUM(i.Total) TotalSales
FROM dbo.Customer c
JOIN dbo.Invoice i ON c.CustomerId = i.CustomerId
GROUP BY c.CustomerId, c.City
ORDER BY TotalSales DESC


-- Who is the highest spending customer?

SELECT TOP 1
    c.CustomerId,
    c.LastName,
    c.FirstName,
    SUM(i.Total) TotalSales
FROM dbo.Customer c
JOIN dbo.Invoice i ON c.CustomerId = i.CustomerId
GROUP BY c.CustomerId, c.FirstName, c.LastName
ORDER BY TotalSales DESC

-- Return the email and full name of of all customers who listen to Rock.

SELECT 
    c.Email,
    c.FirstName + ' ' + c.LastName AS FullName
FROM Customer c 
JOIN Invoice i on c.CustomerId = i.CustomerId
JOIN InvoiceLine il on i.InvoiceId = il.InvoiceId
JOIN Track t ON t.TrackId = il.TrackId
JOIN Genre g ON t.GenreId = g.GenreId
WHERE g.Name = 'Rock'
GROUP BY c.Email, c.FirstName ,c.LastName

-- Which artist has written the most Rock songs?

SELECT
    *
FROM dbo.Artist a

-- Which artist has generated the most revenue?




-- ADVANCED CHALLENGES
-- solve these with a mixture of joins, subqueries, CTE, and set operators.
-- solve at least one of them in two different ways, and see if the execution
-- plan for them is the same, or different.

-- 1. which artists did not make any albums at all?


-- 2. which artists did not record any tracks of the Latin genre?


-- 3. which video track has the longest length? (use media type table)



-- 4. boss employee (the one who reports to nobody)


-- 5. how many audio tracks were bought by German customers, and what was
--    the total price paid for them?



-- 6. list the names and countries of the customers supported by an employee
--    who was hired younger than 35.




-- DML exercises

-- 1. insert two new records into the employee table.

-- 2. insert two new records into the tracks table.

-- 3. update customer Aaron Mitchell's name to Robert Walter

-- 4. delete one of the employees you inserted.

-- 5. delete customer Robert Walter.
