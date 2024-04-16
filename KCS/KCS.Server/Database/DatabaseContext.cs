using Microsoft.EntityFrameworkCore;
using KCS.Server.Database.Models;

namespace KCS.Server.Database
{
    public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Models.Configuration> Configurations { get; set; }
        public DbSet<FilterWord> FilterWords { get; set; }
        public DbSet<BotInfo> Bots { get; set; }
        public DbSet<InviteCode> InviteCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Username).HasColumnName("username").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Password).HasColumnName("password").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Admin).HasColumnName("admin");
                entity.Property(e => e.Paused).HasColumnName("paused");
                entity.Property(e => e.LastOnline).HasColumnName("last_online").HasColumnType("timestamp").HasDefaultValue(TimeHelper.GetUnspecifiedUtc());

                entity.HasOne(e => e.Configuration)
                    .WithOne()
                    .HasForeignKey<Models.Configuration>(c => c.Id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Sessions)
                    .WithOne()
                    .HasForeignKey(s => s.Id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Logs)
                    .WithOne()
                    .HasForeignKey(l => l.Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Session
            modelBuilder.Entity<Session>(entity =>
            {
                entity.ToTable("sessions");
                entity.HasKey(e => e.AuthToken);
                entity.Property(e => e.AuthToken).HasColumnName("auth_token").HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Expires).HasColumnName("expires").HasColumnType("timestamp");
            });

            // Log
            modelBuilder.Entity<Log>(entity =>
            {
                entity.ToTable("logs");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).HasColumnName("log_id");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.Time).HasColumnName("time").HasColumnType("timestamp");
                entity.Property(e => e.Type).HasColumnName("type");
            });

            // Configuration
            modelBuilder.Entity<Models.Configuration>(entity =>
            {
                entity.ToTable("configurations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Tokens).HasColumnType("jsonb").HasColumnName("tokens");
                entity.Property(e => e.StreamerUsername).HasMaxLength(50).HasColumnName("streamerUsername");
                entity.Property(e => e.SpamTemplates).HasColumnType("jsonb").HasColumnName("spamTemplates");
                entity.Property(e => e.Binds).HasColumnType("jsonb").HasColumnName("binds");
            });

            // FilterWord
            modelBuilder.Entity<FilterWord>(entity =>
            {
                entity.ToTable("filter_words");
                entity.HasKey(e => e.Word);
                entity.Property(e => e.Word).HasColumnName("word");
            });

            // BotsInfo
            modelBuilder.Entity<BotInfo>(entity =>
            {
                entity.ToTable("bots_info");
                entity.HasKey(e => e.Username);
                //entity.HasAlternateKey(e => e.Username);
                //entity.Property(e => e.Token).HasColumnName("token");
                entity.Property(e => e.Username).HasColumnName("username");
                entity.Property(e => e.Followed).HasColumnName("followed");
            });

            // InviteCodes
            modelBuilder.Entity<InviteCode>(entity =>
            {
                entity.ToTable("invite_codes");
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).HasColumnName("code");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Expires).HasColumnName("expires").HasColumnType("timestamp");
                entity.Property(e => e.ActivationDate).HasColumnName("activation_date").HasColumnType("timestamp");
                entity.Property(e => e.Mode).HasColumnName("mode");
            });
        }
        internal async Task AddLog(int id, string message, LogType type)
        {
            await Logs.AddAsync(new Log
            {
                Id = id,
                Message = message,
                Time = TimeHelper.GetUnspecifiedUtc(),
                Type = type
            });
        }
        internal async Task AddLog(User user, string message, LogType type)
        {
            await AddLog(user.Id, message, type);
        }
        internal async Task<int> GetId(Guid auth_token)
        {
            return await Sessions.Where(x => x.AuthToken == auth_token).Select(x => x.Id).FirstOrDefaultAsync();
        }
        internal async Task<User> GetUser(Guid auth_token)
        {
            return await Users.FirstAsync(x => Sessions.First(y => y.AuthToken == auth_token).Id == x.Id);
        }
        internal async Task<Models.Configuration> GetConfiguration(Guid auth_token)
        {
            return await Configurations.FirstAsync(x => Sessions.First(y => y.AuthToken == auth_token).Id == x.Id);
        }
        internal async Task<bool> CheckMessageFilter(string message)
        {
            // true if message contains any filter word
            var words = message.Split(' ');
            return await FilterWords.AnyAsync(x => words.Contains(x.Word));
        }
        internal async Task<bool> CheckMessageFilter(string[] message)
        {
            return await FilterWords.AnyAsync(x => message.Contains(x.Word));
        }
    }
}
