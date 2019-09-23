using Microsoft.EntityFrameworkCore;
using Nap.PrototypingApi.Models;

namespace Nap.PrototypingApi.Data
{
	public sealed class PrototypingDbContext : DbContext
	{
		public DbSet<Cat>? Cats { get; set; }

		public PrototypingDbContext()
		{
		}

		public PrototypingDbContext(DbContextOptions<PrototypingDbContext> options) : base(options)
		{
		}
	}
}
