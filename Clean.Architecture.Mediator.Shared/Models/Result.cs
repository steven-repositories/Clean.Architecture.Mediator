﻿using FluentValidation.Results;

namespace Clean.Architecture.Mediator.Shared.Models {
    /// <summary>
    /// The base result type, which is used for unit of work operations.
    /// Does not contain 'Data'.
    /// </summary>
    public class UnitResult {
        public bool Successful => !Errors.Any() && !ValidationFailures.Any();
        public IEnumerable<string> Errors { get; init; } = new List<string>();
        public IEnumerable<ValidationFailure> ValidationFailures { get; init; } = new List<ValidationFailure>();

        public static UnitResult Success() {
            return new();
        }

        public static UnitResult Error(string error) {
            return new() { 
                Errors = new List<string> { 
                    error 
                } 
            };
        }

        public static UnitResult Error(IEnumerable<string> errors) {
            return new() { 
                Errors = errors 
            };
        }

        public static UnitResult ValidationError(IEnumerable<ValidationFailure> validationFailures) {
            return new() { 
                ValidationFailures = validationFailures 
            };
        }
    }

    /// <summary>
    /// A UnitResult, but includes 'Data'.
    /// </summary>
    public class Result<T> : UnitResult {
        public T? Data { get; set; }

        public static Result<T> Success(T data) {
            return new() { 
                Data = data 
            };
        }

        public new static Result<T> Error(string error) {
            return new() { 
                Errors = new List<string> { 
                    error 
                } 
            };
        }

        public new static Result<T> Error(IEnumerable<string> errors) {
            return new() {
                Errors = errors
            };
        }
    }

    /// <summary>
    /// A Result object containing all the common pagination params.
    /// </summary>
    public class PaginatedResult<T> : Result<T> {
        public int PageNumber { get; set; }
        public int ItemsPerPage { get; set; }
        public int ResultsCount { get; set; }
        public int TotalResultsCount { get; set; }
        public int TotalPages { get; set; }
        public new IEnumerable<T>? Data { get; set; }

        public static PaginatedResult<T> Success(IEnumerable<T> data) {
            return new() { 
                Data = data 
            };
        }

        public new static PaginatedResult<T> Error(string error) {
            return new() { 
                Errors = new List<string> { 
                    error 
                } 
            };
        }

        public new static PaginatedResult<T> Error(IEnumerable<string> errors) {
            return new() {
                Errors = errors
            };
        }
    }
}
