/*
This script is intended to be cleanly re-runnable. I should be ablto to run it, top to bottom
and recreate my database in place - this is for demo purpose
*/

--At the top of my file, before any command/statement. I want to make sure, 
--They run in the correcct database.
use LibraryDB;
GO -- GO is a MS SQL specific - it is a batching statement- It's telling 
-- the underlying SQL server instance that is reading (running) this file
-- "execute everythong above this GO, and agter any existing GO above, as 
-- one batch statemets"


/*
USE LibraryDB;
GO -- first batch

CREATE TABLE Author{}
CREATE TABLE Book{}
...
GO -- Second batch

... and so on and so forth
*/

--Section 1 -DDL
-- CREATE: Creating new tables, schemas, databases: NOT INDIVIDUAL ROWS
-- DROP: Deletes a table, or schema or database entirely
-- TRUNCATE: Delets all data in a table - preserves structure (columns + constraints)
-- ALTER:  Used to edit the structure of an existing table (add column), tweak constrints, etc)

DROP TABLE IF EXISTS dbo.Loan;
DROP TABLE IF EXISTS dbo.Book;
DROP TABLE IF EXISTS dbo.Member;
DROP TABLE IF EXISTS dbo.Author;
GO
--The first thing I want to do, is create my tables
--Table name are technically pre-penden by the schema
--We have a schema already - MS SQL Server creates a default schema called "dbo"
-- "DataBase Owner". If I create Author without specifing a schema, SQL Server
--associates it with this default dbo schema. SO the full table name becomes:
--dbo.Authon

CREATE TABLE dbo.Author
(--Column-name data-type constraints(optional)
--IN MY SQL SERVER, Identity lets us define a PK that automatically increments. Start at 1, increment by 1
    AuthorId INT IDENTITY(1,1), 
    FirstName VARCHAR(50) NOT NULL, --constraints are to
    LastName VARCHAR(50) NOT NULL,
    BirthYear INT NULL, -- NULL here signify that we intend for this to maybe be null

    --After I define my columns, datatype and basic constraints
    --I can optionally add some named constraints. If I dont name constraints,
    --nothing breaks BUT I can make my life easier and make error messages more
    --functional/readable by explicitily naming my constraints
    CONSTRAINT PK_Author PRIMARY KEY (AUthorId),
    --WHEN someone tries to add an Author, make sure that BirthYear is either NULL, OR between 300 and 2050
    CONSTRAINT CK_BirthYear CHECK (BirthYear IS NOT NULL OR BirthYear BETWEEN 300 and 2050)
    --If you think that you might need to alter a table's constraints,
    --you should name them. It makes running ALTER TABLE commands easier later
)
GO -- Including my GO batch statements for MY SQL SERVER

