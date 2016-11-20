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
using System.Windows.Media;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;                               //Contexto de la base ENTERA
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;                     //Para convertir xml
using System.Collections.ObjectModel;               //Para convertir xml
using System.ComponentModel;                        //Para convertir xml

namespace AppCala.Ordenes
{
    public partial class EntregaOrden : PhoneApplicationPage
    {
        RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString);
        ServiceReference1.ServiceSoapClient servicio = new ServiceReference1.ServiceSoapClient();
        int nummesa;
        Sincronizacion sinc = new Sincronizacion();
        public EntregaOrden()
        {
            InitializeComponent();

            sinc.RecibeMesas();
            sinc.RecibeRecetas();
            
        }

        private void btMesas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                popupMesas.IsOpen = true;

                var qry = (from c in db.MESA
                           where c.MESA_ESTADO == "OCUPADO"
                           select c.MESA_NUM + " - " + c.MESA_ESTADO).ToList();

                lbMesas.ItemsSource = qry;

            }
            catch
            {
                MessageBox.Show("Error. Revise conexión e intente nuevamente.");
            }
        }

        private void ListBox_GotFocus(object sender, RoutedEventArgs e)
        {

            (sender as ListBox).Background = new SolidColorBrush(Colors.Black);
            (sender as ListBox).Foreground = new SolidColorBrush(Colors.White);
        }

        private void ListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as ListBox).Background = new SolidColorBrush(Colors.Transparent);
            (sender as ListBox).Foreground = new SolidColorBrush(Colors.White);
        }

        private void Button_Tap_3(object sender, System.Windows.Input.GestureEventArgs e)
        {
            popupMesas.IsOpen = false;
        }

        private void btAceptarMesa_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (lbMesas.SelectedItem.ToString() != "")
                {
                    tbMesaNro.Text = "platos de la mesa n°: ";
                    nummesa = Int16.Parse(lbMesas.SelectedItem.ToString().Substring(0, lbMesas.SelectedItem.ToString().Trim().IndexOf("-")));
                    tbMesaNro.Text = tbMesaNro.Text + nummesa;

                    RecibePlatos(nummesa);

                    popupMesas.IsOpen = false;
                }
                else
                {
                    MessageBox.Show("Seleccione una mesa.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        public void RecibePlatos(int orden)
        {
            servicio.RecibePlatosOrdenAsync(orden);
            servicio.RecibePlatosOrdenCompleted += new EventHandler<ServiceReference1.RecibePlatosOrdenCompletedEventArgs>(servicio_RecibePlatosOrdenCompleted);
        }

        private void servicio_RecibePlatosOrdenCompleted(object sender, ServiceReference1.RecibePlatosOrdenCompletedEventArgs e)
        {
            XDocument xdoc = new XDocument(e.Result);

            List<BaseLocal.PLATOSXORDEN> ListaMesas = (from c in xdoc.Descendants("Table")
                                                select new BaseLocal.PLATOSXORDEN
                                                {
                                                    PXO_ID = (int)c.Element("RXO_ID"),
                                                    ORD_NUM = (int)c.Element("ORD_NUM"),
                                                    PXO_CANTIDAD = (int)c.Element("RXO_CANTIDAD"),
                                                    PLATO_ID = (int)c.Element("RECETA_ID")
                                                }).ToList<BaseLocal.PLATOSXORDEN>();

            borrar_pxo();

            foreach (var item in ListaMesas)
            {
                db.PLATOSXORDEN.InsertOnSubmit(item);
                db.SubmitChanges();
            }
            

            var qry = (from c in db.PLATOSXORDEN
                       join d in db.PLATO on c.PLATO_ID equals d.PLATO_ID
                       select c.PLATO_ID + "-" + d.PLATO_NOMBRE + "-" + c.PXO_CANTIDAD).ToList();

            lbx.ItemsSource = qry;

        }

        public void borrar_pxo()
        {
            var qry = (from n in db.PLATOSXORDEN
                       select n);

            List<PLATOSXORDEN> m = qry.ToList();

            if (m != null)
            {
                db.PLATOSXORDEN.DeleteAllOnSubmit(m);
            }
            db.SubmitChanges();
        }

        private void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (nummesa != 0)
            {
                servicio.EntregadoAsync(nummesa);
                servicio.EntregadoCompleted += new EventHandler<ServiceReference1.EntregadoCompletedEventArgs>(servicio_EntregadoCompleted);
            }
        }

        private void servicio_EntregadoCompleted(object sender, ServiceReference1.EntregadoCompletedEventArgs e)
        {
            try
            {
                if (e.Result == "OK")
                {   
                    //Borrar lbx
                    tbMesaNro.Text = "platos de la mesa";
                    MessageBox.Show("Platos de mesa " + nummesa + " entregados");
                    NavigationService.Navigate(new Uri("/Principal.xaml", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    MessageBox.Show("Error, revise conectividad.");
                }
            }
            catch
            {
                MessageBox.Show("Error, revise conectividad.");
            }
        }

    }
}