using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace AuditGeneratorAPI.Models;

public partial class AuditGeneratorDbContext : DbContext
{
    public AuditGeneratorDbContext()
    {
    }

    public AuditGeneratorDbContext(DbContextOptions<AuditGeneratorDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AssignedRoom> AssignedRooms { get; set; }

    public virtual DbSet<Campus> Campuses { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<Qualification> Qualifications { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<RoomSchedule> RoomSchedules { get; set; }

    public virtual DbSet<StudyPractice> StudyPractices { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherGroup> TeacherGroups { get; set; }

    public virtual DbSet<WeekType> WeekTypes { get; set; }

    public virtual DbSet<Weekday> Weekdays { get; set; }

    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssignedRoom>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Assigned__3214EC27278ECDFC");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");

            entity.HasOne(d => d.Room).WithMany(p => p.AssignedRooms)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__AssignedR__RoomI__44FF419A");

            entity.HasOne(d => d.Teacher).WithMany(p => p.AssignedRooms)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__AssignedR__Teach__45F365D3");
        });

        modelBuilder.Entity<Campus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Campuses__3214EC274B9E2A8D");

            entity.HasIndex(e => e.Name, "UQ__Campuses__737584F6EF268EA1").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(255);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Events__3214EC27D8C53886");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.LessonRange).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.RoomId).HasColumnName("RoomID");

            
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Groups__3214EC27AACF9688");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Abbreviation).HasMaxLength(50);
            entity.Property(e => e.AssignedCells).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.QualificationId).HasColumnName("QualificationID");

            /*entity.HasOne(d => d.Qualification).WithMany(p => p.Groups)
                .HasForeignKey(d => d.QualificationId)
                .HasConstraintName("FK__Groups__Qualific__403A8C7D");*/
        });

        modelBuilder.Entity<Qualification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Qualific__3214EC2744B94684");

            entity.HasIndex(e => e.Code, "UQ__Qualific__A25C5AA7490DC5C6").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Qualification1)
                .HasMaxLength(255)
                .HasColumnName("Qualification");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC27A5DEEB32");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CampusId).HasColumnName("CampusID");
            entity.Property(e => e.RoomNumber).HasMaxLength(50);

            entity.HasOne(d => d.Campus).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.CampusId)
                .HasConstraintName("FK__Rooms__CampusID__3A81B327");
        });

        modelBuilder.Entity<RoomSchedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RoomSche__3214EC27B04FBB90");

            entity.ToTable("RoomSchedule");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CampusId).HasColumnName("CampusID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.RoomId).HasColumnName("RoomID");
            entity.Property(e => e.WeekTypeId).HasColumnName("WeekTypeID");
            entity.Property(e => e.WeekdayId).HasColumnName("WeekdayID");

            entity.HasOne(d => d.Campus).WithMany(p => p.RoomSchedules)
                .HasForeignKey(d => d.CampusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RoomSched__Campu__59063A47");

            /*entity.HasOne(d => d.Group).WithMany(p => p.RoomSchedules)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK__RoomSched__Group__5535A963");*/

            entity.HasOne(d => d.Room).WithMany(p => p.RoomSchedules)
                .HasForeignKey(d => d.RoomId)
                .HasConstraintName("FK__RoomSched__RoomI__5629CD9C");

            entity.HasOne(d => d.WeekType).WithMany(p => p.RoomSchedules)
                .HasForeignKey(d => d.WeekTypeId)
                .HasConstraintName("FK__RoomSched__WeekT__571DF1D5");

            entity.HasOne(d => d.Weekday).WithMany(p => p.RoomSchedules)
                .HasForeignKey(d => d.WeekdayId)
                .HasConstraintName("FK__RoomSched__Weekd__5812160E");
        });

        modelBuilder.Entity<StudyPractice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudyPra__3214EC2746DE77D5");

            entity.ToTable("StudyPractice");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.LessonRange).HasMaxLength(50);
            entity.Property(e => e.RoomId).HasColumnName("RoomID");


        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teachers__3214EC275539BBB7");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.FullName).HasMaxLength(255);
        });

        modelBuilder.Entity<TeacherGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TeacherG__3214EC27D9DF055B");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.GroupId).HasColumnName("GroupID");
            entity.Property(e => e.TeacherId).HasColumnName("TeacherID");

            /*entity.HasOne(d => d.Group).WithMany(p => p.TeacherGroups)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("FK__TeacherGr__Group__628FA481");*/

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherGroups)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("FK__TeacherGr__Teach__6383C8BA");
        });

        modelBuilder.Entity<WeekType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__WeekType__3214EC2724182C43");

            entity.ToTable("WeekType");

            entity.HasIndex(e => e.Name, "UQ__WeekType__737584F6EB788CB8").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<Weekday>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Weekdays__3214EC272CEC2DE2");

            entity.HasIndex(e => e.Name, "UQ__Weekdays__737584F61ECA7AEC").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
