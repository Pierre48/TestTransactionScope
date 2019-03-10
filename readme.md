# Test of transactionScope from DotNetFramework

Result should be something like .  
```
-- transaction is completed
--> Commit
Product - Product A
Product - Product B

-- transaction is not completed
--> noCommit

-- No transaction
Product - Product A
Product - Product B
--> end
```
