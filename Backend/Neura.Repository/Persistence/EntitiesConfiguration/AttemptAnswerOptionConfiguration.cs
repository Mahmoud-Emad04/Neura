using Neura.Core.Entities;

namespace Neura.Repository.Persistence.EntitiesConfiguration;


public class AttemptAnswerOptionConfiguration : IEntityTypeConfiguration<AttemptAnswerOption>
{
    public void Configure(EntityTypeBuilder<AttemptAnswerOption> builder)
    {
        builder.HasKey(aao => new { aao.AttemptAnswerId, aao.AnswerOptionId });

        builder.HasOne(aao => aao.AnswerOption)
               .WithMany(ao => ao.AttemptAnswerOptions)
               .HasForeignKey(aao => aao.AnswerOptionId);
    }
}