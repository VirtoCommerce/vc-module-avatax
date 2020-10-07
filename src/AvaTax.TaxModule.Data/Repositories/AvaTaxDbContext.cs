using Microsoft.EntityFrameworkCore;
using VirtoCommerce.TaxModule.Data.Repositories;

namespace AvaTax.TaxModule.Data.Repositories
{
    public class AvaTaxDbContext : TaxDbContext
    {
        public AvaTaxDbContext(DbContextOptions<AvaTaxDbContext> options) : base(options)
        {
        }
    }
}
