# AsyncServices
Sample repository showing how to handle requests asynchronously.

The system is composed by a Web API and a Console app.

The Web API exposes a `POST` endpoint which can be used by clients to send the requests. The payload is encoded and published on a RabbitMQ Exchange. 

The Console app acts as subscriber, receiving and processing the messages.