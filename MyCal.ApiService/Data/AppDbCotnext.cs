using Microsoft.EntityFrameworkCore;

namespace MyCal.ApiService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // todo: add dbset
}