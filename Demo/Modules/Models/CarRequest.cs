using FluentValidation;

namespace Demo.Modules.Models
{
    public class CarRequest
    {
        public string NumberPlate { get; set; }
    }

    public class CarRequestValidator : AbstractValidator<CarRequest>
    {
        public CarRequestValidator()
        {
            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(r => r.NumberPlate).NotNull().NotEmpty();
        }
    }
}