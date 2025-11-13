using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.Data
    {
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options) { }

            public DbSet<Claim> Claims { get; set; }
        }
    }

