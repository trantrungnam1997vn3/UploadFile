using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using ThreadPool_Ex.Models;

namespace ThreadPool_Ex
{

    public class ProductContext: DbContext
    {
        public ProductContext()
            : base("name=ThreadpoolEntities")
        {
        }

        public DbSet<Product> Products { get; set; }

    }
}