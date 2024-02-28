using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(ops =>
{
	ops.UseSqlite(builder.Configuration.GetConnectionString("MessageBus"));
});

var app = builder.Build();


app.MapPost("api/topics", async (AppDbContext dbContext, Topic topic) =>
{
	await dbContext.Topics.AddAsync(topic);

	await dbContext.SaveChangesAsync();

	return Results.Created($"api/topics/{topic.Id}", topic);
});

app.MapGet("api/topics", async (AppDbContext dbContext) =>
{
	var topics = await dbContext.Topics.ToListAsync();

	return Results.Ok(topics);
});

app.Run();
