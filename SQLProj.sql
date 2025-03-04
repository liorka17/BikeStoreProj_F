SET NOCOUNT ON;  
-- מונע הצגת הודעות על מספר השורות שהושפעו מכל פעולה. זה משפר ביצועים ומונע עומס מיותר בלוג.

GO  

USE master;  
-- מעביר את ההקשר למסד הנתונים הראשי (master), כך שניתן יהיה לבצע שינויים במאגרי נתונים אחרים.

GO  

-- סגירת כל החיבורים למסד הנתונים ומחיקתו אם קיים
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'BikeStore')  
-- בודק אם מסד הנתונים בשם 'BikeStore' קיים.

BEGIN  
    ALTER DATABASE BikeStore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;  
    -- מעביר את מסד הנתונים למצב של משתמש יחיד (SINGLE_USER) ומבטל מיד את כל העסקאות הפתוחות, כך שאפשר למחוק אותו.

    DROP DATABASE BikeStore;  
    -- מוחק את מסד הנתונים 'BikeStore'.
END  

GO  


-- יצירת מסד הנתונים
DECLARE @device_directory NVARCHAR(520);  
-- משתנה לאחסון נתיב הקובץ שבו ישמרו קובצי מסד הנתונים.

SELECT @device_directory = SUBSTRING(filename, 1, CHARINDEX(N'master.mdf', LOWER(filename)) - 1)  
FROM master.dbo.sysaltfiles WHERE dbid = 1 AND fileid = 1;
-- מאתר את מיקום הקובץ של מסד הנתונים הראשי (master.mdf) ושומר את נתיב התיקייה בלבד בתוך המשתנה @device_directory.

