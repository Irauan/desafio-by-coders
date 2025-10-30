namespace DesafioByCoders.Api.Messages;

public readonly record struct NotFoundError(string Code, string Message) : IMessage;