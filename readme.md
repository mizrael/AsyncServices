# AsyncServices
Sample repository showing how to handle requests asynchronously.

The system is composed by a Web API and a Console app.

The Web API exposes a `POST` endpoint which can be used by clients to send the requests. The payload is encoded and published on a RabbitMQ Exchange. 

The Console app acts as subscriber, receiving and processing the messages.


### Execution
The infrastructure can be provisioned using Docker Compose. The configuration is available [here](https://github.com/mizrael/AsyncServices/blob/main/src/docker-compose.yml). 

Just run 
```
docker-compose up
```
from the `./src` folder.