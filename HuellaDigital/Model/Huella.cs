namespace HuellaDigital.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;
    using System.Data.Entity.Spatial;
    using System.Linq;

    [Table("Huella")]
    public partial class Huella
    {
        public int Id { get; set; }

        [StringLength(50)]
        public string nombre { get; set; }

        [Column("Huella")]
        public byte[] Huella1 { get; set; }



        //metodo para listar huellas
        public List<Huella> Listar()
        {
            var list = new List<Huella>();

            try
            {
                using(var ctx = new HuellaContext())
                {
                    list = ctx.Huella.ToList();
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return list;
        }


        //metodo para guardar cadena de bytes en base de datos
        public bool Guardar()
        {
            bool result = false;
            try
            {
                using(var ctx = new HuellaContext())
                {
                    ctx.Entry(this).State = EntityState.Added;
                    ctx.SaveChanges();
                    result = true;
                }
            }
            catch(Exception e)
            {
                throw e;
            }
            return result;
        }
    }
}
