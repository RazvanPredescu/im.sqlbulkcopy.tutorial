CREATE TABLE dbo.Employees
(
  Id			INT          NOT NULL IDENTITY,
  LastName      NVARCHAR(250) NOT NULL,
  FirstName     NVARCHAR(250) NOT NULL,
  Address       NVARCHAR(250) NOT NULL,
  City          NVARCHAR(250) NOT NULL,
  CONSTRAINT PK_Employees PRIMARY KEY(Id),
);