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
    public partial class ModificarOrden : PhoneApplicationPage
    {
        Sincronizacion sinc = new Sincronizacion();
        ServiceReference1.ServiceSoapClient servicio = new ServiceReference1.ServiceSoapClient();
        RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString);
        Int16 nummesa;
        EMPLEADO emp;
        public ModificarOrden()
        {
            InitializeComponent();

            nummesa = 0;
            tbMesaNro.Text = "mesa n° ";
            lbx.Items.Clear();

            sinc.RecibeMesas();
            sinc.RecibeRecetas();

            emp = db.EMPLEADO.FirstOrDefault();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Background = new SolidColorBrush(Colors.Black);
        }

        private void ListBox_GotFocus(object sender, RoutedEventArgs e)
        {

            (sender as ListBox).Background = new SolidColorBrush(Colors.Black);
        }

        private void ListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as ListBox).Background = new SolidColorBrush(Colors.Transparent);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox).Background = new SolidColorBrush(Colors.Transparent);
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
                    nummesa = Int16.Parse(lbMesas.SelectedItem.ToString());
                    tbMesaNro.Text = tbMesaNro.Text + nummesa;

                    RecibePlatos(nummesa);

                    popupMesas.IsOpen = false;

                    popupEspere.IsOpen = true;
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
            try
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
                }
                db.SubmitChanges();

                var qry = (from c in db.PLATOSXORDEN
                           join d in db.PLATO on c.PLATO_ID equals d.PLATO_ID
                           select c.PLATO_ID + " - " + d.PLATO_NOMBRE + " - " + c.PXO_CANTIDAD).ToList();


                //inserta en lbx
                foreach (var item in qry)
                {
                    lbx.Items.Add(item);
                }

                //lbx.ItemsSource = qry;

                popupEspere.IsOpen = false;
            }
            catch
            {
                popupEspere.IsOpen = false;
            }

        }

        public void borrar_pxo()
        {
            var qry = (from n in db.PLATOSXORDEN
                       select n);

            List<PLATOSXORDEN> m = qry.ToList();

            foreach (PLATOSXORDEN pxo in m)
            {
                db.PLATOSXORDEN.DeleteOnSubmit(pxo);
                db.SubmitChanges();
            }
        }

        private void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                popupMesas.IsOpen = true;

                var qry = (from c in db.MESA
                           where c.MESA_ESTADO == "OCUPADO"
                           select c.MESA_NUM).ToList();

                lbMesas.ItemsSource = qry;

            }
            catch
            {
                MessageBox.Show("Error. Revise conexión e intente nuevamente.");
            }
        }

        private void lbx_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbx.SelectedIndex != -1)
            {
                btEliminar.IsEnabled = true;
            }
            else
            {
                btEliminar.IsEnabled = false;
            }
        }

        private void btEliminar_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (lbx.SelectedIndex != -1)
            {
                string cadena = lbx.SelectedItem.ToString();
                int platoid = int.Parse(cadena.Substring(0, cadena.IndexOf("-")).Trim());
                cadena = cadena.Substring(cadena.IndexOf("-") + 1).Trim();
                cadena = cadena.Substring(cadena.IndexOf("-") + 1).Trim();
                int cant = int.Parse(cadena);

                PLATOSXORDEN pxo = db.PLATOSXORDEN.FirstOrDefault(r => r.PLATO_ID == platoid && r.PXO_CANTIDAD == cant);

                if (pxo != null)
                {
                    Elimina(platoid, cant, nummesa);
                    popupEspere.IsOpen = true;
                }
                else
                {

                    lbx.Items.RemoveAt(lbx.SelectedIndex);

                }
            }
        }

        private void Elimina(int platoid, int cant, int mesa)
        {
            servicio.EliminaDeOrdenAsync(platoid, cant, mesa);
            servicio.EliminaDeOrdenCompleted += new EventHandler<ServiceReference1.EliminaDeOrdenCompletedEventArgs>(servicio_EliminaDeOrdenCompleted);
        }

        private void servicio_EliminaDeOrdenCompleted(object sender, ServiceReference1.EliminaDeOrdenCompletedEventArgs e)
        {
            if (e.Result == "OK")
            {
                if (lbx.SelectedIndex != -1)
                {
                    lbx.Items.RemoveAt(lbx.SelectedIndex);
                }
                popupEspere.IsOpen = false;
                MessageBox.Show("Receta cancelada!","Éxito",MessageBoxButton.OK);
            }
            else
            {
                popupEspere.IsOpen = false;
                MessageBox.Show("Error");
            }
        }

    }
}