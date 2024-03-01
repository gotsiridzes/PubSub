using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageBroker.Endpoints;

public static class Topics
{
	public static void RegisterTopicsEndpoints(this IEndpointRouteBuilder routeBuilder)
	{
		var topics = routeBuilder.MapGroup("api/topics/");

		// Create Topic
		topics.MapPost("" ,async (AppDbContext dbContext, Topic topic) =>
		{
			await dbContext.Topics.AddAsync(topic);

			await dbContext.SaveChangesAsync();

			return Results.Created($"api/topics/{topic.Id}", topic);
		});

		// Return topics
		topics.MapGet("", async (AppDbContext dbContext) =>
		{
			var topics = await dbContext.Topics.ToListAsync();

			return Results.Ok(topics);
		});

		// Publish message
		topics.MapPost("{topicId}/messages", async (AppDbContext dbContext, int topicId, Message message) =>
		{
			var topics = await dbContext.Topics.AnyAsync(t => t.Id.Equals(topicId));

			if (!topics)
				return Results.NotFound($"Topic with id `{topicId}` not found");

			var subs = await dbContext.Subscriptions.Where(s => s.TopicId.Equals(topicId)).ToListAsync();

			if (subs.Count == 0)
				return Results.NotFound("There are no subscriptions for this topic");

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
		topics.MapPost("{topicId}/subscriptions", async (AppDbContext dbContext, int topicId, Subscription sub) =>
		{
			var topics = await dbContext.Topics.AnyAsync(t => t.Id.Equals(topicId));

			if (!topics)
				return Results.NotFound($"Topic with id `{topicId}` not found");

			sub.TopicId = topicId;

			await dbContext.Subscriptions.AddAsync(sub);
			await dbContext.SaveChangesAsync();

			return Results.Created($"api/topics/{topicId}/subscriptions/sub/{sub.Id}", sub);
		});
	}
}
