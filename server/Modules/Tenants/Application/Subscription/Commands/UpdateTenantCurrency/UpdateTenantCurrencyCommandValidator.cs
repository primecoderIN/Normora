using FluentValidation;
using Normora.Modules.Tenants.Domain;
using Normora.Shared.Constants;

namespace Normora.Modules.Tenants.Application.Subscription.Commands.UpdateTenantCurrency;

public class UpdateTenantCurrencyCommandValidator : AbstractValidator<UpdateTenantCurrencyCommand>
{
    public UpdateTenantCurrencyCommandValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage(ApiMessages.InvalidCurrency)
            .Must(TenantPlanPricingCatalog.IsSupportedCurrency)
            .WithMessage(ApiMessages.InvalidCurrency);
    }
}
