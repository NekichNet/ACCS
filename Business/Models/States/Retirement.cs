using Business.Models.States.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations.Schema;

namespace Business.Models.States
{
    [EntityTypeConfiguration(typeof(RetirementConfiguration))]
    [Table("Retirements")]
    public class Retirement : StateWithDoc
    {
        public override string GetText()
        {
            return "Оформлена отставка";
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