EXECUTE (N'CREATE DATABASE BikeStore  
  ON PRIMARY (NAME = N''BikeStore'', FILENAME = N''' + @device_directory + N'BikeStore.mdf'')  
  LOG ON (NAME = N''BikeStore_log'', FILENAME = N''' + @device_directory + N'BikeStore.ldf'')');
-- יוצר את מסד הנתונים "BikeStore" עם:
-- - קובץ נתונים ראשי (BikeStore.mdf) בתיקייה שזוהתה קודם.
-- - קובץ לוג (BikeStore.ldf) באותו נתיב.

GO  

USE BikeStore;  
-- משנה את ההקשר למסד הנתונים החדש שנוצר "BikeStore", כך שכל הפקודות הבאות יבוצעו בתוכו.

GO  


-- מחיקת הטבלאות אם קיימות
IF OBJECT_ID('dbo.SupplyTransactions', 'U') IS NOT NULL DROP TABLE dbo.SupplyTransactions;
-- בודק אם קיימת טבלה בשם 'SupplyTransactions' בסכימה 'dbo', ואם כן - מוחק אותה.
IF OBJECT_ID('dbo.BikeTypes', 'U') IS NOT NULL DROP TABLE dbo.BikeTypes;
-- בודק אם קיימת טבלה בשם 'BikeTypes' ואם כן - מוחק אותה.
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL DROP TABLE dbo.Suppliers;
-- בודק אם קיימת טבלה בשם 'Suppliers' ואם כן - מוחק אותה.
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
-- בודק אם קיימת טבלה בשם 'Customers' ואם כן - מוחק אותה.
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
-- בודק אם קיימת טבלה בשם 'Orders' ואם כן - מוחק אותה.
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
-- בודק אם קיימת טבלה בשם 'OrderDetails' ואם כן - מוחק אותה.
-- מחיקת הטבלאות במידה והן קיימות כדי להימנע משגיאות בהמשך יצירת המבנה החדש.

GO  


-- יצירת טבלת Suppliers
CREATE TABLE dbo.Suppliers (
    SupplierID INT IDENTITY(1, 1) NOT NULL, 
    -- עמודת מזהה ייחודית לכל ספק, עם ערך שמתווסף אוטומטית (מספר רץ החל מ-1).
    SupplierName NVARCHAR(100) NOT NULL,   
    -- שם הספק, חייב להיות מוזן (לא ניתן להשאיר ריק).
    ContactName NVARCHAR(50) NOT NULL,    
    -- שם איש הקשר של הספק, חובה להזין ערך.
    Email NVARCHAR(100) NOT NULL,         
    -- כתובת אימייל של הספק, חובה להזין ערך.
    PhoneNumber NVARCHAR(15) NOT NULL,    
    -- מספר טלפון של הספק, חובה להזין ערך.
    Address NVARCHAR(250) NOT NULL,       
    -- כתובת הספק, חובה להזין ערך.
    CONSTRAINT PK_SupplierID PRIMARY KEY CLUSTERED (SupplierID) 
    -- הגדרת מפתח ראשי עבור הטבלה על עמודת SupplierID.
    -- שימוש ב-CLUSTERED אומר שהנתונים יאוחסנו פיזית בסדר עולה לפי המפתח הראשי.
);
GO

-- הוספת אינדקסים לטבלת Suppliers
CREATE INDEX IX_SupplierName ON dbo.Suppliers(SupplierName);  
-- יצירת אינדקס על עמודת SupplierName כדי לזרז חיפושים וסינונים לפי שם הספק.
CREATE INDEX IX_ContactName ON dbo.Suppliers(ContactName);  
-- יצירת אינדקס על עמודת ContactName כדי לזרז חיפושים וסינונים לפי שם איש הקשר.
GO


-- יצירת טבלת BikeTypes
CREATE TABLE dbo.BikeTypes (
    BikeID INT IDENTITY(1, 1) NOT NULL,   
    -- מזהה ייחודי לכל סוג אופניים. הערך מתעדכן אוטומטית ומתחיל מ-1.
    BikeSize NVARCHAR(15) NOT NULL,      
    -- גודל האופניים (לדוגמה: Small, Medium, Large). חובה להזין ערך.
    Color NVARCHAR(15) NOT NULL,         
    -- צבע האופניים (לדוגמה: אדום, כחול, שחור). חובה להזין ערך.
    Type NVARCHAR(15) NOT NULL,          
    -- סוג האופניים (לדוגמה: הרים, כביש, עירוני). חובה להזין ערך.
    StockQuantity INT NOT NULL DEFAULT 0,
    -- כמות האופניים במלאי, ערך ברירת המחדל הוא 0.
    SalePrice DECIMAL(18, 2) NOT NULL DEFAULT 0.00, 
    -- מחיר המכירה של האופניים, ערך ברירת המחדל הוא 0.00.
    SupplierID INT NOT NULL,             
    -- מזהה הספק (SupplierID), קישור לספק שסיפק את האופניים. חובה להזין ערך.
    CONSTRAINT PK_BikeID PRIMARY KEY CLUSTERED (BikeID), 
    -- הגדרת מפתח ראשי על עמודת BikeID, אשר יגדיר את הסדר הפיזי של הנתונים בטבלה.
    CONSTRAINT FK_Bike_Supplier FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers(SupplierID) 
    -- הגדרת מפתח זר שמקשר בין BikeTypes לטבלת Suppliers באמצעות עמודת SupplierID.
);
GO


-- הוספת אינדקסים לטבלת BikeTypes
CREATE INDEX IX_BikeType ON dbo.BikeTypes(Type);  
-- יצירת אינדקס על עמודת Type (סוג האופניים) כדי לזרז חיפושים וסינונים לפי סוג האופניים.
CREATE INDEX IX_BikeColor ON dbo.BikeTypes(Color);  
-- יצירת אינדקס על עמודת Color (צבע האופניים) כדי לזרז חיפושים וסינונים לפי צבע האופניים.
GO


-- יצירת טבלת SupplyTransactions
CREATE TABLE dbo.SupplyTransactions (
    SupplyID INT IDENTITY(1, 1) NOT NULL, 
    -- מזהה ייחודי לכל עסקת אספקה. הערך מתעדכן אוטומטית ומתחיל מ-1.
    SupplierID INT NOT NULL,              
    -- מזהה הספק שסיפק את האופניים. חייב להכיל ערך ונקשר לטבלת Suppliers.
    BikeID INT NOT NULL,                  
    -- מזהה סוג האופניים שסופקו בעסקה זו. חייב להכיל ערך ונקשר לטבלת BikeTypes.
    Quantity INT NOT NULL,                
    -- כמות האופניים שסופקו בעסקה זו. חובה להזין ערך.
    SupplyDate DATETIME NOT NULL DEFAULT GETDATE(), 
    -- תאריך האספקה של האופניים, ערך ברירת המחדל הוא תאריך ושעה נוכחיים.
    CONSTRAINT PK_SupplyID PRIMARY KEY (SupplyID), 
    -- הגדרת SupplyID כמפתח ראשי שמבטיח ייחודיות לכל עסקת אספקה.
    CONSTRAINT FK_Supply_Supplier FOREIGN KEY (SupplierID) REFERENCES dbo.Suppliers(SupplierID), 
    -- הגדרת מפתח זר המקשר בין SupplyTransactions לטבלת Suppliers באמצעות SupplierID.
    CONSTRAINT FK_Supply_Bike FOREIGN KEY (BikeID) REFERENCES dbo.BikeTypes(BikeID) 
    -- הגדרת מפתח זר המקשר בין SupplyTransactions לטבלת BikeTypes באמצעות BikeID.
);

GO

-- הוספת אינדקסים לטבלת SupplyTransactions
CREATE INDEX IX_SupplyDate ON dbo.SupplyTransactions(SupplyDate);  
-- יצירת אינדקס על עמודת SupplyDate כדי להאיץ חיפושים וסינונים לפי תאריך האספקה.
CREATE INDEX IX_SupplierID_BikeID ON dbo.SupplyTransactions(SupplierID, BikeID);  
-- יצירת אינדקס משולב על SupplierID ו-BikeID כדי להאיץ שאילתות שמבצעות חיפושים או צירופים (JOIN) על פי שני השדות יחד.

GO


-- יצירת טבלת Customers
CREATE TABLE dbo.Customers (
    CustomerID INT IDENTITY(1, 1) NOT NULL, 
    -- מזהה ייחודי לכל לקוח. הערך מתעדכן אוטומטית ומתחיל מ-1.
    FirstName NVARCHAR(50) NOT NULL,       
    -- שם פרטי של הלקוח. חובה להזין ערך.
    LastName NVARCHAR(50) NOT NULL,        
    -- שם משפחה של הלקוח. חובה להזין ערך.
    Email NVARCHAR(100) NOT NULL,          
    -- כתובת אימייל של הלקוח. חובה להזין ערך.
    PhoneNumber NVARCHAR(15) NOT NULL,     
    -- מספר טלפון של הלקוח. חובה להזין ערך.
    Address NVARCHAR(250) NOT NULL,        
    -- כתובת הלקוח. חובה להזין ערך.
    CONSTRAINT PK_CustomerID PRIMARY KEY CLUSTERED (CustomerID), 
    -- הגדרת מפתח ראשי על עמודת CustomerID כדי להבטיח ייחודיות ולארגן את הנתונים פיזית לפי מזהה הלקוח.
    CONSTRAINT UQ_Customers_PhoneNumber UNIQUE (PhoneNumber) 
    -- הגדרת מגבלת ייחודיות (UNIQUE) על PhoneNumber כדי למנוע רישום כפול של מספרי טלפון.
);
GO


-- הסרת כפילויות קיימות לפי PhoneNumber
WITH CTE AS (
    SELECT 
        CustomerID, 
        PhoneNumber, 
        ROW_NUMBER() OVER (PARTITION BY PhoneNumber ORDER BY CustomerID) AS RowNum
    -- יוצר CTE (טבלה זמנית) שמוסיפה מספר שורה (`RowNum`) לכל לקוח לפי `PhoneNumber`.
    -- אם קיימים מספר לקוחות עם אותו `PhoneNumber`, הם מקבלים מספר רץ לפי `CustomerID`.
    -- `PARTITION BY PhoneNumber` מחלק את הרשומות לפי מספר טלפון כך שכל טלפון יקבל קבוצת מספרים משלו.
    -- `ORDER BY CustomerID` מבטיח שהרשומה עם ה-CustomerID הנמוך ביותר תישאר, והשאר ימחקו.
    
    FROM Customers
)

DELETE FROM Customers
WHERE CustomerID IN (
    SELECT CustomerID 
    FROM CTE
    WHERE RowNum > 1
);
-- מוחק את כל הרשומות שבהן `RowNum > 1`, כלומר כל הלקוחות פרט לרשומה הראשונה שנמצאה עבור כל `PhoneNumber`.

GO

GO

-- הוספת אינדקסים לטבלת Customers
CREATE INDEX IX_CustomerEmail ON dbo.Customers(Email);  
-- יצירת אינדקס על עמודת Email כדי לשפר ביצועים בחיפושים לפי אימייל (WHERE Email = ...).
CREATE INDEX IX_CustomerPhoneNumber ON dbo.Customers(PhoneNumber);  
-- יצירת אינדקס על עמודת PhoneNumber כדי להאיץ חיפושים וסינונים לפי מספר טלפון.

GO


-- יצירת טבלת Orders
CREATE TABLE dbo.Orders (
    OrderID INT IDENTITY(1, 1) NOT NULL, 
    -- מזהה ייחודי לכל הזמנה. הערך מתעדכן אוטומטית ומתחיל מ-1.
    CustomerID INT NOT NULL,             
    -- מזהה הלקוח שהזמין (מפתח זר המקשר לטבלת Customers). חובה להזין ערך.
    OrderDate DATETIME NOT NULL,         
    -- תאריך ההזמנה. חובה להזין ערך.
    TotalAmount DECIMAL(18, 2) NOT NULL, 
    -- הסכום הכולל של ההזמנה. חייב להיות מספר עם 18 ספרות, מתוכן 2 אחרי הנקודה.
    CONSTRAINT PK_OrderID PRIMARY KEY CLUSTERED (OrderID), 
    -- הגדרת מפתח ראשי על OrderID, כך שהנתונים יאוחסנו פיזית בסדר עולה לפי מזהה ההזמנה.
    CONSTRAINT FK_Order_Customer FOREIGN KEY (CustomerID) REFERENCES dbo.Customers(CustomerID) 
    -- הגדרת מפתח זר שמקשר בין Orders לטבלת Customers באמצעות CustomerID.
);

GO

-- הוספת אינדקסים לטבלת Orders
CREATE INDEX IX_OrderDate ON dbo.Orders(OrderDate);  
-- יצירת אינדקס על עמודת OrderDate כדי לשפר ביצועים בשאילתות שמבצעות חיפושים וסינונים לפי תאריך ההזמנה.
CREATE INDEX IX_CustomerID ON dbo.Orders(CustomerID);  
-- יצירת אינדקס על עמודת CustomerID כדי לשפר ביצועים בחיפושים לפי לקוח וכן בצירופים (JOIN) עם טבלת Customers.
GO

-- יצירת טבלת OrderDetails
CREATE TABLE dbo.OrderDetails (
    OrderDetailID INT IDENTITY(1, 1) NOT NULL,  
    -- מזהה ייחודי לכל שורת פרטי הזמנה, מספר רץ שמתחיל מ-1.
    OrderID INT NOT NULL,                      
    -- מזהה ההזמנה (מפתח זר המקשר לטבלת Orders). חובה להזין ערך.
    BikeID INT NOT NULL,                       
    -- מזהה סוג האופניים שנרכשו (מפתח זר המקשר לטבלת BikeTypes). חובה להזין ערך.
    Quantity INT NOT NULL,                     
    -- כמות האופניים שהוזמנו בשורה זו. חובה להזין ערך.
    UnitPrice DECIMAL(18, 2) NOT NULL,         
    -- מחיר ליחידה של סוג האופניים בהזמנה. חובה להזין ערך.
    TotalPrice AS (Quantity * UnitPrice) PERSISTED, 
    -- עמודה מחושבת ששומרת את הסכום הכולל של שורת ההזמנה (`Quantity * UnitPrice`).
    -- `PERSISTED` אומר שהערך מחושב ונשמר בטבלה, כך שאינו מחושב מחדש בכל שאילתה.
    CONSTRAINT PK_OrderDetailID PRIMARY KEY CLUSTERED (OrderDetailID),  
    -- הגדרת מפתח ראשי שמבטיח ייחודיות לכל שורת פרטי הזמנה.
    CONSTRAINT FK_Order_OrderDetail FOREIGN KEY (OrderID) REFERENCES dbo.Orders(OrderID),  
    -- הגדרת מפתח זר שמקשר בין OrderDetails לטבלת Orders באמצעות OrderID.
    CONSTRAINT FK_Bike_OrderDetail FOREIGN KEY (BikeID) REFERENCES dbo.BikeTypes(BikeID)  
    -- הגדרת מפתח זר שמקשר בין OrderDetails לטבלת BikeTypes באמצעות BikeID.
);

GO


-- הוספת אינדקסים לטבלת OrderDetails
CREATE INDEX IX_OrderID ON dbo.OrderDetails(OrderID);  
-- יצירת אינדקס על עמודת OrderID כדי לשפר ביצועים בצירופים (JOINs) עם טבלת Orders ובחיפושים לפי מזהה ההזמנה.
CREATE INDEX IX_BikeID ON dbo.OrderDetails(BikeID);  
-- יצירת אינדקס על עמודת BikeID כדי לשפר ביצועים בצירופים (JOINs) עם טבלת BikeTypes ובחיפושים לפי מזהה סוג האופניים.
GO

-- הכנסת נתונים לדוגמה לטבלת Suppliers
INSERT INTO dbo.Suppliers (SupplierName, ContactName, Email, PhoneNumber, Address)
VALUES ('China Supplier', 'Li Wang', 'china.supplier@example.com', '972-52-1234567', 'Beijing, China'),
       ('Israel Supplier', 'David Cohen', 'israel.supplier@example.com', '972-50-9876543', 'Tel Aviv, Israel'),
       ('USA Supplier', 'John Smith', 'usa.supplier@example.com', '972-52-5555555', 'New York, USA');
GO

INSERT INTO dbo.BikeTypes (BikeSize, Color, Type, StockQuantity, SalePrice, SupplierID)
VALUES 
    -- אופניים בגודל 29 Inch
    ('29 Inch', 'Red', 'Racing Bikes', 30, 1500.00, 1),
	('29 Inch', 'Black', 'Racing Bikes', 30, 1500.00, 1),
	('29 Inch', 'Blue', 'Racing Bikes', 30, 1500.00, 1),
	('14 Inch', 'Red', 'Racing Bikes', 30, 1500.00, 1),
	('14 Inch', 'Black', 'Racing Bikes', 30, 1500.00, 1),
	('14 Inch', 'Blue', 'Racing Bikes', 30, 1500.00, 1),
	('18 Inch', 'Red', 'Racing Bikes', 30, 1500.00, 1),
	('18 Inch', 'Black', 'Racing Bikes', 30, 1500.00, 1),
	('18 Inch', 'Blue', 'Racing Bikes', 30, 1500.00, 1),
	('26 Inch', 'Red', 'Racing Bikes', 30, 1500.00, 1),
	('26 Inch', 'Black', 'Racing Bikes', 30, 1500.00, 1),
	('26 Inch', 'Blue', 'Racing Bikes', 30, 1500.00, 1),


    -- road bikes stock
    ('29 Inch', 'Red', 'Road Bikes', 30, 1500.00, 2),
	('29 Inch', 'Black', 'Road Bikes', 30, 1500.00, 2),
	('29 Inch', 'Blue', 'Road Bikes', 30, 1500.00, 2),
	('14 Inch', 'Red', 'Road Bikes', 30, 1500.00, 2),
	('14 Inch', 'Black', 'Road Bikes', 30, 1500.00, 2),
	('14 Inch', 'Blue', 'Road Bikes', 30, 1500.00, 2),
	('18 Inch', 'Red', 'Road Bikes', 30, 1500.00, 2),
	('18 Inch', 'Black', 'Road Bikes', 30, 1500.00, 2),
	('18 Inch', 'Blue', 'Road Bikes', 30, 1500.00, 2),
	('26 Inch', 'Red', 'Road Bikes', 30, 1500.00, 2),
	('26 Inch', 'Black', 'Road Bikes', 30, 1500.00, 2),
	('26 Inch', 'Blue', 'Road Bikes', 30, 1500.00, 2),

    -- kids bike stock
    ('14 Inch', 'Blue', 'Kids Bikes', 20, 900.00, 3),
    ('29 Inch', 'Red', 'Kids Bikes', 30, 1500.00, 3),
	('29 Inch', 'Black', 'Kids Bikes', 30, 1500.00, 3),
	('29 Inch', 'Blue', 'Kids Bikes', 30, 1500.00, 3),
	('14 Inch', 'Red', 'Kids Bikes', 30, 1500.00, 3),
	('14 Inch', 'Black', 'Kids Bikes', 30, 1500.00, 3),
	('14 Inch', 'Blue', 'Kids Bikes', 30, 1500.00, 3),
	('18 Inch', 'Red', 'Kids Bikes', 30, 1500.00, 3),
	('18 Inch', 'Black', 'Kids Bikes', 30, 1500.00, 3),
	('18 Inch', 'Blue', 'Kids Bikes', 30, 1500.00, 3),
	('26 Inch', 'Red', 'Kids Bikes', 30, 1500.00, 3),
	('26 Inch', 'Black', 'Kids Bikes', 30, 1500.00, 3),
	('26 Inch', 'Blue', 'Kids Bikes', 30, 1500.00, 3)

GO


-- הכנסת נתונים לדוגמה לטבלת Customers
INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, Address)
VALUES ('Adam', 'Levi', 'adam.levi@example.com', '972-54-3456789', 'Haifa, Israel'),
       ('Noa', 'Cohen', 'noa.cohen@example.com', '972-52-9876543', 'Jerusalem, Israel'),
       ('Daniel', 'Friedman', 'daniel.friedman@example.com', '972-50-1234567', 'Tel Aviv, Israel');
