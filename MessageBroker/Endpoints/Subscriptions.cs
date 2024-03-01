using MessageBroker.Data;
using MessageBroker.Models;
using Microsoft.EntityFrameworkCore;

namespace MessageBroker.Endpoints;

public static class Subscriptions
{
	public static void RegisterSubscriptionsEndpoints(this IEndpointRouteBuilder routeBuilder)
	{
		var subscriptions = routeBuilder.MapGroup("api/subscriptions/");

		subscriptions.MapPost("{topicId}/subscriptions", async (AppDbContext dbContext, int topicId, Subscription sub) =>
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
		subscriptions.MapGet("{subId}/messages", async (AppDbContext dbContext, int subId) =>
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
		subscriptions.MapPost("{subId}/messages", async (AppDbContext dbContext, int subId, int[] messageConfirmations) =>
		{
			var subs = await dbContext.Subscriptions.AnyAsync(s => s.Id.Equals(subId));

			if (!subs)
				return Results.NotFound($"Subscription with id `{subId}` not found");

			if (!messageConfirmations.Any())
				return Results.BadRequest();

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
	}
}