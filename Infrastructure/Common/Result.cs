using System;

namespace Infrastructure.Common
{
    public record Error(string Code, string Message, object? Details = null);

    public class Result
    {
        public bool IsSuccess { get; }
        public Error? Error { get; }

        protected Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(Error error) => new(false, error);
        public static Result Failure(string code, string message, object? details = null) => new(false, new Error(code, message, details));

        // Generic factory methods for convenience
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);
        public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
        public static Result<T> Failure<T>(string code, string message, object? details = null) => Result<T>.Failure(code, message, details);
    }

    public class Result<T> : Result
    {
        public T? Value { get; }

        private Result(T value) : base(true, null)
        {
            Value = value;
        }

        private Result(Error error) : base(false, error)
        {
            Value = default;
        }

        public static Result<T> Success(T value) => new(value);
        public static Result<T> Failure(Error error) => new(error);
        public static Result<T> Failure(string code, string message, object? details = null) => new(new Error(code, message, details));

        public static implicit operator Result<T>(T value) => Success(value);
    }
}