GO

-- הכנסת 10 הזמנות לשנת 2021
INSERT INTO dbo.Orders (CustomerID, OrderDate, TotalAmount)
VALUES 
(1, '2021-01-05', 1500.00),
(2, '2021-02-12', 1800.00),
(3, '2021-03-15', 2500.00),
(1, '2021-04-20', 1900.00),
(2, '2021-05-25', 1200.00),
(3, '2021-06-30', 2700.00),
(1, '2021-07-18', 2200.00),
(2, '2021-08-15', 1700.00),
(3, '2021-09-20', 2100.00),
(1, '2021-10-25', 3000.00);

-- הכנסת 10 הזמנות לשנת 2022
INSERT INTO dbo.Orders (CustomerID, OrderDate, TotalAmount)
VALUES 
(1, '2022-01-05', 1500.00),
(2, '2022-02-12', 1800.00),
(3, '2022-03-15', 2500.00),
(1, '2022-04-20', 1900.00),
(2, '2022-05-25', 1200.00),
(3, '2022-06-30', 2700.00),
(1, '2022-07-18', 2200.00),
(2, '2022-08-15', 1700.00),
(3, '2022-09-20', 2100.00),
(1, '2022-10-25', 3000.00);

-- הכנסת 10 הזמנות לשנת 2023
INSERT INTO dbo.Orders (CustomerID, OrderDate, TotalAmount)
VALUES 
(1, '2023-01-05', 1500.00),
(2, '2023-02-12', 1800.00),
(3, '2023-03-15', 2500.00),
(1, '2023-04-20', 1900.00),
(2, '2023-05-25', 1200.00),
(3, '2023-06-30', 2700.00),
(1, '2023-07-18', 2200.00),
(2, '2023-08-15', 1700.00),
(3, '2023-09-20', 2100.00),
(1, '2023-10-25', 3000.00);

