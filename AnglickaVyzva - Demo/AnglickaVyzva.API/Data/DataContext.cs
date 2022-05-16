using AnglickaVyzva.API.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AnglickaVyzva.API.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Progress> Progresses { get; set; }
        public DbSet<TestProgress> TestProgresses { get; set; }
        public DbSet<ChestPointsUse> ChestPointsUses { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<NotificationToken> NotificationTokens { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<ChallengeUserRelation> ChallengeUserRelations { get; set; }
        public DbSet<PersonalChallenge> PersonalChallenges { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<ActivationCode> ActivationCodes { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<TopicSection> TopicSections { get; set; }
        public DbSet<TopicSet> TopicSets { get; set; }
        public DbSet<TopicItem> TopicItems { get; set; }
        public DbSet<TopicPoints> TopicPoints { get; set; }
        public DbSet<TopicItem_User> TopicItems_Users { get; set; }


        //protected override void OnModelCreating(ModelBuilder builder)
        //{
        //    // Nastavi email jako unikatni
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.UserName);
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.GoogleEmail);
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.GoogleUserId);
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.FacebookEmail);
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.FacebookUserId);
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.AppleEmail);
        //    //builder.Entity<User>()
        //    //    .HasAlternateKey(x => x.AppleUserId);
        //}
    }
}
