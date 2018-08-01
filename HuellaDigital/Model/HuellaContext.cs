namespace HuellaDigital.Model
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class HuellaContext : DbContext
    {
        public HuellaContext()
            : base("name=HuellaContext")
        {
        }

        public virtual DbSet<Huella> Huella { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Huella>()
                .Property(e => e.nombre)
                .IsUnicode(false);
        }
    }
}
