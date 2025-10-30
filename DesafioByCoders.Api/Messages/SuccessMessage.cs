namespace DesafioByCoders.Api.Messages;

public readonly record struct SuccessMessage(string Code, string Message) : IMessage;