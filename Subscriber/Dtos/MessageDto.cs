using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Subscriber.Dtos;

internal class MessageReadDto
{
	public int Id { get; set; }

	public string? TopicMessage { get; set; } = null!;

	public DateTime ExpiresAfter { get; set; }

	public string? MessageStatus { get; set; } = null!;
}