CREATE TABLE dbo.Member(
    --This is perhaps faster but not best practice. For example, lets say I want to change from MemberId as my PK
    --to email as my PK. By doing an in-line non-named constraints, I have shot myself in the foot. 
    --It is to ALTER our table later on, to play with the constraint
    MemberId INT IDENTITY(1,1) NOT NULL PRIMARY KEY, --Fun fact, Identity will not reuse any number even if deleted
    FirstName VARCHAR(50) NOT NULL,
    LastName VARCHAR(50) NOT NULL,
    Email VARCHAR(125) NOT NULL UNIQUE,
    --Using default constraint, if no value is provided, 
    --the build GETDATE() function gets a value to satisfy the column
    JoinedDate DATE NOT NULL DEFAULT(GETDATE()),
)
--Book is our largest table so far
CREATE TABLE dbo.Book
(
    --Columns + some constrints
    BookId INT IDENTITY(1,1) NOT NULL,
    Title VARCHAR(200) NOT NULL,
    ISBN CHAR(13) NOT NULL,
    PublishedYear INT NULL,
    --Creating a named constraint inline, useful for the constrint code isn't super long
    CategoryName VARCHAR(60) NOT NULL CONSTRAINT DF_Book_CateforyName DEFAULT('General'),
    AuthorId INT NOT NULL, --This will be a foreign key, we'll set the FK constrint below
    TotalCopies INT NOT NULL CONSTRAINT DF_Book_TotalCopies DEFAULT(1),
    AvailableCopies INT NOT NULL CONSTRAINT DF_Book_AvailableCopies DEFAULT(1),
    --More named constraints below
    CONSTRAINT PK_Book PRIMARY KEY (BookId),
    CONSTRAINT UQ_Book_ISBN UNIQUE(ISBN),

    --Setting our first Foreign Key constrint
    --We need to tell SQL engine, what column in this table is getting the FK constraint
    --as well as what column is another existing table that FK points to
    --When I set a FK CONSTRAINT, I can optionally set the delete behavior via ON DELETE
    -- CASCADE - Very Risky - If an author is deleted, all their books go to
    -- SET NULL - Los risk - If an author is deleted, this AuthorId in Book is set to null (requires a nullable column)
    -- RESTRICT - SAFE - Default behavior, blocks deletion of an author if any books reference it
    -- NO ACTION - SAFE -Same as restrict in MS MSQL SERVER
    -- SET DEFAULT - Low risk - Requires a default value constraint, will set the FK column to that value if the author is deleted
    CONSTRAINT FK_Book_Author FOREIGN KEY (AuthorID) REFERENCES dbo.Author(AuthorId) ON DELETE CASCADE,

    --The final thing I want to do is enforce some logical rules about Available and Total copies
    --AvailableCopies can not exceed TotalCopies
    CONSTRAINT CK_book_COpies CHECK(TotalCopies >= AvailableCopies)

);
GO

-- Loan -- two foreign key
-- Represents the library loaning a book to a member
CREATE TABLE dbo.Loan
(
    LoanId INT IDENTITY(1,1), --PK
    BookId INT NOT NULL, -- FK 
    MemberID INT NOT NULL, --FK
    --Date stamp for when the book was Lent to the member
    LoanDate DATE NOT NULL CONSTRAINT DF_Loan_LoanDate DEFAULT(GETDATE()),
    DueDate DATE NOT NULL,
    ReturnDate DATE NULL, --This will remain NULL until the book is actually returned

    --More named constraints below
    CONSTRAINT PK_Loan PRIMARY KEY(LoanId),
    --Note: Technically, FK columns don't have to match the column in the table they are a PK in
    CONSTRAINT FK_Loan_Book FOREIGN KEY(BookId) REFERENCES dbo.Book(BookId),
    CONSTRAINT FK_Loan_Member FOREIGN KEY(MemberId) REFERENCES dbo.Member(MemberId),
    CONSTRAINT CK_Loan_Dates CHECK (DueDate >= LoanDate) --DueDate has to be in the future
); 
GO

--Now that I've created the tables, using CREATE(DDL), How can I edit the tables itselves
ALTER TABLE dbo.Book ADD Edition INT NOT NULL CONSTRAINT DF_Book_Edition DEFAULT(1);

--We can get more granular, and not just add a new column, we can edit things about existing columns
--I can use this ALTER TABLE + ALTER COLUMN to add or edit constraints
ALTER TABLE dbo.Book ALTER COLUMN Title VARCHAR(250) NOT NULL;

--Ideally, we would never have to ALTER stuff. When possible, do it in CREATE. In the real world,
--you will need to ALTER things about the table in a schema. Once you have data in a table, you are
--stuck ALTERING it. Prior to giving a table any data, it is often easier to drop the table and
--edit the  statement for it

--DROP and TRUNCATE - please Learn the difference
--DROP: removes a table entirely, Data is lost, the structure is also gone. Like it never existed
--DROP TABLE dbo.Loan;

--TRUNCATE: removes all the data(rows) in table, eaves behind the structure
--TRUNCATE TABLE vbo.Loan;

/*
SECTION 2: DML + DDL - reading and Writing (CRUD)
DML - Data manipulation languade - Used for affectiong rows in a table
 INSERT - Used to insert new rows in an existing table
 UPDATE - Used to update an existing row's information
 DELETE - Used to remove a row

DQL - Data Query Languade - Select rows.
 SELECT - Used to select a record or recornds. (This is where otehre SQL
 keywords like GROUP BY, HAVING, WHERE, etc live)
*/

