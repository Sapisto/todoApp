using Microsoft.EntityFrameworkCore;
using PracticeApi.Domain.Entities;

namespace PracticeApi.Persistent
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Todo> Todos { get; set; }
    }
}
