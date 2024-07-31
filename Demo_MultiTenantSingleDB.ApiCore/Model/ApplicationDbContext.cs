using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Demo_MultiTenantSingleDB.ApiCore.Model
{
    public class ApplicationDbContext : DbContext
    {
        private readonly int? _currentTenantId;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, int? currentTenantId = null)
            : base(options)
        {
            _currentTenantId = currentTenantId;
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            if (_currentTenantId.HasValue)
            {
                ApplyGlobalQueryFilters(modelBuilder);
            }
        }
        private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
        {
            var entityTypes = modelBuilder.Model.GetEntityTypes()
                .Where(e => e.ClrType.GetProperty("TenantId") != null);

            foreach (var entityType in entityTypes)
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");

                // Condition: e.TenantId == null || e.TenantId == 0 || e.TenantId == _currentTenantId
                var property = Expression.Property(parameter, "TenantId");
                var nullCheck = Expression.Equal(property, Expression.Constant(null, typeof(int?)));
                var zeroCheck = Expression.Equal(property, Expression.Constant(0, typeof(int?)));
                var tenantIdCheck = Expression.Equal(property, Expression.Constant(_currentTenantId, typeof(int?)));
                var filterCondition = Expression.OrElse(Expression.OrElse(nullCheck, zeroCheck), tenantIdCheck);

                var lambda = Expression.Lambda(filterCondition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }


        }
    }

    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<User> Users { get; set; }

    }

    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ICollection<User> Users { get; set; }

    }

    public class User
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int RoleId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public Tenant Tenant { get; set; }
        public Role Role { get; set; }
    }
}
