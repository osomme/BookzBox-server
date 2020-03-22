# BookzBox-server

## Firebase cloud services


## Recommender server
<b>Cloud provider:</b> Amazon AWS   
<b>Instance type:</b> EC2   
<b>IPv4:</b> 13.48.105.244   
<b>DNS:</b> ec2-13-48-105-244.eu-north-1.compute.amazonaws.com   
<b>OS:</b> Ubuntu   

#### Access
sudo ssh -i ~/.ssh/[KEY_FILE] USERNAME@DNS   

#### Architecture
Client - (HTTP) -> Nginx (public) -> ASP.NET API (local) -> Neo4j (local)

#### ASP.NET Core
Recommender API    
<b>Location:</b>  http://localhost:5000    
<b>Service name:</b> recommender-sys    
<b>Files: </b> ~/www   

<b>Deploy</b>    
1. Build:   dotnet publish -r linux-x64 --self-contained false     
2. Copy build to server:   scp -ri ~/.ssh/[KEY_FILE] publish/ USERNAME@DNS:~/www/     


#### Nginx
Acts as a proxy redirecting requests to the correct service. 

<b>Location:</b> IPv4:80    
<b>Service name:</b> nginx   


#### Neo4j

<b>Location:</b> bolt://localhost:7687   
<b>Service name:</b> neo4j    
<b>Log:</b>     /var/log/neo4j/  
<b>Config:</b> /etc/neo4j/neo4j.conf   
<b>Auth:</b> /var/lib/neo4j/data/dbms/auth  

<b>System requirements: </b>
  
- Ubuntu 16.04+  

- OpenJDK 11

<b>Setup:</b>

- Install neo4j [install-instructions](https://neo4j.com/docs/operations-manual/current/installation/linux/debian/)

- Name database in config file

- Change HTTP/HTTPS settings in config file

Add constraint on userId: CREATE CONSTRAINT ON (u:User) ASSERT u.userId IS UNIQUE   
Add constraint on boxId: CREATE CONSTRAINT ON (box:Box) ASSERT box.boxId IS UNIQUE  
Add constraint on subject: CREATE CONSTRAINT ON (s:Subject) ASSERT s.name IS UNIQUE  
Add constraint on thumbnailUrl: CREATE CONSTRAINT ON (b:Book) ASSERT b.thumbnailUrl IS UNIQUE  


