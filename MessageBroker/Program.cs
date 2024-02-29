using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(ops =>
{
	ops.UseSqlite(builder.Configuration.GetConnectionString("MessageBus"));
});

var app = builder.Build();


// Create Topic
app.MapPost("api/topics", async (AppDbContext dbContext, Topic topic) =>
{
	await dbContext.Topics.AddAsync(topic);

	await dbContext.SaveChangesAsync();

	return Results.Created($"api/topics/{topic.Id}", topic);
});

// Return topics
app.MapGet("api/topics", async (AppDbContext dbContext) =>
{
	var topics = await dbContext.Topics.ToListAsync();

	return Results.Ok(topics);
});

// Publish message
app.MapPost("/api/topics/{id}/messages", async (AppDbContext dbContext, int id, Message message) =>
{
	var topics = await dbContext.Topics.AnyAsync(t => t.Id.Equals(id));

	if (!topics)
		return Results.NotFound($"Topic with id `{id}` not found");

	var subs = await dbContext.Subscriptions.Where(s => s.TopicId.Equals(id)).ToListAsync();

	if (subs.Count == 0)
		return Results.NotFound("There are no subscriptions for this topic");

	//subs.Aggregate((subs2) =>
	//{
	//	subs2.
	//});

	subs.ForEach(async (sub) =>
	{
		await dbContext.Messages.AddAsync(new Message
		{
			TopicMessage = message.TopicMessage,
			ExpiresAfter = message.ExpiresAfter,
			MessageStatus = message.MessageStatus,
			SubscriptionId = sub.Id
		});
	});


	await dbContext.SaveChangesAsync();

	return Results.Ok("Message has been published");
});

app.Run();
