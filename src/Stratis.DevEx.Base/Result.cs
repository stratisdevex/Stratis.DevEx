using System;
using System.Collections.Generic;
using System.Text;

namespace Stratis.DevEx
{
    public enum ResultType
    {
        Success,
        Failure
    }
    
    public struct Result<T>
    {
        public Result(ResultType type, T? value = default, string? message = null, Exception ? exception = null)
        {
            this.Type = type;
            this._Value = value;
            this.Message = message;
            this.Exception = exception;
        }

        public bool IsSuccess => this.Type == ResultType.Success;

        public T Value => IsSuccess ? _Value!: throw new InvalidOperationException("The operation did not succced.");
       
        public bool Succeeded(out Result<T> r)
        {
            r = this;
            return IsSuccess;
        }

        public string FailureMessage
        {
            get
            {
                if (IsSuccess || (string.IsNullOrEmpty(Message) && Exception == null))
                    throw new InvalidOperationException("The operation succeded.");
                else if (this.Exception != null && !string.IsNullOrEmpty(Message))
                {
                    return $"{this.Message}: {this.Exception.Message}.";
                }
                else if (this.Exception != null)
                {
                    return this.Exception.Message;
                }
                else
                {
                    return this.Message!;

                }
            }
        }
        public static Result<T> Success(T value) => new Result<T>(ResultType.Success, value);

        public static Result<T> Failure(string? message, Exception? exception) => new Result<T>(ResultType.Failure, message:message, exception: exception);

        public static async Task<Result<T>> ExecuteAsync(Task<T> task, string? errorMessage = null)
        {
            try
            {  
                return Success(await task);
            }
            catch (Exception ex)
            {
                return Failure(errorMessage, ex);   
            }
        }
        public ResultType Type;
        public T? _Value;
        public string? Message;
        public Exception? Exception;
    }

    public static class Result
    {
        public static async Task<Result<T>> ExecuteAsync<T>(Task<T> task, string? errorMessage = null) => await Result<T>.ExecuteAsync(task, errorMessage);

        public static bool Succedeed<T>(Result<T> result, out Result<T> r)
        {
            r = result;
            return r.IsSuccess;
        }
    }
}
