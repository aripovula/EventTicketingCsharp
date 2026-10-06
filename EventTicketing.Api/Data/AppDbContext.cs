using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
