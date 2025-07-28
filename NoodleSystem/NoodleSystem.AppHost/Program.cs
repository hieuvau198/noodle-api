var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var password = builder.AddParameter("password", secret: true);
var sql = builder.AddSqlServer("sql", password);

var databaseNameUser = "SpicyNoodleDbUser";
var databaseNamePayment = "SpicyNoodleDbPayment";
var databaseNameOrder = "SpicyNoodleDbOrder";

var creationScriptUser = $$"""
    IF DB_ID('{{databaseNameUser}}') IS NULL
        CREATE DATABASE [{{databaseNameUser}}];
    GO

    USE [{{databaseNameUser}}];
    GO

    -- Create Users Table
    CREATE TABLE Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255) NOT NULL UNIQUE,
        Password NVARCHAR(255) NULL, -- NULL for Google users
        GoogleId NVARCHAR(100) NULL,
        Role INT NOT NULL DEFAULT 2 CHECK (Role IN (1, 2)), -- 1 = Admin, 2 = Customer
        IsGoogleUser BIT NOT NULL DEFAULT 0,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Create Indexes for better performance
    CREATE INDEX IX_Users_Email ON Users(Email);

    -- Seed Users
    INSERT INTO Users (FullName, Email, Password, GoogleId, Role, IsGoogleUser, IsActive) VALUES
    ('John Doe', 'john.doe@email.com', 'hashedpassword123', NULL, 2, 0, 1),     -- Customer
    ('Jane Smith', 'jane.smith@gmail.com', NULL, 'google_id_12345', 2, 1, 1),   -- Customer (Google)
    ('Mike Johnson', 'mike.johnson@email.com', 'hashedpassword456', NULL, 1, 0, 1), -- Admin
    ('Sarah Wilson', 'sarah.wilson@gmail.com', NULL, 'google_id_67890', 2, 1, 1), -- Customer (Google)
    ('David Brown', 'david.brown@email.com', 'hashedpassword789', NULL, 1, 0, 1);  -- Admin
    GO
    """;

var creationScriptPayment = $$"""
    IF DB_ID('{{databaseNamePayment}}') IS NULL
        CREATE DATABASE [{{databaseNamePayment}}];
    GO

    USE [{{databaseNamePayment}}];
    GO

    -- Create Payments Table
    CREATE TABLE Payments (
        PaymentId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        Amount DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        PaymentMethod NVARCHAR(50) NULL,
        TransactionId NVARCHAR(255) NULL,
        PaidAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Create Indexes for better performance
    CREATE INDEX IX_Payments_OrderId ON Payments(OrderId);

    -- Seed Payments
    INSERT INTO Payments (OrderId, Amount, Status, PaymentMethod, TransactionId, PaidAt) VALUES
    (1, 15.49, 'Completed', 'Credit Card', 'txn_1234567890', DATEADD(MINUTE, -30, GETDATE())),
    (2, 22.98, 'Completed', 'PayPal', 'pp_0987654321', DATEADD(HOUR, -2, GETDATE())),
    (3, 18.99, 'Pending', 'Credit Card', NULL, NULL),
    (4, 13.49, 'Processing', 'Google Pay', 'gp_1122334455', DATEADD(MINUTE, -10, GETDATE())),
    (5, 25.47, 'Completed', 'Credit Card', 'txn_9988776655', DATEADD(HOUR, -1, GETDATE()));
    GO
    """;

