# door

## role

the door microservice manages the register and login of users

## usage

the door microservice has the following endpoints:

### register a new user (/register)

##### method

POST

##### body

{
  "username":"put_username_here",
  "password":"put_password_here",
  "ticket":"put_ticket_here"
}

### login (/login)

##### method

POST // we should change this to GET though

##### body

{
  "username":"put_username_here",
  "password":"put_password_here",
}

### login with a token (/tokenlogin)

##### method

POST // we should change this to GET though

##### body

{
  "token":"put_token_here"
}

## configuration

this microservice is configured with the file appsettings.json.
a template file is provided, the expected database is a MongoDB database
