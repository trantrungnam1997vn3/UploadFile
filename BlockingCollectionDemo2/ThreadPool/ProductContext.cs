using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadPool
{
    public partial class ProductContext : DbContext
    {

        public ProductContext() : base("name=MyDbConnectionString")
        {
        }

        public virtual DbSet<Product> Products { get; set; }
    }
}
