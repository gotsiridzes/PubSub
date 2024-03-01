using System.Net.Http.Json;
using System.Threading.Channels;
using Subscriber.Dtos;

Console.WriteLine("Press ESC to stop");

do
{
	var client = new HttpClient
	{
		BaseAddress = new Uri("http://localhost:5254/")
	};

	Console.WriteLine("Listening for new messages...");

	while (!Console.KeyAvailable)
	{
		var ackIds = await GetMessagesAsync(client);

		Thread.Sleep(2000);

		if (ackIds.Any()) await AckMessagesAsync(client, ackIds);
	}

} while (Console.ReadKey().Key != ConsoleKey.Escape);

static async Task<List<int>> GetMessagesAsync(HttpClient client)
{
	var ackIds = new List<int>();
	var newMessages = new List<MessageReadDto>();

	try
	{
		newMessages = await client.GetFromJsonAsync<List<MessageReadDto>>("api/subscriptions/5/messages");

		newMessages!.ForEach(nm =>
		{
			Console.WriteLine($"{nm.Id} - {nm.TopicMessage} - {nm.MessageStatus}");
			ackIds.Add(nm.Id);
		});

		
	}
	catch
	{
		// ignored
	}

	return ackIds;
}

static async Task AckMessagesAsync(HttpClient client, List<int> ackIds)
{
	var response = await client.PostAsJsonAsync("api/subscriptions/5/messages/", ackIds);
	var message = await response.Content.ReadAsStringAsync();

	Console.WriteLine(message);
}