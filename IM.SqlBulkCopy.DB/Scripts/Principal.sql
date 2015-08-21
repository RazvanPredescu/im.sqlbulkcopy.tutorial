IF NOT EXISTS 
    (SELECT name  
     FROM master.sys.server_principals
     WHERE name = 'bulkinsert')
BEGIN
    CREATE LOGIN bulkinsert WITH PASSWORD = 'bulkinsertpassword'
END

GO

-- Creates a database user for the login created above.
IF NOT EXISTS 
	(SELECT * FROM sys.sysusers WHERE name='bulkinsert')
BEGIN
	CREATE USER bulkinsert FOR LOGIN bulkinsert;
	EXEC sp_addrolemember 'db_owner', 'bulkinsert'
END

GO