-- הכנסת 10 הזמנות לשנת 2024
INSERT INTO dbo.Orders (CustomerID, OrderDate, TotalAmount)
VALUES 
(1, '2024-01-05', 1500.00),
(2, '2024-02-12', 1800.00),
(3, '2024-03-15', 2500.00),
(1, '2024-04-20', 1900.00),
(2, '2024-05-25', 1200.00),
(3, '2024-06-30', 2700.00),
(1, '2024-07-18', 2200.00),
(2, '2024-08-15', 1700.00),
(3, '2024-09-20', 2100.00),
(1, '2024-10-25', 3000.00);
GO


-- הכנסת נתונים לדוגמה לטבלת OrderDetails
INSERT INTO dbo.OrderDetails (OrderID, BikeID, Quantity, UnitPrice)
VALUES (1, 1, 2, 1500.00),
       (2, 2, 1, 1200.00),
       (3, 3, 1, 900.00);
GO

-- הכנסת נתונים לדוגמה לטבלת SupplyTransactions
INSERT INTO dbo.SupplyTransactions (SupplierID, BikeID, Quantity)
VALUES (1, 1, 10), (2, 2, 20), (3, 3, 5);
GO

CREATE TRIGGER trg_AutoSupplyOnLowStock
ON dbo.BikeTypes
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- מונע הצגת הודעות על מספר השורות שהושפעו מכל פעולה, כדי לשפר ביצועים.

    -- הוספת רשומות לטבלת SupplyTransactions כאשר המלאי יורד מתחת ל-15
    INSERT INTO dbo.SupplyTransactions (SupplierID, BikeID, Quantity)
    SELECT 
        inserted.SupplierID, 
        inserted.BikeID, 
        20 -- כמות להזמנה (ברירת מחדל)
    FROM inserted
    JOIN deleted
        ON inserted.BikeID = deleted.BikeID
    WHERE 
        inserted.StockQuantity < 15 -- המלאי ירד מתחת ל-15
        AND deleted.StockQuantity >= 15; -- לפני העדכון המלאי היה 15 או יותר

    -- עדכון המלאי בטבלת BikeTypes לאחר ההזמנה האוטומטית
    UPDATE dbo.BikeTypes
    SET StockQuantity = StockQuantity + 20 -- עדכון מלאי עם כמות ההזמנה
    WHERE BikeID IN (
        SELECT BikeID
        FROM inserted
        WHERE StockQuantity < 15
    );