--DML first - let's see our database
-- Single row insertion. It is bet practice - borderline mandatory - to explicitly list he columns
-- you are inserting into
use LibraryDB;
/*
INSERT INTO dbo.Author(FirstName, LastName, BirthYear) VALUES
    ('Robert','Martin', 1952);

INSERT INTO dbo.Author(FirstName, LastName, BirthYear) VALUES
    ('Martin','Flowler', 1963),
    ('Frank','Herbert', 1920),
    ('Kent','Beck', 1961);

SELECT * FROM dbo.Author;
--Strings are done with ' '
INSERT INTO dbo.Book(Title, ISBN, PublishedYear, CategoryName, AuthorId, TotalCopies, AvailableCopies, Edition) VALUES
('Clean Code', '9780132350884', 2008, 'Software',5,3,3,1);

SELECT * FROM dbo.Author;
GO
*/


INSERT INTO dbo.Author (FirstName, LastName, BirthYear) VALUES
    ('Robert',  'Martin',   1952),   -- 1
    ('Martin',  'Fowler',   1963),   -- 2
    ('Kent',    'Beck',     1961),   -- 3
    ('Erich',   'Gamma',    1961),   -- 4
    ('Andrew',  'Hunt',     1964),   -- 5
    ('David',   'Thomas',   1956);   -- 6
GO
select * from dbo.Author;

INSERT INTO dbo.Member (FirstName, LastName, Email, JoinedDate) VALUES
    ('Ada',     'Lovelace', 'ada@example.com',     '2025-01-10'),  -- 1
    ('Grace',   'Hopper',   'grace@example.com',   '2025-02-02'),  -- 2
    ('Alan',    'Turing',   'alan@example.com',    '2025-02-20'),  -- 3
    ('Linus',   'Torvalds', 'linus@example.com',   '2025-03-15'),  -- 4
    ('Margaret','Hamilton', 'margaret@example.com','2025-04-01'),  -- 5
    ('Dennis',  'Ritchie',  'dennis@example.com',  '2025-05-05');  -- 6
GO
SELECT * FROM Member;

INSERT INTO dbo.Book (Title, ISBN, PublishedYear, CategoryName, AuthorId, TotalCopies, AvailableCopies, Edition) VALUES
    ('Clean Code',                         '9780132350884', 2008, 'Software',            1, 3, 3, 1),
    ('Clean Architecture',                 '9780134494166', 2017, 'Software',            1, 2, 2, 1),
    ('Refactoring',                        '9780201485677', 1999, 'Software',      2, 2, 1, 2),
    ('Patterns of Enterprise Application Architecture','9780321127426',2002,'Software', 2, 1, 1, 1),
    ('Test Driven Development',            '9780321146533', 2002, 'Testing',         3, 2, 2, 1),
    ('Extreme Programming Explained',      '9780321278654', 2004, 'Process',           3, 1, 0, 2),
    ('Design Patterns',                    '9780201633610', 1994, 'Software',          4, 2, 2, 1),
    ('The Pragmatic Programmer',           '9780201616224', 1999, 'Software',           5, 4, 3, 1),
    ('The Pragmatic Programmer 20th Anniv','9780135957059', 2019, 'Software',            5, 2, 2, 2),
    ('Programming Ruby',                   '9780974514055', 2004, 'Languages',           6, 1, 1, 1);
GO

INSERT INTO dbo.Loan (BookId, MemberID, DueDate, ReturnDate)
VALUES(3, 1, '2026-6-30', NULL);

SELECT * FROM dbo.Author; 
SELECT * FROM dbo.Book; 
SELECT * FROM dbo.Member; 
SELECT * FROM dbo.Loan; 
-- We have some data in our DB - Lets do some update
-- Lets grab a boook and give it a new Editino number
UPDATE dbo.Book
SET EDITION = 2
WHERE BookId = 3; --if I leave this off, EVERY ROW gets that new value

