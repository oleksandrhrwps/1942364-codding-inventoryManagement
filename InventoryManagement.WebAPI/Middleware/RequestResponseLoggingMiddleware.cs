using System.Diagnostics;

namespace InventoryManagement.WebAPI.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // Log request metadata
            var stopwatch = Stopwatch.StartNew();
            var requestInfo = $"Request: {context.Request.Method} {context.Request.Path}\n";
            requestInfo += $"Headers: {string.Join("; ", context.Request.Headers)}\n";

            // Wrap response body to read it later
            var originalBody = context.Response.Body;
            using (var newBody = new MemoryStream())
            {
                context.Response.Body = newBody;

                await _next(context);

                stopwatch.Stop();
                // Log response data
                var responseInfo = $"Response: {context.Response.StatusCode}\n";
                responseInfo += $"Headers: {string.Join("; ", context.Response.Headers)}\n";
                responseInfo += $"Time taken: {stopwatch.ElapsedMilliseconds} ms\n";

                // Write log to a file
                await File.AppendAllTextAsync("http_log.txt", requestInfo + responseInfo);

                // Copy contents of the new stream to the original body stream
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await context.Response.Body.CopyToAsync(originalBody);
            }
        }
    }
}
