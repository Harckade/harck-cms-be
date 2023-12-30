using Harckade.CMS.Azure.Enums;

namespace Harckade.CMS.Azure.Domain
{
    //based on https://github.com/achraf1996/SimpleTodoList/blob/master/SimpleTodoList.Logic/Result.cs
    public class Result
    {
        public bool Success { get; }
        public Failure FailureReason { get; private set; }
        public string Description { get; private set; }
        public bool Failed => !Success;

        protected Result(bool success, Failure failure = Failure.Default, string description = "")
        {
            if (!success && failure == Failure.Default)
            {
                throw new System.InvalidOperationException("A failure must always contain a message");
            }
            else if (!success && failure != Failure.Default)
            {
                FailureReason = failure;
                Description = description;
            }
            Success = success;
        }

        public static Result Fail(Failure failure, string description = "")
        {
            return new Result(false, failure, description);
        }

        public static Result<T> Fail<T>(Failure failure, string description = "")
        {
            return new Result<T>(default(T), false, failure, description);
        }

        public static Result Ok()
        {
            return new Result(true);
        }

        public static Result<T> Ok<T>(T value)
        {
            return new Result<T>(value, true);
        }
    }

    public class Result<T> : Result
    {
        public T Value
        {
            get
            {
                if (this.Failed)
                {
                    throw new System.InvalidOperationException("The operation failed. Thus there is no result value.");
                }
                return _Value;
            }
        }
        private T _Value;

        protected internal Result(T value, bool success, Failure failure = Failure.Default, string description = "") : base(success, failure, description)
        {
            _Value = value;
        }
    }
}
