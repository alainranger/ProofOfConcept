public static class DatabaseSetupExtension
{
    public static IResourceBuilder<SqlServerDatabaseResource> AddDatabase(this IDistributedApplicationBuilder builder, string databaseName)
    {
        // Add SQL Server database with persistent lifetime
        var sql = builder.AddSqlServer("sql")
                         .WithHostPort(52425);
        //.WithLifetime(ContainerLifetime.Persistent);

        var creationScript = $$"""
            IF DB_ID('{{databaseName}}') IS NULL
            CREATE DATABASE [{{databaseName}}];
            GO

            -- Use the database
            USE [{{databaseName}}];
            GO

            -- Create Orders table
            IF OBJECT_ID('dbo.Order', 'U') IS NULL
            CREATE TABLE dbo.Order
            (
                OrderID INT PRIMARY KEY IDENTITY(1,1),
                OrderDate DATETIME NOT NULL,
                CustomerName NVARCHAR(100) NOT NULL,
                TotalAmount DECIMAL(18, 2) NOT NULL
            );
            GO
            -- Create OrderItems table
            IF OBJECT_ID('dbo.OrderItem', 'U') IS NULL
            CREATE TABLE dbo.OrderItem
            (
                OrderItemID INT PRIMARY KEY IDENTITY(1,1),
                OrderID INT NOT NULL,
                ProductName NVARCHAR(100) NOT NULL,
                Quantity INT NOT NULL,
                UnitPrice DECIMAL(18, 2) NOT NULL,
                FOREIGN KEY (OrderID) REFERENCES dbo.Order(OrderID)
            );
            GO
            -- Create stored procedure to get order by id with items
            IF OBJECT_ID('dbo.GetOrderById', 'P') IS NULL
            EXEC('
            CREATE PROCEDURE dbo.GetOrderById
                @OrderID INT
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT * FROM dbo.Order WHERE OrderID = @OrderID;
                SELECT * FROM dbo.OrderItem WHERE OrderID = @OrderID;
            END
            ');
            GO
            -- Create stored procedure to get all Order
            IF OBJECT_ID('dbo.GetAllOrders', 'P') IS NULL
            EXEC('
            CREATE PROCEDURE dbo.GetAllOrders
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT * FROM dbo.Order;
            END
            ');
            GO
        """;

        var db = sql.AddDatabase(databaseName)
                    .WithCreationScript(creationScript);

        return db;
    }
}