END;
GO
--מתבצע לאחר עדכון (AFTER UPDATE) על BikeTypes:

--הטריגר יופעל רק כאשר מתבצע עדכון לטבלה BikeTypes.
--בודק האם המלאי ירד מתחת ל-15:
--אם לפני העדכון (deleted.StockQuantity) המלאי היה 15 או יותר, אך לאחר העדכון (inserted.StockQuantity) ירד פחות מ-15, אז מתבצע הזמנה אוטומטית.
--מוסיף רשומה חדשה לטבלת SupplyTransactions:
--מזמין 20 יחידות של אותו דגם אופניים (BikeID) מהספק (SupplierID).
--מעודכן המלאי בטבלת BikeTypes:
--לאחר ההזמנה, המלאי מתווסף אוטומטית (StockQuantity + 20).

-- עדכון מלאי לדגם כדי לבדוק את הטריגר
UPDATE dbo.BikeTypes
SET StockQuantity = 10
WHERE BikeID = 3;

-- בדיקה אם התווספה הזמנה לטבלת SupplyTransactions
SELECT * FROM dbo.SupplyTransactions;

-- בדיקה אם המלאי התעדכן בטבלת BikeTypes
SELECT * FROM dbo.BikeTypes WHERE BikeID = 1;

-- בדיקות
SELECT * FROM dbo.Customers;
SELECT * FROM dbo.Orders;
SELECT * FROM dbo.OrderDetails;
SELECT * FROM dbo.BikeTypes;

SELECT * FROM dbo.Suppliers;
SELECT * FROM dbo.SupplyTransactions;
GO

--enable TRIGGER trg_AutoSupplyOnLowStock
--ON dbo.BikeTypes


SELECT * 
FROM dbo.Orders
FOR JSON AUTO;