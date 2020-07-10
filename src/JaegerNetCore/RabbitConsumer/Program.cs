using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter.Jaeger;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;


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

            var activity = new Activity(memberName)
                .Start();

            activity.AddTag("CallerMemberName", memberName);
            activity.AddTag("CallerFilePath", sourceFilePath);
            activity.AddTag("CallerLineNumber", sourceLineNumber.ToString());


            return activity;


        }
        static Tracer tracer;
        static void Main(string[] args)
        {

            var opt = new JaegerExporterOptions();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();

            var serviceProvider = new ServiceCollection()
                    .AddLogging()
                    .AddSingleton<IConfiguration>(config)
                    .AddOpenTelemetry(b =>
                    {
                        b//.AddRequestAdapter()
                       .UseJaeger(c =>
                       {
                           var s = config.GetSection("Jaeger");

                           s.Bind(c);


                       });
                        var x = new Dictionary<string, object>() {
                            { "PC", Environment.MachineName } };
                        b.SetResource(new Resource(x.ToArray()));

                    }).BuildServiceProvider();
            var f = serviceProvider.GetRequiredService<TracerFactoryBase>();
            tracer = f.GetTracer("custom");



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
                            var traceidHex = Encoding.UTF8.GetString((byte[])props["MyTraceId"]);
                            var spanIdHex =Encoding.UTF8.GetString((byte[]) props["MySpanId"]);
                            var traceId = ActivityTraceId.CreateFromString(traceidHex);
                            var spanId = ActivitySpanId.CreateFromString(spanIdHex);

                            act.SetParentId(traceId, spanId);
                        }
                        TelemetrySpan tsMultiple;
                        using (var span = tracer.StartActiveSpanFromActivity(act.OperationName, act, SpanKind.Producer, out tsMultiple))
                        {
                            //Console.WriteLine(f.Key + f.Value);
                            var message = Encoding.UTF8.GetString(body);
                            tsMultiple.SetAttribute("message received", message);
                            Console.WriteLine(" [x] Received {0}", message);
                        }
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
