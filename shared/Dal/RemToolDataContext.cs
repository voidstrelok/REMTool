using Microsoft.EntityFrameworkCore;

namespace RemTool.Shared
{
    public partial class RemToolDataContext : DbContext
    {
        public RemToolDataContext(DbContextOptions<RemToolDataContext> options) : base(options)
        {
        }

        public virtual DbSet<HojaRem> HojaRem { get; set; }

        public virtual DbSet<VersionHojaRem> VersionHojaRem { get; set; }

        public virtual DbSet<Prestacion> Prestacion { get; set; }

        public virtual DbSet<SeccionRem> SeccionRem { get; set; }

        public virtual DbSet<FilaSeccionRem> FilaSeccionRem { get; set; }

        public virtual DbSet<CeldaSeccionRem> CeldaSeccionRem { get; set; }

        public virtual DbSet<CoordenadaPrestacion> CoordenadaPrestacion { get; set; }

        public virtual DbSet<SerieRem> SerieRem { get; set; }

        public virtual DbSet<VersionRem> VersionRem { get; set; }
        public virtual DbSet<Reporte> Reporte { get; set; }
        public virtual DbSet<Registro> Registro { get; set; }
        public virtual DbSet<Establecimiento> Establecimiento { get; set; }
        public virtual DbSet<Sector> Sector { get; set; }
        public virtual DbSet<Comuna> Comuna { get; set; }
        public virtual DbSet<ResultadoIndicador> ResultadoIndicador { get; set; }
        public virtual DbSet<Indicador> Indicador { get; set; }
        public virtual DbSet<TipoIndicador> TipoIndicador { get; set; }
        public virtual DbSet<Parametros> Parametros { get; set; }
        public virtual DbSet<TipoRegla> TipoRegla { get; set; }
        public virtual DbSet<Regla> Regla { get; set; }
        public virtual DbSet<Convenio> Convenio { get; set; }
        public virtual DbSet<IndicadorConvenio> IndicadorConvenio { get; set; }
        public virtual DbSet<PercapitaSsc> PercapitaSsc { get; set; }
        public virtual DbSet<FiltroEstablecimiento> FiltroEstablecimiento { get; set; }
        public virtual DbSet<PuntoResumen> PuntoResumen { get; set; }
        public virtual DbSet<Informativo> Informativo { get; set; }
        public virtual DbSet<ConvenioFinanciero> ConvenioFinanciero { get; set; }
        public virtual DbSet<ItemPresupuestario> ItemPresupuestario { get; set; }
        public virtual DbSet<MovimientoFinanciero> MovimientoFinanciero { get; set; }
        public virtual DbSet<SubItemPresupuestario> SubItemPresupuestario { get; set; }
        public virtual DbSet<ProveedorFinanciero> ProveedorFinanciero { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new SeccionRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new HojaRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new VersionHojaRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new FilaSeccionRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new CeldaSeccionRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new CoordenadaPrestacionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new SerieRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new VersionRemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new PrestacionEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new RegistroEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ReporteEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ComunaEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new EstablecimientoEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new SectorEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new IndicadorEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new TipoIndicadorEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ResultadoIndicadorEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ParametrosEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new TipoReglaEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ReglaEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ConvenioEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new IndicadorConvenioEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new PercapitaSscEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new FiltroEstablecimientoEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new PuntoResumenEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new InformativoEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ConvenioFinancieroEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ItemPresupuestarioEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new MovimientoFinancieroEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new SubItemPresupuestarioEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new ProveedorFinancieroEntityTypeConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
