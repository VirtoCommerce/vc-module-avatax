using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AvaTax.TaxModule.Data.Repositories
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AvaTaxDbContext>
    {
        public AvaTaxDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AvaTaxDbContext>();

            builder.UseSqlServer("Data Source=(local);Initial Catalog=VirtoCommerce3;Persist Security Info=True;User ID=virto;Password=virto;MultipleActiveResultSets=True;Connect Timeout=30");

            return new AvaTaxDbContext(builder.Options);
        }
    }
}
