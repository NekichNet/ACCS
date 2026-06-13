using accs.Models.SingleDayEvents.Abstraction;
using accs.Models.Statuses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.SingleDayEvents
{
    [Table("UnitDismissingEvents")]
    public class UnitDismissingEvent : EventWithInitiator
    {
        public override string GetText()
        {
            AssignedRank? initiatorRank = Initiator.AssignedRanks.FirstOrDefault(ar => ar.IsActive(DateTime));
            string rankName = initiatorRank == null ? "Без звания" : initiatorRank.Rank.Name;
			return $"Увольнение бойцом {rankName} {Initiator.Nickname}";
        }

        public override string GetHexColor()
        {
            return "#994444";
        }
    }

	public class UnitDismissingEventConfiguration : IEntityTypeConfiguration<UnitDismissingEvent>
	{
		public void Configure(EntityTypeBuilder<UnitDismissingEvent> builder)
		{

		}
	}
}
