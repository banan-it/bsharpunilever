﻿// <auto-generated />
using System;
using BSharpUnilever.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BSharpUnilever.Migrations
{
    [DbContext(typeof(BSharpContext))]
    partial class BSharpContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.1.3-rtm-32065")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("BSharpUnilever.Data.Entities.GeneratedDocument", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTime>("Date");

                    b.Property<int>("SerialNumber");

                    b.Property<int>("State");

                    b.Property<int>("SupportRequestId");

                    b.HasKey("Id");

                    b.HasIndex("SupportRequestId");

                    b.ToTable("GeneratedDocuments");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Barcode")
                        .HasMaxLength(255);

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<bool>("IsActive");

                    b.Property<bool>("IsPromo");

                    b.Property<string>("SapCode")
                        .HasMaxLength(255);

                    b.Property<string>("Type")
                        .HasMaxLength(255);

                    b.HasKey("Id");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.StateChange", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("FromState")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<int>("SupportRequestId");

                    b.Property<DateTimeOffset>("Time");

                    b.Property<string>("ToState")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("UserId");

                    b.Property<string>("UserRole");

                    b.HasKey("Id");

                    b.HasIndex("SupportRequestId");

                    b.HasIndex("UserId");

                    b.ToTable("StateChanges");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.Store", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AccountExecutiveId");

                    b.Property<bool>("IsActive");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.HasKey("Id");

                    b.HasIndex("AccountExecutiveId");

                    b.ToTable("Stores");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.SupportRequest", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("AccountExecutiveId")
                        .IsRequired();

                    b.Property<string>("Comment")
                        .HasMaxLength(1023);

                    b.Property<DateTimeOffset>("Created");

                    b.Property<string>("CreatedBy");

                    b.Property<DateTime>("Date");

                    b.Property<string>("ManagerId")
                        .IsRequired();

                    b.Property<DateTimeOffset>("Modified");

                    b.Property<string>("ModifiedBy");

                    b.Property<string>("Reason")
                        .IsRequired();

                    b.Property<int>("SerialNumber");

                    b.Property<string>("State")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<int>("StoreId");

                    b.HasKey("Id");

                    b.HasIndex("AccountExecutiveId");

                    b.HasIndex("ManagerId");

                    b.HasIndex("SerialNumber")
                        .IsUnique();

                    b.HasIndex("StoreId");

                    b.ToTable("SupportRequests");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.SupportRequestLineItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("ApprovedSupport")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<decimal>("ApprovedValue")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<int?>("ProductId");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<decimal>("RequestedSupport")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<decimal>("RequestedValue")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<int>("SupportRequestId");

                    b.Property<decimal>("UsedSupport")
                        .HasColumnType("decimal(18, 2)");

                    b.Property<decimal>("UsedValue")
                        .HasColumnType("decimal(18, 2)");

                    b.HasKey("Id");

                    b.HasIndex("ProductId");

                    b.HasIndex("SupportRequestId");

                    b.ToTable("SupportRequestLineItems");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.User", b =>
                {
                    b.Property<string>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AccessFailedCount");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken();

                    b.Property<string>("Email")
                        .HasMaxLength(256);

                    b.Property<bool>("EmailConfirmed");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<bool>("LockoutEnabled");

                    b.Property<DateTimeOffset?>("LockoutEnd");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256);

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256);

                    b.Property<string>("PasswordHash");

                    b.Property<string>("PhoneNumber");

                    b.Property<bool>("PhoneNumberConfirmed");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(255);

                    b.Property<string>("SecurityStamp");

                    b.Property<bool>("TwoFactorEnabled");

                    b.Property<string>("UserName")
                        .HasMaxLength(256);

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasName("UserNameIndex")
                        .HasFilter("[NormalizedUserName] IS NOT NULL");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("ClaimType");

                    b.Property<string>("ClaimValue");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.Property<string>("LoginProvider");

                    b.Property<string>("ProviderKey");

                    b.Property<string>("ProviderDisplayName");

                    b.Property<string>("UserId")
                        .IsRequired();

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.Property<string>("UserId");

                    b.Property<string>("LoginProvider");

                    b.Property<string>("Name");

                    b.Property<string>("Value");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.GeneratedDocument", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.SupportRequest", "SupportRequest")
                        .WithMany("GeneratedDocuments")
                        .HasForeignKey("SupportRequestId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.StateChange", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.SupportRequest", "SupportRequest")
                        .WithMany("StateChanges")
                        .HasForeignKey("SupportRequestId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("BSharpUnilever.Data.Entities.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId");
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.Store", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.User", "AccountExecutive")
                        .WithMany("AssignedStores")
                        .HasForeignKey("AccountExecutiveId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.SupportRequest", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.User", "AccountExecutive")
                        .WithMany("Requests")
                        .HasForeignKey("AccountExecutiveId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("BSharpUnilever.Data.Entities.User", "Manager")
                        .WithMany("ManagedRequests")
                        .HasForeignKey("ManagerId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("BSharpUnilever.Data.Entities.Store", "Store")
                        .WithMany("SupportRequests")
                        .HasForeignKey("StoreId")
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity("BSharpUnilever.Data.Entities.SupportRequestLineItem", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.Product", "Product")
                        .WithMany("SupportRequestLineItems")
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("BSharpUnilever.Data.Entities.SupportRequest", "SupportRequest")
                        .WithMany("LineItems")
                        .HasForeignKey("SupportRequestId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<string>", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<string>", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<string>", b =>
                {
                    b.HasOne("BSharpUnilever.Data.Entities.User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
