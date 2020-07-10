using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RabbitProducer
{
    class Program
    {
        static private Activity GetNewActionFromCurrent(
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var curent = Activity.Current;

            var traceId = curent.TraceId;
            var spanId = curent.SpanId;
            if (curent?.Baggage.Count(it => it.Key == "MyTraceId") > 0)
            {

                traceId = ActivityTraceId.CreateFromString(curent.GetBaggageItem("MyTraceId"));
                spanId = ActivitySpanId.CreateFromString(curent.GetBaggageItem("MySpanId"));
            }

            var activity = new Activity(memberName)
                .SetParentId(traceId, spanId)
                .Start();

            activity.AddBaggage("MyTraceId", activity.TraceId.ToHexString());
            activity.AddBaggage("MySpanId", activity.SpanId.ToHexString());
            activity.AddTag("CallerMemberName", memberName);
            activity.AddTag("CallerFilePath", sourceFilePath);
            activity.AddTag("CallerLineNumber", sourceLineNumber.ToString());


            return activity;


        }
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() {
                HostName = "localhost",
                UserName ="user",
                Password="password"
            };
            while (true)
            {
                using (var connection = factory.CreateConnection())
                {
                    using (var model = connection.CreateModel())
                    {
                        model.QueueDeclare(queue: "hello",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                        string message = "Message sent at :" + DateTime.Now.ToString("o");
                        var body = Encoding.UTF8.GetBytes(message);
                        var props = model.CreateBasicProperties();
                        var act = GetNewActionFromCurrent();
                        props.Headers = new Dictionary<string, object>();
                        props.Headers.Add("MyTraceId", act.TraceId.ToHexString());
                        props.Headers.Add("MySpanId", act.SpanId.ToHexString());

                        props.Persistent = true;

                        model.BasicPublish(exchange: "",
                                             routingKey: "hello",
                                             basicProperties: props,
                                             body: body);
                        Console.WriteLine(" [x] Sent {0}", message);
                    }
                }
                Console.ReadLine();
            }
        }
    }
}
