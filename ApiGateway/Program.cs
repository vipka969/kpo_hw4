using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Primitives;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient("order-service", client =>
{
    client.BaseAddress = new Uri("http://order-service:8080/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("payments-service", client =>
{
    client.BaseAddress = new Uri("http://payments-service:8080/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapHealthChecks("/health");
app.MapGet("/", () => "API Gateway");

app.MapMethods("/orders/{**path}", 
    new[] { "GET", "POST", "PUT", "DELETE", "PATCH" },
    async (HttpContext context, IHttpClientFactory httpClientFactory, [FromRoute] string? path) =>
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient("order-service");
            string url = string.IsNullOrEmpty(path) ? "orders" : $"orders/{path}";
            
            Console.WriteLine($"Proxying to order-service: {url}");
            
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(context.Request.Method), url);

            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            if (context.Request.ContentLength > 0)
            {
                using StreamReader reader = new StreamReader(context.Request.Body);
                string body = await reader.ReadToEndAsync();
                request.Content = new StringContent(body, Encoding.UTF8, 
                    context.Request.ContentType ?? "application/json");
            }

            HttpResponseMessage response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
            
            return Results.Text(responseContent, contentType, statusCode: (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return Results.Problem(detail: "Order Service is unavailable", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    });

app.MapMethods("/payments/{**path}", 
    new[] { "GET", "POST", "PUT", "DELETE", "PATCH" },
    async (HttpContext context, IHttpClientFactory httpClientFactory, [FromRoute] string? path) =>
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient("payments-service");
            string url = string.IsNullOrEmpty(path) ? "" : path;
            
            Console.WriteLine($"Proxying to payments-service: '{url}'");
            Console.WriteLine($"Request method: {context.Request.Method}");
            Console.WriteLine($"Request headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
            
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(context.Request.Method), url);

            foreach (KeyValuePair<string, StringValues> header in context.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            if (context.Request.ContentLength > 0)
            {
                using StreamReader reader = new StreamReader(context.Request.Body);
                string body = await reader.ReadToEndAsync();
                Console.WriteLine($"Request body: {body}");
                request.Content = new StringContent(body, Encoding.UTF8, 
                    context.Request.ContentType ?? "application/json");
            }

            HttpResponseMessage response = await client.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            string contentType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
            
            Console.WriteLine($"Response from payments-service: {response.StatusCode}");
            Console.WriteLine($"Response body: {responseContent}");
            
            return Results.Text(responseContent, contentType, statusCode: (int)response.StatusCode);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error connecting to payments-service: {ex.Message}");
            return Results.Problem(
                detail: "Payments Service is unavailable",
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return Results.Problem(
                detail: "Internal server error",
                statusCode: StatusCodes.Status500InternalServerError
            );
        }
    });

app.Run();