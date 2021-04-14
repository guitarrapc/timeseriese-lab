﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SampleConsole.Data;

namespace SampleConsole.Migrations
{
    [DbContext(typeof(TimescaleDbContext))]
    [Migration("20210414042823_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.4")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("SampleConsole.Models.Condition", b =>
                {
                    b.Property<double?>("Humidity")
                        .HasColumnType("double precision")
                        .HasColumnName("humidity");

                    b.Property<string>("Location")
                        .HasColumnType("text")
                        .HasColumnName("location");

                    b.Property<double?>("Temperature")
                        .HasColumnType("double precision")
                        .HasColumnName("temperature");

                    b.Property<DateTime>("Time")
                        .HasColumnType("timestamp without time zone")
                        .HasColumnName("time");

                    b.ToTable("conditions");
                });
#pragma warning restore 612, 618
        }
    }
}