var creationScriptOrder = $$"""
    IF DB_ID('{{databaseNameOrder}}') IS NULL
        CREATE DATABASE [{{databaseNameOrder}}];
    GO

    USE [{{databaseNameOrder}}];
    GO

    -- Create SpiceLevels Table
    CREATE TABLE SpiceLevels (
        SpiceLevelId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(50) NOT NULL,
        Level INT NOT NULL UNIQUE,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Create Toppings Table
    CREATE TABLE Toppings (
        ToppingId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        ImageUrl NVARCHAR(500) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Create SpicyNoodles Table
    CREATE TABLE SpicyNoodles (
        NoodleId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        BasePrice DECIMAL(10,2) NOT NULL,
        ImageUrl NVARCHAR(500) NULL,
        Description NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Create Orders Table
    CREATE TABLE Orders (
        OrderId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        TotalAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
    );

    -- Create OrderItems Table
    CREATE TABLE OrderItems (
        OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        NoodleId INT NOT NULL,
        Quantity INT NOT NULL DEFAULT 1,
        Subtotal DECIMAL(10,2) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (OrderId) REFERENCES Orders(OrderId) ON DELETE CASCADE,
        FOREIGN KEY (NoodleId) REFERENCES SpicyNoodles(NoodleId)
    );

    -- Create NoodleToppings Table (Junction table for many-to-many)
    CREATE TABLE NoodleToppings (
        NoodleToppingId INT IDENTITY(1,1) PRIMARY KEY,
        NoodleId INT NOT NULL,
        ToppingId INT NOT NULL,
        OrderItemId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (NoodleId) REFERENCES SpicyNoodles(NoodleId),
        FOREIGN KEY (ToppingId) REFERENCES Toppings(ToppingId),
        FOREIGN KEY (OrderItemId) REFERENCES OrderItems(OrderItemId) ON DELETE CASCADE,
        UNIQUE(NoodleId, ToppingId, OrderItemId)
    );

    -- Create NoodleSpiceLevels Table (Junction table)
    CREATE TABLE NoodleSpiceLevels (
        NoodleSpiceLevelId INT IDENTITY(1,1) PRIMARY KEY,
        NoodleId INT NOT NULL,
        SpiceLevelId INT NOT NULL,
        OrderItemId INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (NoodleId) REFERENCES SpicyNoodles(NoodleId),
        FOREIGN KEY (SpiceLevelId) REFERENCES SpiceLevels(SpiceLevelId),
        FOREIGN KEY (OrderItemId) REFERENCES OrderItems(OrderItemId) ON DELETE CASCADE,
        UNIQUE(NoodleId, SpiceLevelId, OrderItemId)
    );

    -- Create Indexes for better performance
    CREATE INDEX IX_Orders_UserId ON Orders(UserId);
    CREATE INDEX IX_Orders_Status ON Orders(Status);
    CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
    CREATE INDEX IX_OrderItems_NoodleId ON OrderItems(NoodleId);

    -- Seed SpiceLevels
    INSERT INTO SpiceLevels (Name, Level) VALUES
    ('Mild', 1),
    ('Medium', 2),
    ('Hot', 3),
    ('Very Hot', 4),
    ('Extreme', 5),
    ('Ghost Pepper', 6);

    -- Seed Toppings
    INSERT INTO Toppings (Name, Price, ImageUrl) VALUES
    ('Extra Noodles', 2.50, '/images/toppings/extra-noodles.jpg'),
    ('Fried Egg', 1.50, '/images/toppings/fried-egg.jpg'),
    ('Grilled Chicken', 4.00, '/images/toppings/grilled-chicken.jpg'),
    ('Beef Slices', 5.00, '/images/toppings/beef-slices.jpg'),
    ('Shrimp', 4.50, '/images/toppings/shrimp.jpg'),
    ('Mushrooms', 2.00, '/images/toppings/mushrooms.jpg'),
    ('Bean Sprouts', 1.00, '/images/toppings/bean-sprouts.jpg'),
    ('Green Onions', 0.50, '/images/toppings/green-onions.jpg'),
    ('Corn', 1.50, '/images/toppings/corn.jpg'),
    ('Cheese', 2.00, '/images/toppings/cheese.jpg'),
    ('Seaweed', 1.50, '/images/toppings/seaweed.jpg'),
    ('Bamboo Shoots', 2.00, '/images/toppings/bamboo-shoots.jpg');

    -- Seed SpicyNoodles
    INSERT INTO SpicyNoodles (Name, BasePrice, ImageUrl, Description) VALUES
    ('Classic Spicy Ramen', 8.99, '/images/noodles/classic-spicy-ramen.jpg', 'Traditional spicy ramen with rich broth and tender noodles'),
    ('Korean Fire Noodles', 9.99, '/images/noodles/korean-fire-noodles.jpg', 'Extremely spicy Korean-style instant noodles that will test your limits'),
    ('Thai Tom Yum Noodles', 10.49, '/images/noodles/thai-tom-yum.jpg', 'Aromatic Thai soup noodles with lemongrass and chili'),
    ('Sichuan Mala Noodles', 11.99, '/images/noodles/sichuan-mala.jpg', 'Numbing and spicy Sichuan noodles with signature mala sauce'),
    ('Japanese Tantanmen', 10.99, '/images/noodles/japanese-tantanmen.jpg', 'Japanese-style spicy sesame noodle soup'),
    ('Vietnamese Bun Bo Hue', 9.49, '/images/noodles/bun-bo-hue.jpg', 'Spicy Vietnamese beef noodle soup from Hue'),
    ('Indian Curry Noodles', 8.99, '/images/noodles/indian-curry-noodles.jpg', 'Fusion noodles with aromatic Indian curry spices'),
    ('Mexican Chipotle Noodles', 9.49, '/images/noodles/mexican-chipotle.jpg', 'Smoky chipotle-flavored noodles with a kick');

    -- Seed Orders
    INSERT INTO Orders (UserId, Status, TotalAmount) VALUES
    (1, 'Completed', 15.49),
    (2, 'Completed', 22.98),
    (3, 'Pending', 18.99),
    (4, 'In Progress', 13.49),
    (5, 'Completed', 25.47);

    -- Seed OrderItems
    INSERT INTO OrderItems (OrderId, NoodleId, Quantity, Subtotal) VALUES
    (1, 1, 1, 8.99),   -- Classic Spicy Ramen
    (1, 3, 1, 10.49),  -- Thai Tom Yum Noodles (but will be adjusted with toppings)
    (2, 2, 2, 19.98),  -- Korean Fire Noodles x2
    (2, 5, 1, 10.99),  -- Japanese Tantanmen
    (3, 4, 1, 11.99),  -- Sichuan Mala Noodles
    (4, 6, 1, 9.49),   -- Vietnamese Bun Bo Hue
    (5, 7, 2, 17.98),  -- Indian Curry Noodles x2
    (5, 8, 1, 9.49);   -- Mexican Chipotle Noodles

    -- Seed NoodleToppings (Some orders with toppings)
    INSERT INTO NoodleToppings (NoodleId, ToppingId, OrderItemId) VALUES
    (1, 2, 1),  -- Classic Ramen + Fried Egg
    (1, 8, 1),  -- Classic Ramen + Green Onions
    (3, 3, 2),  -- Thai Tom Yum + Grilled Chicken
    (3, 9, 2),  -- Thai Tom Yum + Corn
    (2, 4, 3),  -- Korean Fire + Beef Slices
    (4, 6, 5),  -- Sichuan Mala + Mushrooms
    (6, 7, 6),  -- Bun Bo Hue + Bean Sprouts
    (7, 5, 7),  -- Indian Curry + Shrimp
    (8, 10, 8); -- Mexican Chipotle + Cheese

    -- Seed NoodleSpiceLevels
    INSERT INTO NoodleSpiceLevels (NoodleId, SpiceLevelId, OrderItemId) VALUES
    (1, 2, 1),  -- Classic Ramen - Medium
    (3, 3, 2),  -- Thai Tom Yum - Hot
    (2, 5, 3),  -- Korean Fire - Extreme
    (2, 5, 4),  -- Korean Fire - Extreme
    (5, 3, 4),  -- Japanese Tantanmen - Hot
    (4, 6, 5),  -- Sichuan Mala - Ghost Pepper
    (6, 4, 6),  -- Bun Bo Hue - Very Hot
    (7, 2, 7),  -- Indian Curry - Medium
    (7, 2, 8),  -- Indian Curry - Medium
    (8, 3, 8);  -- Mexican Chipotle - Hot

    -- Update Order totals based on items + toppings
    UPDATE Orders 
    SET TotalAmount = (
        SELECT SUM(oi.Subtotal) + COALESCE(SUM(t.Price), 0)
        FROM OrderItems oi
        LEFT JOIN NoodleToppings nt ON oi.OrderItemId = nt.OrderItemId
        LEFT JOIN Toppings t ON nt.ToppingId = t.ToppingId
        WHERE oi.OrderId = Orders.OrderId
        GROUP BY oi.OrderId
    );
    GO
    """;


var spicyNoodleDbUser = sql.AddDatabase(databaseNameUser)
    .WithCreationScript(creationScriptUser);

var spicyNoodleDbPayment = sql.AddDatabase(databaseNamePayment);

var spicyNoodleDbOrder = sql.AddDatabase(databaseNameOrder);



builder.AddProject<Projects.ApiGateway>("apigateway");

builder.AddProject<Projects.UserService>("user-service")
    .WithReference(spicyNoodleDbUser)
       .WaitFor(spicyNoodleDbUser);

var orderService = builder.AddProject<Projects.OrderService>("order-service")
    .WithReference(spicyNoodleDbOrder)
    .WaitFor(spicyNoodleDbOrder);

builder.AddProject<Projects.PaymentService>("payment-service")
    .WithReference(spicyNoodleDbPayment)
    .WithReference(orderService)
    .WaitFor(spicyNoodleDbPayment);


builder.Build().Run();
