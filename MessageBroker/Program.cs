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
app.MapPost("/api/topics/{topicId}/messages", async (AppDbContext dbContext, int topicId, Message message) =>
{
	var topics = await dbContext.Topics.AnyAsync(t => t.Id.Equals(topicId));

	if (!topics)
		return Results.NotFound($"Topic with id `{topicId}` not found");

	var subs = await dbContext.Subscriptions.Where(s => s.TopicId.Equals(topicId)).ToListAsync();

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


// Create subscription
app.MapPost("api/topics/{topicId}/subscriptions", async (AppDbContext dbContext, int topicId, Subscription sub) =>
{
	var topics = await dbContext.Topics.AnyAsync(t => t.Id.Equals(topicId));

	if (!topics)
		return Results.NotFound($"Topic with id `{topicId}` not found");

	sub.TopicId = topicId;

	await dbContext.Subscriptions.AddAsync(sub);
	await dbContext.SaveChangesAsync();
	
	return Results.Created($"api/topics/{topicId}/subscriptions/sub/{sub.Id}", sub);
});

// Get messages for subscription
app.MapGet("api/subscriptions/{subId}/messages", async (AppDbContext dbContext, int subId) =>
{
	bool subs = await dbContext.Subscriptions.AnyAsync(s => s.Id.Equals(subId));

	if (!subs)
		return Results.NotFound($"Subscription with id `{subId}` not found");

	var messages = dbContext.Messages.Where(m => m.SubscriptionId.Equals(subId) && m.MessageStatus != "SENT");

	if (!messages.Any())
		return Results.NotFound("No new messages");

	messages.ToList()
			.ForEach(m => m.MessageStatus = "REQUESTED");

	await dbContext.SaveChangesAsync();

	return Results.Ok(messages);
});

// Ack messages for subscriber
app.MapPost("api/subscriptions/{subId}/messages",
	async (AppDbContext dbContext, int subId, int[] messageConfirmations) =>
	{
		bool subs = await dbContext.Subscriptions.AnyAsync(s => s.Id.Equals(subId));

		if (!subs)
			return Results.NotFound($"Subscription with id `{subId}` not found");

		if (!messageConfirmations.Any())
		{
			return Results.BadRequest();
		}

		var count = 0;

		foreach (var messageId in messageConfirmations)
		{
			var msg = dbContext.Messages.FirstOrDefault(m => m.Id.Equals(messageId));

			if (msg != null)
			{
				msg.MessageStatus = "SENT";
				count++;
			} 
		}

		await dbContext.SaveChangesAsync();

		return Results.Ok($"Acknowledged {count}/{messageConfirmations.Length} messages");
	});

app.Run();
