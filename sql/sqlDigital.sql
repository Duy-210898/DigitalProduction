-- Check if the database exists
USE CuttingProjectData;
GO
-- Table: Department
CREATE TABLE Department (
    DepartmentID INT PRIMARY KEY IDENTITY(1,1),
    DepartmentName VARCHAR(100),
    IsActive BIT
);
GO
CREATE TABLE Position (
    PositionID INT PRIMARY KEY IDENTITY(1,1),
    PositionName VARCHAR(100)
);
GO

-- Table: Product
CREATE TABLE Product (
    ProductId INT PRIMARY KEY IDENTITY(1,1),
    ART VARCHAR(255),
    Model VARCHAR(255)
);
GO

-- Table: Material
CREATE TABLE Material (
    MaterialID INT PRIMARY KEY IDENTITY(1,1),
    MaterialCode VARCHAR(50),
    MaterialName VARCHAR(1000),
    Unit VARCHAR(20)
);
GO
-- Table: Order
CREATE TABLE ProductOrder (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    ProductId INT,
    Factory VARCHAR(10),
    SO VARCHAR(100),
    PO VARCHAR(100),
    MasterWorkOrder VARCHAR(100),
    LastNo VARCHAR(20),
    Process VARCHAR(20),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME
);
GO
-- Table: Size
CREATE TABLE Size (
    SizeID INT PRIMARY KEY IDENTITY(1,1),
    Size VARCHAR(10)
);
GO
-- Table: Part
CREATE TABLE Part (
    PartId INT PRIMARY KEY IDENTITY(1,1),
    SizeId INT,
    PartName VARCHAR(255),
    PartCode NVARCHAR(255)
);
GO

-- Table: Users
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    DepartmentID INT,
	PositionID INT,
    Username VARCHAR(50),
    Password VARCHAR(100),
    EmployeeName NVARCHAR(100),
    EmployeeID INT,
    CreatedAt DATETIME,
    UpdatedAt DATETIME,
    IsActive BIT
);
GO

-- Table: Operator
CREATE TABLE Operator (
    OperatorID INT PRIMARY KEY IDENTITY(1,1),
    DepartmentID INT,
	PositionID INT,
    OperatorName NVARCHAR(100),
    EmployeeID INT,
    IsActive BIT
);
GO

-- Table: DeviceList
CREATE TABLE DeviceList (
    DeviceID INT PRIMARY KEY IDENTITY(1,1),
    DepartmentID INT,
	PlantID INT,
    IpAddress VARCHAR(50),
    MachineName VARCHAR(100),
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsActive BIT DEFAULT 0,
    ConnectionStatus BIT,
);
GO
CREATE TABLE Plant (
    PlantID INT PRIMARY KEY IDENTITY(1,1),
    PlantName NVARCHAR(100) NOT NULL
);
GO
-- Table: DeviceOutput
CREATE TABLE DeviceOutput (
    OutputID INT PRIMARY KEY IDENTITY(1,1),
	SizeID INT,
	OrderID INT,
	IsLeather INT,
    PiecesPerPair INT,
    MaterialLayer INT,
    CuttingDieQty INT,
    ActualCut INT,
    ActualPieces INT,
    ActualSizeQty INT,
    InventoryQty INT,
	TotalPiecesPerPair INT,
	CreatedAt DATETIME,
    UpdatedAt DATETIME
);
GO

-- Table: DistributionData
CREATE TABLE DistributionData (
    DistributionID INT PRIMARY KEY IDENTITY(1,1),
	UserID INT,
    DeviceID INT,
    PartSizeOrderId INT,
    OperatorID INT,
    InventoryQty INT,
    Status VARCHAR(50) DEFAULT 'Pending',
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsLeather BIT,
    IsDelete BIT,
	Note INT
);


