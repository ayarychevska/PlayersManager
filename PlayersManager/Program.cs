
using Microsoft.EntityFrameworkCore;
using PlayersManager.Data;

namespace PlayersManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' not found.");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
