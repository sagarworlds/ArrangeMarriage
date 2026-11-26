using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Domain.Entities;

namespace ArrangeMarriage.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Profile> Profiles { get; set; } = null!;
        public DbSet<FamilyDetail> FamilyDetails { get; set; } = null!;
        public DbSet<PartnerPreference> PartnerPreferences { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<MembershipPackage> MembershipPackages { get; set; } = null!;
        public DbSet<Subscription> Subscriptions { get; set; } = null!;
        public DbSet<Proposal> Proposals { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Meeting> Meetings { get; set; } = null!;
        public DbSet<MeetingFeedback> MeetingFeedbacks { get; set; } = null!;
        public DbSet<MarriageSuccess> MarriageSuccesses { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // EF's convention only recognises "Id" or "<TypeName>Id" as a primary key. These entities use
            // shorter key names (matching the SQL schema), so without explicit keys EF fails to build the
            // model and every database call throws.
            modelBuilder.Entity<MarriageSuccess>().HasKey(e => e.SuccessId);
            modelBuilder.Entity<MeetingFeedback>().HasKey(e => e.FeedbackId);
            modelBuilder.Entity<MembershipPackage>().HasKey(e => e.PackageId);
            modelBuilder.Entity<PartnerPreference>().HasKey(e => e.PreferenceId);

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.PhoneNumber).IsUnique();

            modelBuilder.Entity<Profile>()
                .HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserId);

            modelBuilder.Entity<FamilyDetail>()
                .HasOne(f => f.Profile)
                .WithOne(p => p.FamilyDetail)
                .HasForeignKey<FamilyDetail>(f => f.ProfileId);

            modelBuilder.Entity<PartnerPreference>()
                .HasOne(pp => pp.Profile)
                .WithOne(p => p.PartnerPreference)
                .HasForeignKey<PartnerPreference>(pp => pp.ProfileId);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
