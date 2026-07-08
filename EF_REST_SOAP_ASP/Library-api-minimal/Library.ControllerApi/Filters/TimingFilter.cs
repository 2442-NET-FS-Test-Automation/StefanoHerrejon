using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Library.ControllerApi.Filters;

//A filter applies AFTER model binding - so at this point the route has been matched
//we are inside Controller "Action" - so we deal not with the HTTPContext, but with the ActionContext
//Middleware are essentially global - we can write filters and then activate them only for certain endpoints
//or controllers

public class TimingFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        //we will just make a stopwatch again
        var sw = Stopwatch.StartNew();
        var execute = await next(); //calls the next step in the controller action "pipeline"
        sw.Stop();
        execute.HttpContext.Response.Headers["X-Elapsed-ms"] = sw.ElapsedMilliseconds.ToString();
    }
}
