using MessageBroker.Data;
using Microsoft.EntityFrameworkCore;

namespace MessageBroker;

public static class ConfigurationExts
{
	public static IServiceCollection AddSqliteDbContext(this IServiceCollection services, IConfiguration config)
	{
		services.AddDbContext<AppDbContext>(ops =>
		{
			ops.UseSqlite(config.GetConnectionString("MessageBus"));
		});

		return services;
	}
}