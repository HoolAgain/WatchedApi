using Microsoft.EntityFrameworkCore;
using WatchedApi.Data.Models;

namespace WatchedApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<PostLike> PostLikes { get; set; }
        public DbSet<AdminLog> AdminLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PostLike>()
                .HasIndex(pl => new { pl.UserId, pl.PostId })
                .IsUnique();

            modelBuilder.Entity<Post>()
                .HasOne(p => p.User)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.Post)
                .WithMany(p => p.PostLikes)
                .HasForeignKey(pl => pl.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PostLike>()
                .HasOne(pl => pl.User)
                .WithMany(u => u.PostLikes)
                .HasForeignKey(pl => pl.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminLog>()
                .HasOne(al => al.Admin)
                .WithMany()
                .HasForeignKey(al => al.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdminLog>()
                .HasOne(al => al.TargetUser)
                .WithMany()
                .HasForeignKey(al => al.TargetUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminLog>()
                .HasOne(al => al.TargetPost)
                .WithMany()
                .HasForeignKey(al => al.TargetPostId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdminLog>()
                .HasOne(al => al.TargetComment)
                .WithMany()
                .HasForeignKey(al => al.TargetCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
