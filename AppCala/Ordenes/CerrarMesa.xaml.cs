using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaseLocal;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;                     //Para convertir xml
using System.Collections.ObjectModel;               //Para convertir xml
using System.ComponentModel;
using System.Windows.Media;                        //Para convertir xml

namespace AppCala.Ordenes
{
    public partial class CerrarMesa : PhoneApplicationPage
    {
        ServiceReference1.ServiceSoapClient servicio = new ServiceReference1.ServiceSoapClient();
        RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString);
        static String mesanum = "mesa número: ";
        XDocument xmlDocSocios = new XDocument();
        public CerrarMesa()
        {
            InitializeComponent();

            RecibeMesasEntregadas();

        }

        public void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RecibeMesasEntregadas();
        }

        private void btCerrar_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lbMesas.SelectedIndex != -1)
            {
                Int16 me = Int16.Parse(lbMesas.SelectedItem.ToString().Substring(0, lbMesas.SelectedItem.ToString().IndexOf("-")).Trim());
                MessageBoxResult result = MessageBox.Show("¿Desea cerrar la mesa " + me + "?", "Cierre", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    CerrarOrden(me);
                    //NavigationService.Navigate(new Uri("/Principal.xaml", UriKind.RelativeOrAbsolute));
                }
            }
            else
            {
                MessageBox.Show("Seleccione una mesa para cerrar.");
            }
        }

        private void lbMesas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbMesas.SelectedIndex != -1)
            {
                tbNumMesa.Text = mesanum + lbMesas.SelectedItem.ToString();
                tbNumMesa.Visibility = System.Windows.Visibility.Visible;
            }

        }

        /// <summary>
        /// Fondo ListBox
        /// </summary>

        private void ListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as ListBox).Background = new SolidColorBrush(Colors.Black);
        }

        private void ListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as ListBox).Background = new SolidColorBrush(Colors.Transparent);
        }


        /// <summary>
        /// Sincronización//////
        /// </summary>
        public void CerrarOrden(Int16 mesa)
        {
            servicio.EstadoMesaAsync(mesa, "DISPONIBLE");
            servicio.EstadoMesaCompleted += new EventHandler<ServiceReference1.EstadoMesaCompletedEventArgs>(servicio_EstadoMesaCompleted);

        }

        private void servicio_EstadoMesaCompleted(object sender, ServiceReference1.EstadoMesaCompletedEventArgs e)
        {
            try
            {
                double precio = e.Result;
                if (precio != 0)
                {
                    MessageBox.Show("El precio total es: $" + precio.ToString());
                    RecibeMesasEntregadas();
                    tbNumMesa.Text = mesanum;
                    tbNumMesa.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show("Atención", "No se encontró una orden con el número de mesa seleccionado", MessageBoxButton.OK);
                }

            }
            catch
            {
                MessageBox.Show("Error", "Error en la conexión con el servidor", MessageBoxButton.OK);
            }
        }

        public void RecibeMesasEntregadas()
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
                var qry = (from c in db.MESA
                           where c.MESA_ESTADO == "OCUPADO"
                           select c.MESA_NUM + " - " + c.MESA_ESTADO).ToList();

                lbMesas.ItemsSource = qry;
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


    }
}