-- I can also do calculations based on existing values inside the SET area
UPDATE dbo.Book
SET AvailableCopies = AvailableCopies -1 --removing a copy from circulation entirely
WHERE BookId = 1;

--lets remove a row
--Same general rules as UPDATE - if you dont include a WHERE you have truncated the table
DELETE FROM dbo.Member 
WHERE Email = '' -- DONT FORGET THE WHERE

--DELETE FROM dbo.Loan
--WHERE BookId = 3; 
GO
-- DQL
SELECT * FROM dbo.Book; -- The simplest select

SELECT Title, PublishedYear, AvailableCopies from dbo.Book;

-- SELECT with a computed column, aliased with AS
SELECT Title, TotalCopies - AvailableCopies as CopiesOut from dbo.Book;

-- Getting back everything from a table is fine, for our training. Usually, we want to be more specific
SELECT Title, PublishedYear
FROM dbo.Book
WHERE PublishedYear >= 2000; --using WHERE as a filter

-- I can use things like BETWEEN, LIKE, and is combined with my WHERE
-- to provide more complex/precise filtering logic

--I wanto just the title from every book published between 1999 and 2004
SELECT Title
FROM dbo.Book
WHERE PublishedYear BETWEEN 1999 and 2004;

-- I want title, category name from everybook who's category name is either softwware or testing
SELECT Title, CategoryName
from dbo.Book
WHERE CategoryName in('Software', 'testing'); -- By default, many SQL RBDMS system are case-insensitive for comparison
--They render case, and when tou retunr a valur back to say a c# program, case is preserved. BUT
--when doing comparisons on the DB 'Testing' = 'testing' UNLESS we change the collation setting during server creation

SELECT Title 
FROM dbo.Book
where Title LIKE 'Test%';

--Last SELECT in this section
--Give me every book title where the category is software and available copies is greater than 1

SELECT Title
from dbo.Book
where CategoryName = 'Software'
and AvailableCopies > 1;

--Give me every book ttle where the publisherUear was not provided
SELECT Title 
FROM dbo.Book
WHERE PublishedYear is NULL; --if we are trying to do a comparison to assert that somethings
--is null, we dont use =. In sql null doesnt equal anything. Its unknown, the absence of a value

-- LIKE vs IN vs =
-- = matches one exact value
-- IN - matches any value in the provided list
-- LIKE - matches some patters with wildcards %

--ORDER BY and DISTINCT
-- we probably want to be able to order the returned records based on some logic
--atLeast sometimes

SELECT Title, PublishedYear
FROM dbo.Book
ORDER BY PublishedYear DESC, Title; 

-- Using Distinct
--Give me all the distinct category names that appear in dbo.Book
SELECT DISTINCT CategoryName
FROM dbo.Book
ORDER BY CategoryName; -- ASC

--ORDER BY - Sorts the output, bu¿y default in ascending order

--IT USES SUBQUERY KEYS TO SORT WITHIN some category

--Distinct - removes duplicates from the result set

--GROUP BY and HAVING - a preview
-- We are definitly coming back to this later whis week

--Give me the category name, and the count of books in that category
--where the count is more than 2. Order the rsults by book count descending

SELECT CategoryName, COUNT(*) as BookCount
FROM dbo.Book
GROUP BY CategoryName
HAVING COUNT(*) > 2 -- I cant use an alias name in HAVING, either a column that exist, or some function
ORDER BY BookCount; 

--GROUP BY CategoryName - Collapses all rows within that category into one group
--Count(*) - an aggregate function that counts the rows in each group
--We get back one line per CategoryName, with the number of books in with that CategoryName

--Havig vs Where
--HAVING filters groups in a GROUP BY, after agrouping
--Where filters rows

--GROUP BY vs DISTINCT
--DISTINCT is just straight de-duping
--GROUP BY lest you run computation against the groups. Count how many per group for example

--If I have a SELECT that blends WHERE, GROUP BY and HAVING
-- WHERE rune before any GROUP BY, and filters the raw word that are then passed
-- to GROUP BY, then HAVING filters the group