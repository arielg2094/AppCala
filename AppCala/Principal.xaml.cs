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

namespace AppCala
{
    public partial class Principal : PhoneApplicationPage
    {
        ServiceReference1.ServiceSoapClient servicio = new ServiceReference1.ServiceSoapClient();
        Sincronizacion sinc = new Sincronizacion();
        RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString);
        public Principal()
        {
            InitializeComponent();

            ValidaUsuario();

        }

        private void Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ValidaUsuario())
            {
                NavigationService.Navigate(new Uri("/Ordenes/NuevaOrden.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                MessageBox.Show("Inicie sesión para continuar", "Atención", MessageBoxButton.OK);
            }
        }

        private void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ValidaUsuario())
            {
                NavigationService.Navigate(new Uri("/Ordenes/ModificarOrden.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                MessageBox.Show("Inicie sesión para continuar", "Atención", MessageBoxButton.OK);
            }
        }

        private void Button_Tap_2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ValidaUsuario())
            {
                NavigationService.Navigate(new Uri("/Ordenes/CerrarMesa.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                MessageBox.Show("Inicie sesión para continuar", "Atención", MessageBoxButton.OK);
            }
        }

        private void Button_Tap_3(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (ValidaUsuario())
            {
                NavigationService.Navigate(new Uri("/Ordenes/EntregaOrden.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                MessageBox.Show("Inicie sesión para continuar", "Atención", MessageBoxButton.OK);
            }
        }

        private void Button_Tap_4(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if ( tbUsuario.Text != "" && tbPassword.Password != "")
            {
                this.IniciarSesión(tbUsuario.Text, tbPassword.Password);
                 

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

                    tbMozo.Text = "mozo: " + emp.EMP_LOGIN;

                    MessageBox.Show(emp.EMP_LOGIN + " ha iniciado sesión.", "App Cala", MessageBoxButton.OK);
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrecta.");
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

        public bool ValidaUsuario()
        {
            try
            {
                using (RestauranteBaseContext db = new RestauranteBaseContext(RestauranteBaseContext.ConnectionString))
                {
                    if (!db.DatabaseExists())
                        db.CreateDatabase();
                    EMPLEADO emp = db.EMPLEADO.FirstOrDefault();
                    if (emp != null)
                    {

                        tbMozo.Text = "mozo: " + emp.EMP_LOGIN;
                        return true;
                    }

                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

    }


}