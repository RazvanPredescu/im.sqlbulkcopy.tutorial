using System.Data.Entity;

namespace IM.BulkInsert.Tutorial.Entities
{
    public partial class SqlBulkCopyDbContext : DbContext
    {
        public SqlBulkCopyDbContext()
            : base("name=SqlBulkCopyDbContext")
        {
        }

        public virtual DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}
