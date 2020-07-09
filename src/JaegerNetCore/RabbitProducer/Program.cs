using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() {
                HostName = "localhost",
                UserName ="user",
                Password="password"
            };
            using (var connection = factory.CreateConnection())
            {
                using (var model = connection.CreateModel())
                {
                    model.QueueDeclare(queue: "hello",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                    string message = "Message sent at :"+ DateTime.Now.ToString("o");
                    var body = Encoding.UTF8.GetBytes(message);
                    var props = model.CreateBasicProperties();
                    props.Headers = new Dictionary<string, object>();
                    props.Headers.Add("andrei", "test");
                    props.Persistent = true;

                    model.BasicPublish(exchange: "",
                                         routingKey: "hello",
                                         basicProperties: props,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", message);
                }
            }
        }
    }
}
