using FluentValidation;
using api.models;

namespace api.validators
{
    public class PartidaValidator : AbstractValidator<Partida>
    {
        public PartidaValidator()
        {
            RuleFor(p => p.Mapa)
                .NotEmpty().WithMessage("O mapa e obrigatorio.")
                .MaximumLength(50).WithMessage("Nome do mapa muito longo.");

            RuleFor(p => p.Resultado)
                .NotEmpty().WithMessage("O resultado e obrigatorio.")
                .Must(r => r == "Vitoria" || r == "Derrota")
                .WithMessage("Resultado deve ser 'Vitoria' ou 'Derrota'.");

            RuleFor(p => p.Kills)
                .GreaterThanOrEqualTo(0).WithMessage("Kills nao pode ser negativo.");

            RuleFor(p => p.Deaths)
                .GreaterThanOrEqualTo(0).WithMessage("Deaths nao pode ser negativo.");

            RuleFor(p => p.Assists)
                .GreaterThanOrEqualTo(0).WithMessage("Assists nao pode ser negativo.");
        }
    }
}