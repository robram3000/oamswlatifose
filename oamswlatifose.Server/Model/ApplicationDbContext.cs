using Microsoft.EntityFrameworkCore;

using oamswlatifose.Server.Model.user;
using oamswlatifose.Server.Model.occurance;

namespace oamswlatifose.Server.Model
{
    public class ApplicationDbContext : DbContext
    {

        public DbSet<EMEmployees> EMEmployees { get; set; }
        public DbSet<EMAttendance> EMAttendance { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

}