GO
-- Table: ProductionSchedule
CREATE TABLE ProductionSchedule (
    ScheduleID INT PRIMARY KEY IDENTITY(1,1),
    Factory VARCHAR(50),
    ART VARCHAR(50),
    Model VARCHAR(100),
    PO VARCHAR(50),
    SO VARCHAR(50),
    MasterWorkOrder VARCHAR(50),
    Size VARCHAR(10),
    PartCode VARCHAR(50),
    PartName VARCHAR(100),
    MaterialCode VARCHAR(50),
    MaterialName VARCHAR(1000),
    SizeQty INT,
    UNIT VARCHAR(10),
    ProductionProcess VARCHAR(50),
    Page VARCHAR(10),
    LastNo VARCHAR(50),
    UnitUsage FLOAT
);
GO

-- Table: PartSizeOrder
CREATE TABLE PartSizeOrder (
    PartSizeOrderId INT PRIMARY KEY IDENTITY(1,1),
    PartId INT,
    SizeId INT,
    OrderId INT,
	SizeQty INT,
	Unit VARCHAR(50),
	UnitUsage FLOAT
);
GO

-- Table: DefaultInfo
CREATE TABLE DefaultInfo (
    DefaultID INT PRIMARY KEY IDENTITY(1,1),
    ProductID INT,
    PiecesPerPair INT,
	TotalPiecesPerPair INT,
    CuttingDieQty INT,
    MaterialLayer INT
);
GO

-- Foreign Key Constraints
ALTER TABLE DeviceOutput
	ADD CONSTRAINT FK_DeviceOutput_Size FOREIGN KEY (SizeID) REFERENCES Size(SizeID),
	  CONSTRAINT FK_DeviceOutput_ProductOrder FOREIGN KEY (OrderID) REFERENCES ProductOrder(OrderID);
GO
ALTER TABLE DefaultInfo
    ADD CONSTRAINT FK_DefaultInfo_Product FOREIGN KEY (ProductID) REFERENCES Product(ProductID);
GO
ALTER TABLE DistributionData
	ADD CONSTRAINT FK_DistributionData_Users FOREIGN KEY (UserID) REFERENCES Users(UserID),
    CONSTRAINT FK_DistributionData_Device FOREIGN KEY (DeviceID) REFERENCES DeviceList(DeviceID),
    CONSTRAINT FK_DistributionData_PartSizeOrder FOREIGN KEY (PartSizeOrderId) REFERENCES PartSizeOrder(PartSizeOrderId),
    CONSTRAINT FK_DistributionData_Operator FOREIGN KEY (OperatorID) REFERENCES Operator(OperatorID);
GO
ALTER TABLE Operator
    ADD CONSTRAINT FK_Operator_Department FOREIGN KEY (DepartmentID) REFERENCES Department(DepartmentID),
	CONSTRAINT FK_Operator_Position FOREIGN KEY (PositionID) REFERENCES  Position(PositionID);
GO
ALTER TABLE Users
    ADD CONSTRAINT FK_Users_Department FOREIGN KEY (DepartmentID) REFERENCES Department(DepartmentID),
	CONSTRAINT FK_User_Position FOREIGN KEY (PositionID) REFERENCES  Position(PositionID);
GO
ALTER TABLE DeviceList
    ADD CONSTRAINT FK_DeviceList_Department FOREIGN KEY (DepartmentID) REFERENCES  Department(DepartmentID),
	CONSTRAINT FK_DeviceList_Plant FOREIGN KEY (PlantID) REFERENCES Plant(PlantID) ON DELETE CASCADE;
GO
/*ALTER TABLE Size
    ADD CONSTRAINT FK_Size_ProductOrder FOREIGN KEY (OrderID) REFERENCES ProductOrder(OrderID);
GO
ALTER TABLE Part
    ADD CONSTRAINT FK_Part_Size FOREIGN KEY (SizeId) REFERENCES Size(SizeID);
GO */
ALTER TABLE ProductOrder
    ADD CONSTRAINT FK_Order_Product FOREIGN KEY (ProductId) REFERENCES Product(ProductId);
