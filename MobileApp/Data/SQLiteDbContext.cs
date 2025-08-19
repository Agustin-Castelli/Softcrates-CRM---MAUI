using Microsoft.EntityFrameworkCore;

using MobileApp.Models;
using MobileApp.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileApp.Data
{
    public class SQLiteDbContext : DbContext
    {
        public SQLiteDbContext(DbContextOptions<SQLiteDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Factura> Facturas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración extra si las tablas tienen nombres diferentes
            modelBuilder.Entity<User>().ToTable("sisusuar");
            modelBuilder.Entity<Client>().ToTable("Clien");
            modelBuilder.Entity<Factura>().ToTable("deudeu");


            // Mapeo de tabla Clien
            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("Clien");
                entity.HasKey(e => e.CodCli)
                      .HasName("PK_Cliente");

                entity.Property(e => e.CodCli).HasColumnName("codcli");
                entity.Property(e => e.NomCli).HasColumnName("nomcli");
                entity.Property(e => e.SaldoDeuCc).HasColumnName("saldodeucc");
                entity.Property(e => e.SaldoVencCc).HasColumnName("saldovencc");
                entity.Property(e => e.LimiteCredito).HasColumnName("limcrecli");
                entity.Property(e => e.LimiteCreditoUso).HasColumnName("limcrecliuso");
            });

            // Mapeo de tabla sisusuar
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("sisusuar");
                entity.HasKey(e => e.CodUsr)
                      .HasName("PK_Usuario");

                entity.Property(e => e.CodUsr).HasColumnName("codusr");
                entity.Property(e => e.NomUsr).HasColumnName("nomusr");
                entity.Property(e => e.AdmUsr).HasColumnName("admusr");
                entity.Property(e => e.PwdUsr).HasColumnName("pwdusr");
                entity.Property(e => e.Inactivo).HasColumnName("inactivo");
                entity.Property(e => e.CodGrp).HasColumnName("codgrp");
            });

            // Mapeo de tabla deudeu
            modelBuilder.Entity<Factura>(entity =>
            {
                entity.ToTable("deudeu");
                entity.HasKey(e => e.CSIDcbtDeu)
                      .HasName("PK_Factura");  // RECORDAR QUE LA TABLA FACTURAS (deudeu) NO TIENE PK. HABLAR CON ARI.

                entity.Property(e => e.CSIDcbtDeu).HasColumnName("CSIDcbtdeu");
                entity.Property(e => e.FechaVto).HasColumnName("fecvtodeu");
                entity.Property(e => e.CodCli).HasColumnName("codcli");
                entity.Property(e => e.NroDocCli).HasColumnName("nrodoccli");
                entity.Property(e => e.CodTipoCbt).HasColumnName("codtipcbtdeu");
                entity.Property(e => e.CemCbt).HasColumnName("cemcbtdeu");
                entity.Property(e => e.NroCbt).HasColumnName("nrocbtdeu");
                entity.Property(e => e.FechaCbt).HasColumnName("feccbtdeu");
                entity.Property(e => e.CodCta).HasColumnName("codctadeu");
                entity.Property(e => e.CodMon).HasColumnName("codmon");
                entity.Property(e => e.ImpCot).HasColumnName("impcot");
                entity.Property(e => e.ImporteOriginal).HasColumnName("imporideu");
                entity.Property(e => e.Saldo).HasColumnName("saldodeu");
                entity.Property(e => e.Observaciones).HasColumnName("obsdeu");
                entity.Property(e => e.CodVen).HasColumnName("codven");
                entity.Property(e => e.CodZonaVta).HasColumnName("codzonvta");
                entity.Property(e => e.ImpIntCob).HasColumnName("impintcobdeu");
                entity.Property(e => e.CobInt).HasColumnName("cobintdeu");
                entity.Property(e => e.DifCot).HasColumnName("difcot");
                entity.Property(e => e.FechaCbtOri).HasColumnName("feccbtori");
                entity.Property(e => e.CodVen).HasColumnName("codven");
                entity.Property(e => e.CSIDci).HasColumnName("csidci");
                entity.Property(e => e.IntMora).HasColumnName("intmora");
                entity.Property(e => e.ImpIntDeu).HasColumnName("impintdeu");
                entity.Property(e => e.CodVen).HasColumnName("codven");
                entity.Property(e => e.NroCuo).HasColumnName("nrocuodeu");
                entity.Property(e => e.CSIDndDev).HasColumnName("CSIDnddev");
                entity.Property(e => e.ImpIf).HasColumnName("impifdeu");
                entity.Property(e => e.IvaIf).HasColumnName("ivaifdeu");
                entity.Property(e => e.FechaAnuPorCi).HasColumnName("fecanuporci");
                entity.Property(e => e.DtoRec).HasColumnName("dtorec");
                entity.Property(e => e.CodSuc).HasColumnName("codsuc");
                entity.Property(e => e.CodDimEle1).HasColumnName("coddimele1");
                entity.Property(e => e.FechaCasFlo).HasColumnName("feccasflo");
                entity.Property(e => e.ExcDeuFrgRec).HasColumnName("excdeufrgrec");
                entity.Property(e => e.ClaveEmp).HasColumnName("claveemp");
                entity.Property(e => e.EstadoCC).HasColumnName("estadocc");
            });
        }
    }
}

