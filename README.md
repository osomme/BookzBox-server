# BookzBox-server

## Firebase cloud services

## Recommender system

#### Server software- ASP.NET Core

#### Database software- Neo4j
<b>System requirements: </b>
  
- Ubuntu 16.04+  

- OpenJDK 11

<b>Log:</b>     /var/log/neo4j/  
<b>Config:</b> /etc/neo4j/neo4j.conf   
<b>Auth:</b> /var/lib/neo4j/data/dbms/auth  

<b>Setup:</b>

- Install neo4j [install-instructions](https://neo4j.com/docs/operations-manual/current/installation/linux/debian/)

- Unmark HTTP in config file

- Set allow_upgrade=true in config file

Add constraint on userId: CREATE CONSTRAINT ON (u:User) ASSERT u.userId IS UNIQUE   
Add constraint on boxId: CREATE CONSTRAINT ON (box:Box) ASSERT box.boxId IS UNIQUE  
Add constraint on subject: CREATE CONSTRAINT ON (s:Subject) ASSERT s.name IS UNIQUE  
Add constraint on thumbnailUrl: CREATE CONSTRAINT ON (b:Book) ASSERT b.thumbnailUrl IS UNIQUE  


