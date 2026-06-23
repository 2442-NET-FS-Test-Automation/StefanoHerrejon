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
-- I will come back once we've created our tables, and include a drop section

--The first thing I want to do, is create my tables
--Table name are technically pre-penden by the schema
--We have a schema already - MS SQL Server creates a default schema called "dbo"
-- "DataBase Owner". If I create Author without specifing a schema, SQL Server
--associates it with this default dbo schema. SO the full table name becomes:
--dbo.Authon

CREATE TABLE dbo.Author
(--Column-name data-type constraints(optional)
--IN MY SQL SERVER, Identity lets us define a PK that automatically increments. Start at 1, increment by 1
    AuthorId INT IDENTITY(1,2), 
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

);GO

-- Loan -- two foreign key
-- Represents the library loaning a book to a member
CREATE TABLE dbo.LOCATION
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
); GO

--Now that I've created the tables, using CREATE(DDL), How can I edit the tables itselves
ALTER TABLE dbo.Book ADD Edition INT NOT NULL CONSTRAINT DF_Book_Edition DEFAULT(1);

--We can get more granular, and not just add a new column, we can edit things about existing columns