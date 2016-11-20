using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using BaseLocal;                                //Contexto de la base ENTERA
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;                     //Para convertir xml
using System.Collections.ObjectModel;               //Para convertir xml
using System.ComponentModel;                        //Para convertir xml
using System.Threading.Tasks;

namespace AppCala
{

    class Sincronizacion
    {
        ServiceReference1.ServiceSoapClient servicio = new ServiceReference1.ServiceSoapClient();
        XDocument xmlDocSocios = new XDocument();
        XDocument xmlDocParametros = new XDocument();
        XDocument xmlVinculados = new XDocument();
        RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString);
        public int numorden = 0;

        public void RecibeMesas()
        {
            try
            {
                servicio.MesasDisponiblesCompleted += new EventHandler<ServiceReference1.MesasDisponiblesCompletedEventArgs>(servicio_MesasDisponiblesCompleted);
                servicio.MesasDisponiblesAsync();
            }
            catch
            {
                MessageBox.Show("Error");
            }
        }

        private void servicio_MesasDisponiblesCompleted(object sender, ServiceReference1.MesasDisponiblesCompletedEventArgs e)
        {
            try
            {
                XDocument xmlD = new XDocument(e.Result);
                xmlDocSocios = xmlD;

                using (RestauranteBaseContext ctx = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString))
                {
                    if (!ctx.DatabaseExists())
                        ctx.CreateDatabase();

                    List<BaseLocal.MESA> ListaMesas = (from c in xmlDocSocios.Descendants("Table")
                                                       select new BaseLocal.MESA
                                                       {
                                                           MESA_NUM = (Int16)c.Element("MESA_NUM"),
                                                           MESA_CANTSILLAS = (Int16)c.Element("MESA_CANTSILLAS"),
                                                           MESA_ESTADO = (String)c.Element("MESA_ESTADO"),
                                                           REST_ID = (int)c.Element("REST_ID")
                                                       }).ToList<BaseLocal.MESA>();

                    borrar_mesas();

                    foreach (BaseLocal.MESA mesa in ListaMesas)
                    {
                        ctx.MESA.InsertOnSubmit(mesa);
                    }
                    ctx.SubmitChanges();
                    ctx.Dispose();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        public void borrar_mesas()
        {
            var qry = (from n in db.MESA
                       select n);

            List<MESA> m = qry.ToList();

            if (m != null)
            {
                db.MESA.DeleteAllOnSubmit(m);
            }
            db.SubmitChanges();
        }

        public void RecibeRecetas()
        {
            try
            {

                servicio.RecetasCompleted += new EventHandler<ServiceReference1.RecetasCompletedEventArgs>(servicio_RecetasCompleted);
                servicio.RecetasAsync();

            }
            catch
            {
                MessageBox.Show("Error, servidor no encontrado.");
            }
        }

        private void servicio_RecetasCompleted(object sender, ServiceReference1.RecetasCompletedEventArgs e)
        {
            XDocument xmlD = new XDocument(e.Result);
            xmlDocSocios = xmlD;

            using (RestauranteBaseContext ctx = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString))
            {
                if (!ctx.DatabaseExists())
                    ctx.CreateDatabase();

                List<BaseLocal.PLATO> ListaPlato = (from c in xmlDocSocios.Descendants("Table")
                                                    select new BaseLocal.PLATO
                                                    {
                                                        PLATO_ID = (Int16)c.Element("RECETA_ID"),
                                                        PLATO_NOMBRE = (String)c.Element("RECETA_NOMBRE"),
                                                        PLATO_PRECIO = (double)c.Element("RECETA_PRECIO"),
                                                    }).ToList<BaseLocal.PLATO>();
                borrar_platos();

                foreach (PLATO plato in ListaPlato)
                {
                    int length = plato.PLATO_NOMBRE.Length;
                    if (length > 20) plato.PLATO_NOMBRE = plato.PLATO_NOMBRE.Substring(0, 19);
                }

                foreach (BaseLocal.PLATO plato in ListaPlato)
                {
                    ctx.PLATO.InsertOnSubmit(plato);
                    ctx.SubmitChanges();
                }
                
                ctx.Dispose();

            }
        }

        public void borrar_platos()
        {
            var qry = (from n in db.PLATO
                       select n);

            List<PLATO> m = qry.ToList();

            if (m != null)
            {
                db.PLATO.DeleteAllOnSubmit(m);
            }
            db.SubmitChanges();
        }

        public void CerrarOrden(Int16 mesa)
        {
            servicio.EstadoMesaAsync(mesa, "DISPONIBLE");
            servicio.EstadoMesaCompleted += new EventHandler<ServiceReference1.EstadoMesaCompletedEventArgs>(servicio_EstadoMesaCompleted);

        }

        private void servicio_EstadoMesaCompleted(object sender, ServiceReference1.EstadoMesaCompletedEventArgs e)
        {
            try
            {
                MessageBox.Show("Exito");
            }
            catch
            {
                MessageBox.Show("Error");
            }
        }

        public void IniciarSesión(String usuario, String pass)
        {
            servicio.CheckPassAsync(usuario, pass);
            servicio.CheckPassCompleted += new EventHandler<ServiceReference1.CheckPassCompletedEventArgs>(servicio_CheckPassCompleted);
        }

        private void servicio_CheckPassCompleted(object sender, ServiceReference1.CheckPassCompletedEventArgs e)
        {
            try
            {
                String resultado = e.Result;
                if (resultado != "NO")
                {
                    db.CreateIfNotExists();

                    var qry = (from c in db.EMPLEADO
                               select c);

                    if (qry != null)
                    {
                        borrar_empleados();
                    }
                    EMPLEADO emp = new EMPLEADO();
                    emp.EMP_LOGIN = resultado;

                    db.EMPLEADO.InsertOnSubmit(emp);
                    db.SubmitChanges();

                    MessageBoxResult result = MessageBox.Show(emp.EMP_LOGIN + " ha iniciado sesión.");
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña inválida.","Sesión",MessageBoxButton.OK);
                }
            }
            catch
            {
                MessageBox.Show("Error al iniciar sesión. Revise su conexión WIFI");
            }
        }

        private void borrar_empleados()
        {
            var qry = (from n in db.EMPLEADO
                       select n);

            List<EMPLEADO> m = qry.ToList();

            if (m != null)
            {
                db.EMPLEADO.DeleteAllOnSubmit(m);
            }
            db.SubmitChanges();
        }

    }
}
