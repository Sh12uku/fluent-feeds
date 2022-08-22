﻿// <auto-generated />
using System;
using FluentFeeds.App.Shared.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FluentFeeds.App.Shared.Migrations
{
    [DbContext(typeof(AppDbContext))]
    partial class AppDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.8");

            modelBuilder.Entity("FluentFeeds.App.Shared.Models.Database.FeedNodeDb", b =>
                {
                    b.Property<Guid>("Identifier")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Feed")
                        .HasColumnType("TEXT");

                    b.Property<bool>("HasChildren")
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsUserCustomizable")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("ParentIdentifier")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Symbol")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .HasColumnType("TEXT");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Identifier");

                    b.HasIndex("ParentIdentifier");

                    b.ToTable("FeedNodes");
                });

            modelBuilder.Entity("FluentFeeds.App.Shared.Models.Database.FeedProviderDb", b =>
                {
                    b.Property<Guid>("Identifier")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("RootNodeIdentifier")
                        .HasColumnType("TEXT");

                    b.HasKey("Identifier");

                    b.HasIndex("RootNodeIdentifier");

                    b.ToTable("FeedProviders");
                });

            modelBuilder.Entity("FluentFeeds.App.Shared.Models.Database.ItemDb", b =>
                {
                    b.Property<Guid>("Identifier")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Author")
                        .HasColumnType("TEXT");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ContentUrl")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsRead")
                        .HasColumnType("INTEGER");

                    b.Property<DateTimeOffset>("ModifiedTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProviderIdentifier")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("PublishedTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("StorageIdentifier")
                        .HasColumnType("TEXT");

                    b.Property<string>("Summary")
                        .HasColumnType("TEXT");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Url")
                        .HasColumnType("TEXT");

                    b.HasKey("Identifier");

                    b.HasIndex("ProviderIdentifier", "StorageIdentifier");

                    b.ToTable("Items");
                });

            modelBuilder.Entity("FluentFeeds.App.Shared.Models.Database.FeedNodeDb", b =>
                {
                    b.HasOne("FluentFeeds.App.Shared.Models.Database.FeedNodeDb", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentIdentifier");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("FluentFeeds.App.Shared.Models.Database.FeedProviderDb", b =>
                {
                    b.HasOne("FluentFeeds.App.Shared.Models.Database.FeedNodeDb", "RootNode")
                        .WithMany()
                        .HasForeignKey("RootNodeIdentifier")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RootNode");
                });
#pragma warning restore 612, 618
        }
    }
}
