using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaseLocal;
using System.Data;
using System.Data.Linq;
using System.Data.Linq.Mapping;                               //Contexto de la base ENTERA
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;                     //Para convertir xml
using System.Collections.ObjectModel;               //Para convertir xml
using System.ComponentModel;                        //Para convertir xml
using System.Threading.Tasks;
using System.Windows.Media;

namespace AppCala.Ordenes
{
    public partial class NuevaOrden : PhoneApplicationPage
    {

        Sincronizacion sinc = new Sincronizacion();
        ServiceReference1.ServiceSoapClient servicio = new ServiceReference1.ServiceSoapClient();
        RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString);
        Int16 nummesa;
        EMPLEADO emp;
        public NuevaOrden()
        {

            InitializeComponent();
            nummesa = 0;
            tbMesaNro.Text = "mesa n° ";
            lbx.Items.Clear();

            sinc.RecibeMesas();
            sinc.RecibeRecetas();

            emp = db.EMPLEADO.FirstOrDefault();
        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            popupRecetas.IsOpen = false;
            tbCant.Text = "0";
        }

        private void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                popupMesas.IsOpen = true;

                var qry = (from c in db.MESA
                           select c.MESA_NUM + " - " + c.MESA_ESTADO).ToList();

                lbMesas.ItemsSource = qry;

            }
            catch
            {
                MessageBox.Show("Error. Revise conexión e intente nuevamente.");
            }
        }

        private void Button_Tap_2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            tbCant.Text = "1";

            var qry = (from p in db.PLATO
                       select p.PLATO_ID + " - " + p.PLATO_NOMBRE).ToList();

            lbRecetas.ItemsSource = qry;

            popupRecetas.IsOpen = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lbRecetas.SelectedItem.ToString() != "")
                {
                    //insertar en lbx
                    lbx.Items.Add(lbRecetas.SelectedItem.ToString() + " - " + tbCant.Text);
                    popupRecetas.IsOpen = false;
                }
                else
                {
                    MessageBox.Show("seleccione un plato");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }

        }

        private void btAceptarMesa_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (lbMesas.SelectedItem.ToString() != "")
                {
                    tbMesaNro.Text = "mesa n° ";
                    nummesa = Int16.Parse(lbMesas.SelectedItem.ToString().Substring(0,lbMesas.SelectedItem.ToString().Trim().IndexOf("-")));
                    tbMesaNro.Text = tbMesaNro.Text + nummesa;

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

        private void imgMas_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

            int cant = Int16.Parse(tbCant.Text);
            if (cant >= 1)
            {
                cant++;
                tbCant.Text = cant.ToString();
            }
        }

        private void imgMenos_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int cant = Int16.Parse(tbCant.Text);
            if (cant > 1)
            {
                cant--;
                tbCant.Text = cant.ToString();
            }
        }

        private void Button_Tap_3(object sender, System.Windows.Input.GestureEventArgs e)
        {
            popupMesas.IsOpen = false;
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
                lbx.Items.RemoveAt(lbx.SelectedIndex);
            }
        }

        public void NuevaOr(String mesa, String usuario)
        {
            var qry = (from n in db.PLATOSXORDEN
                       select n).ToList();

            List<PLATOSXORDEN> lista = qry;

            //////xml del detalle de orden
            XDocument xml = new XDocument();


            xml = new XDocument(new XElement("Detalle",
               from vd in db.PLATOSXORDEN
               //where vd.PXO_ID == fila.PXO_ID
               select new XElement("PLATOSXORDEN",
                   new XElement("PLATO_ID", vd.PLATO_ID),
                   new XElement("ORD_NUM", vd.ORD_NUM),
                   new XElement("PXO_CANTIDAD", vd.PXO_CANTIDAD),
                   new XElement("PLATO_FECHA", vd.PLATO_FECHA),
                   new XElement("PLATO_HORA", vd.PLATO_HORA)
                   )
                ));


            servicio.NuevaOrdenAsync(mesa, usuario, xml.ToString());
            servicio.NuevaOrdenCompleted += new EventHandler<ServiceReference1.NuevaOrdenCompletedEventArgs>(servicio_NuevaOrdenCompleted);

        }

        private void servicio_NuevaOrdenCompleted(object sender, ServiceReference1.NuevaOrdenCompletedEventArgs e)
        {
            try
            {
                String resultado = e.Result;
                if (resultado == "OK")
                {
                    var qry = (from n in db.PLATOSXORDEN
                               select n);

                    List<PLATOSXORDEN> m = qry.ToList();

                    foreach (PLATOSXORDEN pxo in m)
                    {
                        db.PLATOSXORDEN.DeleteOnSubmit(pxo);
                        db.SubmitChanges();
                    }

                    var qry1 = (from n in db.ORDEN
                                select n);

                    List<ORDEN> m1 = qry1.ToList();

                    foreach(ORDEN ord in m1)
                    {
                        db.ORDEN.DeleteOnSubmit(ord);
                        db.SubmitChanges();
                    }

                    MessageBox.Show("¡Registrado!");
                    NavigationService.Navigate(new Uri("/Principal.xaml", UriKind.RelativeOrAbsolute));
                }
                else
                {
                    MessageBox.Show("¡Error!");
                }


            }
            catch
            {
                MessageBox.Show("Error, no registrado.");
            }

        }

        protected void borrar_pxo()
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

        private void tbBuscar_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            tbBuscar.Text = "";
        }

        private void tbObs_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            tbObs.Text = "";
        }

        private void lbRecetas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            tbCant.Text = "1";
        }

        private void btOrdenar_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Está a punto de ordenar. Desea continuar?", "Atención", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {

                if (nummesa > 0)
                {
                    if (lbx.Items.Count > 0)
                    {
                        borrar_pxo();

                        foreach (var item in lbx.Items)
                        {
                            string cadena = item.ToString();
                            string platoid = cadena.Substring(0, cadena.IndexOf("-")).Trim();
                            cadena = cadena.Substring(cadena.IndexOf("-")+1).Trim();
                            cadena = cadena.Substring(cadena.IndexOf("-") + 1).Trim();
                            string cantidad = cadena;

                            var pxoult = (from c in db.PLATOSXORDEN
                                          orderby c.PXO_ID descending
                                          select c).FirstOrDefault();

                            if (pxoult != null)
                            {
                                PLATOSXORDEN pxo = new PLATOSXORDEN();
                                pxo.PXO_ID = pxoult.PXO_ID + 1;
                                pxo.PLATO_FECHA = DateTime.Now;
                                pxo.PLATO_HORA = DateTime.Now.TimeOfDay.ToString();
                                pxo.PLATO_ID = int.Parse(platoid);
                                pxo.ORD_NUM = pxoult.ORD_NUM + 1;
                                pxo.PXO_CANTIDAD = int.Parse(cadena);
                                pxo.PXO_ESTADO = "PENDIENTE";

                                db.PLATOSXORDEN.InsertOnSubmit(pxo);
                                db.SubmitChanges();
                            }
                            else
                            {
                                PLATOSXORDEN pxo = new PLATOSXORDEN();
                                pxo.PXO_ID = 1;
                                pxo.PLATO_FECHA = DateTime.Now;
                                pxo.PLATO_HORA = DateTime.Now.TimeOfDay.ToString();
                                pxo.PLATO_ID = int.Parse(platoid);
                                pxo.ORD_NUM = 1;
                                pxo.PXO_CANTIDAD = int.Parse(cadena);
                                pxo.PXO_ESTADO = "PENDIENTE";

                                db.PLATOSXORDEN.InsertOnSubmit(pxo);
                                db.SubmitChanges();
                            }
                        }

                        NuevaOr(nummesa.ToString(), emp.EMP_LOGIN);
                    }
                    else
                    {
                        MessageBox.Show("Seleccione platos");
                    }
                }
                else
                {
                    MessageBox.Show("Seleccione mesa");
                }
            }
        }

        private void tbBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbBuscar.Text != "")
            {
                var qry = (from p in db.PLATO
                           where p.PLATO_NOMBRE.Contains(tbBuscar.Text)
                           select p.PLATO_ID + " - " + p.PLATO_NOMBRE).ToList();

                lbRecetas.ItemsSource = qry;

            }
            else
            {
                var qry = (from p in db.PLATO
                           select p.PLATO_ID + " - " + p.PLATO_NOMBRE).ToList();

                lbRecetas.ItemsSource = qry;
            }
        }

    }
}