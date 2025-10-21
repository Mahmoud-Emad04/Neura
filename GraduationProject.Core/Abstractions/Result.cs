namespace GraduationProject.Core.Abstractions;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A result cannot be successful and contain an error.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failing result must contain an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; } = default!;

    public static Result Success()
    {
        return new Result(true, Error.None);
    }

    public static Result Failure(Error error)
    {
        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value)
    {
        return new Result<T>(value, true, Error.None);
    }

    public static Result<T> Failure<T>(Error error)
    {
        return new Result<T>(default!, false, error);
    }
}

public class Result<T>(T value, bool isSuccess, Error? error) : Result(isSuccess, error)
{
    private readonly T _value = value;

    public T Value =>
        IsSuccess ? _value : throw new InvalidOperationException("Cannot access the value of a failed result.");
}