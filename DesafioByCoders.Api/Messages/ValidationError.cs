namespace DesafioByCoders.Api.Messages;

public readonly record struct ValidationError(string Code, string Message) : IMessage;