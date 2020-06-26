﻿using System;
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
        private Activity GetCurrentAction(string name)
        {
            var curent = Activity.Current;
            var traceId = curent.TraceId;
            var spanId = curent.SpanId;
            if (curent.Baggage.Count(it => it.Key == "MyTraceId") > 0)
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
        public TestMultiple(TracerFactoryBase tracerFactory)
        {
            this.tracer = tracerFactory.GetTracer("TestMultiple");
        }

        [HttpGet("{id}")]
        public async Task<string> WaitFirst( string id)
        {
            var rng = new Random();
            var val = rng.Next(5, 15);
            await Task.Delay(val * 1000);
            var activity = GetCurrentAction(nameof(WaitFirst));
            activity.AddTag("action", nameof(WaitFirst));
            activity.AddTag("id", id);

            TelemetrySpan ts;

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {

                activity.Stop();
                return $"This is {id}";
                
            }
        }
        [HttpGet("{id}")]
        public async Task<string> GetActivityFirst(string id)
        {
            var rng = new Random();
            var val = rng.Next(5, 15);
            
            var activity = GetCurrentAction(nameof(GetActivityFirst));
            activity.AddTag("action", nameof(WaitFirst));
            activity.AddTag("id", id);

            await Task.Delay(val* 1000);
            
            TelemetrySpan ts;

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {
                activity.Stop();
                return "This is {id}";
                
            }
        }

        private Task<int> SecondAction()
        {
            var activity = GetCurrentAction(nameof(SecondAction));
            activity.AddTag("action", nameof(SecondAction));
            TelemetrySpan ts;

            using (var span = tracer.StartActiveSpanFromActivity(activity.OperationName, activity, SpanKind.Client, out ts))
            {
                activity.Stop();
                return Task.FromResult(10);

            }
        }

    }
}
