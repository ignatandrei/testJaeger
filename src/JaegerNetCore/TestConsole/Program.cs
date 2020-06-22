using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Exporter.Jaeger;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var hc = new HttpClient();
            hc.BaseAddress = new Uri("http://localhost:5000/");

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
            var tracer = f.GetTracer("custom");

            var activity = new Activity("I am from console").Start();

            activity.AddBaggage("MyTraceId", activity.TraceId.ToHexString());
            activity.AddBaggage("MySpanId", activity.SpanId.ToHexString());
            activity.AddTag("fromConsole", "Console");
            TelemetrySpan ts;//= tracer.StartSpanFromActivity("muop", activity);

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {
                ts.SetAttribute("orgId", "test backend" + DateTime.Now.Ticks);
                var response = hc.GetStringAsync("WeatherForecast").GetAwaiter().GetResult();
                Console.WriteLine(response);
                activity.Stop();
            }
            Console.ReadLine();

        }
    }
}