GO
ALTER TABLE PartSizeOrder
    ADD CONSTRAINT FK_PartSizeOrder_Part FOREIGN KEY (PartId) REFERENCES Part(PartId),
	CONSTRAINT FK_PartSizeOrder_Size FOREIGN KEY (SizeId) REFERENCES Size(SizeId),
	CONSTRAINT FK_PartSizeOrder_ProductOrder FOREIGN KEY (OrderID) REFERENCES ProductOrder(OrderID),
	CONSTRAINT FK_PartSizeOrder_Material FOREIGN KEY (MaterialId) REFERENCES Material(MaterialID);
GO

-- Strore Procuduce
CREATE PROCEDURE sp_LoginUser
    @Username VARCHAR(50),
    @Password VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    -- Check for a matching user record (user must be active)
    SELECT UserID, EmployeeName, PositionID, DepartmentID 
    FROM Users 
    WHERE Username = @Username 
      AND Password = @Password 
      AND IsActive = 1;
END
GO
CREATE PROCEDURE RegisterUser
    @Username VARCHAR(50),
    @PasswordHash VARCHAR(64),  -- Hashed password
    @EmployeeName NVARCHAR(100),
	@EmployeeID INT,
    @DepartmentID INT,
    @Position NVARCHAR(100),
    @IsActive BIT
AS
BEGIN
    INSERT INTO Users (Username, Password, EmployeeName, EmployeeID, DepartmentID, Position, IsActive, CreatedAt)
    VALUES (@Username, @PasswordHash, @EmployeeName, @EmployeeID, @DepartmentID, @Position, @IsActive, GETDATE());
END;
GO
CREATE PROCEDURE sp_UpdateUser
    @Username VARCHAR(50),  
    @NewEmployeeID INT,
    @NewEmployeeName NVARCHAR(100),
    @NewDepartmentID INT,
    @NewPositionID INT,
    @NewIsActive BIT,
    @UpdateStatus INT OUTPUT -- Added OUTPUT parameter
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if the employee exists before updating
    IF EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
    BEGIN
        -- Perform the update
        UPDATE Users
        SET 
            EmployeeID = @NewEmployeeID,
            EmployeeName = @NewEmployeeName,
            DepartmentID = @NewDepartmentID,
            PositionID = @NewPositionID,
            IsActive = @NewIsActive,
            UpdatedAt = GETDATE()
        WHERE Username = @Username;

        -- Set success status
        SET @UpdateStatus = 1;
    END
    ELSE
    BEGIN
        -- If the username does not exist, set failure status
        SET @UpdateStatus = 0;
    END
END;

SELECT * FROM Users WHERE Username = 'nhatboy'
SELECT * FROM DeviceList WHERE IsActive = 1
SELECT * FROM DistributionData
SELECT * FROM PartSizeOrder
SELECT * FROM ProductOrder
SELECT * FROM Part
SELECT * FROM Size


SELECT PartSizeOrderId FROM PartSizeOrder WHERE PartId = 136 AND SizeId = 76 AND OrderId = 63;
delete DistributionData

SELECT 
    dd.DistributionID,
    d.IpAddress,
	d.MachineName,
    pa.PartName,
	se.Size,
    o.OperatorName,  
    dd.InventoryQty,
    dd.Status,
    dd.CreatedAt,
    dd.IsLeather,
    dd.IsDelete
FROM 
    DistributionData dd
JOIN 
    DeviceList d ON dd.DeviceID = d.DeviceID 
JOIN 
    PartSizeOrder ps ON dd.PartSizeOrderId = ps.PartSizeOrderId  
JOIN 
    Part pa ON pa.PartID = ps.PartID
JOIN 
    Size se ON se.SizeID= ps.SizeID
JOIN 
    Operator o ON dd.OperatorID = o.OperatorID  
WHERE 
    dd.IsDelete = 0;
