using accs.Models.Statuses.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace accs.Models.States
{
    [EntityTypeConfiguration(typeof(RetirementConfiguration))]
    [Table("Retirements")]
    public class Retirement : UnitState
    {
        public override string GetText()
        {
            return "Отставка";
        }

        public override string? GetHexColor()
        {
            return "#333333";
        }
    }

	public class RetirementConfiguration : IEntityTypeConfiguration<Retirement>
	{
		public void Configure(EntityTypeBuilder<Retirement> builder)
		{

		}
	}
}
