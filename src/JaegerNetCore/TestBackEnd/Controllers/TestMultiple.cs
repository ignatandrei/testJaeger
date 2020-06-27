using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry.Trace;

namespace TestBackEnd.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestMultipleController : ControllerBase
    {
        private Tracer tracer;
        private Activity GetNewActionFromCurrent(string name)
        {
            var curent = Activity.Current;

            var traceId = curent.TraceId;
            var spanId = curent.SpanId;
            if (curent?.Baggage.Count(it => it.Key == "MyTraceId") > 0)
            {

                traceId = ActivityTraceId.CreateFromString(curent.GetBaggageItem("MyTraceId"));
                spanId = ActivitySpanId.CreateFromString(curent.GetBaggageItem("MySpanId"));
            }

            var activity = new Activity(name)            
                .SetParentId(traceId, spanId)
                .Start();

            activity.AddBaggage("MyTraceId", activity.TraceId.ToHexString());
            activity.AddBaggage("MySpanId", activity.SpanId.ToHexString());


            return activity;


        }
        public TestMultipleController(TracerFactoryBase tracerFactory)
        {
            this.tracer = tracerFactory.GetTracer("TestMultiple");
        }

        [HttpGet("{id}")]
        public async Task<string> WaitFirst(string id)
        {
            return id;
            var rng = new Random();
            var val = rng.Next(5, 15);
            await Task.Delay(val * 1000);
            var activity = GetNewActionFromCurrent(nameof(WaitFirst) + "_"+id);
            activity.AddTag("action", nameof(WaitFirst));

            TelemetrySpan ts;

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {

                await SecondAction(nameof(WaitFirst) +"_"+id);
                //activity.Stop();
                return $"This is " ;
                
            }
        }
        [HttpGet("{id}")]
        public async Task<string> GetActivityFirst(string id)
        {
            var rng = new Random();
            var val = rng.Next(5, 15);
            
            var activity = GetNewActionFromCurrent(nameof(GetActivityFirst)+"_"+id);
            activity.AddTag("action", nameof(WaitFirst));
            

            await Task.Delay(val* 1000);
            
            TelemetrySpan ts;

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {
                var actDelay = Activity.Current;
                await FirstAction(nameof(GetActivityFirst) + "_" + id);
                Activity.Current = actDelay;
                await SecondAction(nameof(GetActivityFirst)+"_"+id);
                //activity.Stop();
                return "This is ";
                
            }
        }

        private Task<int> FirstAction(string fromWhere)
        {
            var activity = GetNewActionFromCurrent(nameof(FirstAction)+ fromWhere);
            activity.AddTag("action", nameof(FirstAction));
            TelemetrySpan ts;

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {
                //activity.Stop();
                ts.SetAttribute("I am from", nameof(FirstAction) + fromWhere);
                return Task.FromResult(10);

            }
        }
        private Task<int> SecondAction(string fromWhere)
        {
            var activity = GetNewActionFromCurrent(nameof(SecondAction) + fromWhere);
            activity.AddTag("action", nameof(SecondAction));
            TelemetrySpan ts;
            
            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {
                ts.SetAttribute("I am from", nameof(SecondAction) + fromWhere);
                //activity.Stop();
                return Task.FromResult(10);

            }
        }

    }
}
