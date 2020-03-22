# BookzBox-server

## Firebase cloud services

## Recommender system

#### Server software- ASP.NET Core
<b>Port:</b> 5000

<b>Add box</b>   
Protocol: HTTP POST

Uri: /api/box?key=KEY

Body:   
{
	"Id": "box10",
	"PublisherId": "user10",
	"PublishedOn": "01-01-2019",
	"Title": "A box",
	"Books": [ { "ThumbnailUrl": "URL", "Subjects": [ "Fiction", "Sci-Fi" ] } ],
	"Status": 1
}

<b>Add user</b>   
Protocol: HTTP POST

Uri: /api/users?key=KEY

Body: { "Id": " " }

<b>Like</b>   
Protocol: HTTP GET

Uri: /api/like?key=KEY&userId=USER_ID&boxId=BOX_ID

<b>Update status</b>
Protocol: HTTP PUT

Uri: /api/box/status?key=KEY&boxId=BOX_ID&status=NEW_BOX_STATUS

<b>Get recommendations</b>   
Protocol: HTTP GET

URI: api/recommendations?userId=USER_ID

Returns: list of boxes (feed boxes)

<b>Set/Update preferences</b>   
Protocol: HTTP PUT

URI: /api/preferences?key=KEY&userId=USER_ID

Body: { "Subjects" : [ "Fiction", "Romance" ] }


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


