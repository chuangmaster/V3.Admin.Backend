namespace V3.Admin.Backend;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
#if (useSwagger)
        builder.Services.AddOpenApi();
#endif

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseAuthorization();

        // Configure the HTTP request pipeline.
#if (useSwagger)
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }
#endif

        app.MapGet("/", () => "Hello from {{org}} - {{product}} API!");

        app.Run();
    }
}
