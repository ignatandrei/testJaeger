using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RabbitConsumer
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
            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "user",
                Password = "password"
            };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "hello",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var props = ea.BasicProperties.Headers;
                        var act = GetNewActionFromCurrent();
                        if (props?.Count(it => it.Key == "MyTraceId") > 0)
                        {
                            var traceidHex =  props["MyTraceId"].ToString();
                            var spanIdHex = props["MySpanId"].ToString();
                            var traceId = ActivityTraceId.CreateFromString(traceidHex);
                            var spanId = ActivitySpanId.CreateFromString(spanIdHex);

                            act.SetParentId(traceId, spanId);
                        }
                        //Console.WriteLine(f.Key + f.Value);
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine(" [x] Received {0}", message);
                    };
                    channel.BasicConsume(queue: "hello",
                                         autoAck: true,
                                         consumer: consumer);

                    Console.WriteLine(" Press [enter] to exit.");
                    Console.ReadLine();

                }
            }
        }
    }
}
