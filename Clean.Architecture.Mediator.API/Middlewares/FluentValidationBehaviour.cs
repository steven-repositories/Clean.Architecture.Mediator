using FluentValidation;
using MediatR;

namespace Clean.Architecture.Mediator.API.Middlewares {
    public class FluentValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public FluentValidationBehaviour(IEnumerable<IValidator<TRequest>> validators) {
            _validators = validators;
        }

        public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken token) {
            var context = new ValidationContext<TRequest>(request);

            var failures = _validators
                .Select(validtor => validtor.Validate(context))
                .SelectMany(result => result.Errors)
                .Where(error => error != null)
                .ToList();

            if (failures.Count > 0) {
                throw new ValidationException(failures);
            }

            return next();
        }
    }
}
