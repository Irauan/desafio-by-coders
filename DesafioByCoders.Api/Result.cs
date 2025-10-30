using DesafioByCoders.Api.Messages;

namespace DesafioByCoders.Api;

public abstract record Result<TSuccess>
{
    public sealed record Success(TSuccess Value) : Result<TSuccess>()
    {
        public static explicit operator TSuccess(Success success)
        {
            return success.Value;
        }
    }

    public sealed record Failure(List<ValidationError> Errors) : Result<TSuccess>()
    {
        public static explicit operator List<ValidationError>(Failure failure)
        {
            return failure.Errors;
        }
    }

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;
    
    public static implicit operator Result<TSuccess>(TSuccess success)
    {
        return new Success(success);
    }

    public static implicit operator Result<TSuccess>(List<ValidationError> errors)
    {
        return new Failure(errors);
    }

    public static implicit operator Result<TSuccess>(ValidationError error)
    {
        return new Failure([error]);
    }

    public static explicit operator TSuccess(Result<TSuccess> result)
    {
        return (TSuccess)(Success)result;
    }
    
    public static explicit operator List<ValidationError>(Result<TSuccess> result)
    {
        return (List<ValidationError>)(Failure)result;
    }
    
    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<List<ValidationError>, TResult> onError)
    {
        return this switch
        {
            Success success => onSuccess(success.Value),
            Failure errors => onError(errors.Errors),
            _ => throw new InvalidOperationException()
        };
    }

    public void Switch(Action<TSuccess> onSuccess, Action<List<ValidationError>> onError)
    {
        switch (this)
        {
            case Success success:
                onSuccess(success.Value);

                break;
            case Failure errors:
                onError(errors.Errors);

                break;
        }
    }
}