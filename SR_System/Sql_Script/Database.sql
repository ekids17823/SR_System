-- ================================================================================
-- 檔案：/Database.sql
-- 功能：建立符合新流程的完整資料庫結構。
-- ================================================================================

-- USE [YourDatabaseName];
-- GO

-- 刪除現有資料表 (方便重新執行)
IF OBJECT_ID('dbo.ASE_BPCIM_SR_Action_HIS', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_Action_HIS;
IF OBJECT_ID('dbo.ASE_BPCIM_SR_Approvers_HIS', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_Approvers_HIS;
IF OBJECT_ID('dbo.ASE_BPCIM_SR_HIS', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_HIS;
IF OBJECT_ID('dbo.ASE_BPCIM_SR_Statuses_DEFINE', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_Statuses_DEFINE;
IF OBJECT_ID('dbo.ASE_BPCIM_SR_Users_DEFINE', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_Users_DEFINE;
IF OBJECT_ID('dbo.ASE_BPCIM_SR_CIMLeaders_DEFINE', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_CIMLeaders_DEFINE;
IF OBJECT_ID('dbo.ASE_BPCIM_SR_YellowPages_TEST', 'U') IS NOT NULL DROP TABLE dbo.ASE_BPCIM_SR_YellowPages_TEST;
GO

-- 建立資料表
CREATE TABLE dbo.ASE_BPCIM_SR_YellowPages_TEST (
    EmployeeID NVARCHAR(50) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Department NVARCHAR(50) NULL,
    CIM_Group NVARCHAR(50) NULL,
    Position NVARCHAR(50) NULL,
    ManagerEmployeeID NVARCHAR(50) NULL 
);
GO

CREATE TABLE dbo.ASE_BPCIM_SR_Users_DEFINE (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    EmployeeID NVARCHAR(50) UNIQUE NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_YellowPages_TEST(EmployeeID),
    CreateDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL
);
GO

CREATE TABLE dbo.ASE_BPCIM_SR_CIMLeaders_DEFINE (
    CIM_Group NVARCHAR(50) PRIMARY KEY,
    LeaderEmployeeID NVARCHAR(50) NOT NULL,
    Boss1EmployeeID NVARCHAR(50) NOT NULL,
    Boss2EmployeeID NVARCHAR(50) NULL
);
GO

CREATE TABLE dbo.ASE_BPCIM_SR_Statuses_DEFINE (
    StatusID INT PRIMARY KEY IDENTITY(1,1),
    StatusName NVARCHAR(50) UNIQUE NOT NULL
);
GO

CREATE TABLE dbo.ASE_BPCIM_SR_HIS (
    SRID INT PRIMARY KEY IDENTITY(1,1),
    SR_Number NVARCHAR(50) UNIQUE NOT NULL, 
    CIM_Group NVARCHAR(50) NULL, 
    Title NVARCHAR(200) NOT NULL,
    RequestorEmployeeID NVARCHAR(50) NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_YellowPages_TEST(EmployeeID),
    Purpose NVARCHAR(MAX) NULL,
    Scope NVARCHAR(MAX) NULL,
    Benefit NVARCHAR(MAX) NULL,
    InitialDocPath NVARCHAR(MAX) NULL,
    CurrentStatusID INT NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_Statuses_DEFINE(StatusID),
    SubmitDate DATETIME NOT NULL DEFAULT GETDATE(),
    AssignedEngineerEmployeeID NVARCHAR(50) NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_YellowPages_TEST(EmployeeID),
    AssignmentDate DATETIME NULL,
    PlannedCompletionDate DATE NULL,
    EngineerAcceptanceDate DATETIME NULL,
    ClosureReportPath NVARCHAR(MAX) NULL,
    EngineerConfirmClosureDate DATETIME NULL
);
GO

CREATE TABLE dbo.ASE_BPCIM_SR_Approvers_HIS (
    SRAID INT PRIMARY KEY IDENTITY(1,1),
    SRID INT NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_HIS(SRID),
    ApproverEmployeeID NVARCHAR(50) NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_YellowPages_TEST(EmployeeID),
    ApprovalStatus NVARCHAR(20) NOT NULL DEFAULT '待簽核',
    ApprovalDate DATETIME NULL,
    Comments NVARCHAR(MAX) NULL
);
GO

CREATE TABLE dbo.ASE_BPCIM_SR_Action_HIS (
    HistoryID INT PRIMARY KEY IDENTITY(1,1),
    SRID INT NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_HIS(SRID),
    Action NVARCHAR(100) NOT NULL,
    ActionByEmployeeID NVARCHAR(50) NOT NULL FOREIGN KEY REFERENCES dbo.ASE_BPCIM_SR_YellowPages_TEST(EmployeeID),
    ActionDate DATETIME NOT NULL DEFAULT GETDATE(),
    OldStatusID INT NULL,
    NewStatusID INT NULL,
    Notes NVARCHAR(MAX) NULL
);
GO

-- 插入預設資料
PRINT 'Inserting default data...';
INSERT INTO dbo.ASE_BPCIM_SR_YellowPages_TEST (EmployeeID, Username, Department, CIM_Group, Position, ManagerEmployeeID) VALUES
('admin', '系統管理員', 'IT', NULL, 'Admin', NULL),
('user001', '開單人A', 'SALES', NULL, '一般職員', 'manager001'),
('manager001', '開單主管A', 'SALES', NULL, '主管', NULL),
('signoff01', '會簽人員1', 'FINANCE', NULL, '一般職員', 'signmanager01'),
('signmanager01', '會簽主管1', 'FINANCE', NULL, '主管', NULL),
('cim_eng_a', 'CIM工程師A', 'CIM', '1000', '工程師', 'leader1'),
('cim_eng_b', 'CIM工程師B', 'CIM', '2000', '工程師', 'leader2'),
('leader1', 'CIM主任1', 'CIM', '1000', '主任', 'boss1'),
('leader2', 'CIM主任2', 'CIM', '2000', '主任', 'boss1'),
('boss1', 'CIM經副理1', 'CIM', NULL, '經副理', NULL),
('boss2', 'CIM經副理2', 'CIM', NULL, '經副理', 'boss1');
GO

INSERT INTO dbo.ASE_BPCIM_SR_CIMLeaders_DEFINE (CIM_Group, LeaderEmployeeID, Boss1EmployeeID, Boss2EmployeeID) VALUES
('1000', 'leader1', 'boss1', 'boss2'),
('2000', 'leader2', 'boss1', 'boss2');
GO

INSERT INTO dbo.ASE_BPCIM_SR_Statuses_DEFINE (StatusName) VALUES
('待開單主管審核'), ('待會簽審核'), ('待CIM主管審核'), ('待CIM主任指派'),
('待工程師接單'), ('需求確認中'), ('開發中'), ('待使用者測試'),
('待使用者上傳報告'), ('待程式上線'), ('已結案'), ('已拒絕'),
('待開單人修改'), ('已取消');
GO

PRINT 'Database setup complete.';
