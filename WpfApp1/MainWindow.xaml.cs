using System;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows.Media;
using System.Linq;
using System.Windows.Input;
using System.Diagnostics;
using System.Security.Principal;

namespace WpfApp1
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            if (!IsAdministrator())
            {
                //intenta reiniciar la aplicacion con privilegios de administrador
                RestartAsAdmin();
                Application.Current.Shutdown();
            }
            InitializeComponent();

            CargarConfiguracion();
            ListaDeProgramas.SelectedItemChanged += ListaDeProgramas_SelectedItemChanged;
        }
        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        private void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule.FileName,
                UseShellExecute = true,
                Verb = "runas" //esto es lo que provoca que se solicite las credenciales de administrador
            };
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("La aplicacion necesita permisos de administrador para ejecutarse." + ex.Message);
            }
        }

        private void AbrirSharePointPage(object sender, RoutedEventArgs e)
        {

        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void CargarConfiguracion()
        {
            try
            {
                // Ruta del archivo XML
                string xmlFilePath = "Config.xml";

                // Cargar el archivo XML
                XDocument xmlDoc = XDocument.Load(xmlFilePath);

                // Limpiar el TreeView antes de cargar la nueva configuración
                ListaDeProgramas.Items.Clear();

                // Obtener todos los grupos principales del XML en el orden en que aparecen
                var gruposPrincipales = xmlDoc.Root.Elements("Grupo").ToList();

                // Recorrer todos los grupos principales y agregarlos al TreeView
                foreach (var grupo in gruposPrincipales)
                {
                    // Crear un TreeViewItem para el grupo principal
                    TreeViewItem treeViewGroup = new TreeViewItem();

                    // Establecer el encabezado del grupo principal
                    treeViewGroup.Header = grupo.Attribute("Nombre").Value;

                    // Agregar el grupo principal al TreeView
                    ListaDeProgramas.Items.Add(treeViewGroup);

                    // Cargar los elementos del grupo principal recursivamente
                    CargarElementosDelGrupo(grupo, treeViewGroup);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la configuración: " + ex.Message);
            }
        }


        private void CargarElementosDelGrupo(XElement grupo, TreeViewItem treeViewGroup)
        {
            // Obtener todos los elementos del grupo actual en el orden en que aparecen en el XML
            var elementos = grupo.Elements().ToList();

            // Recorrer todos los elementos (Item o Grupo) dentro del grupo actual en el orden en que aparecen en el XML
            foreach (var elemento in elementos)
            {
                // Crear un TreeViewItem para el elemento actual
                TreeViewItem treeViewItem = new TreeViewItem();

                // Verificar si el elemento tiene el atributo "Nombre"
                var nombreAttribute = elemento.Attribute("Nombre");
                if (nombreAttribute != null)
                {
                    // Acceder al valor del atributo "Nombre"
                    string nombre = nombreAttribute.Value;
                    treeViewItem.Header = nombre;
                }

                // Si es un elemento <Item>, cambiar el color de la fuente a amarillo
                if (elemento.Name == "Item")
                {
                    treeViewItem.Foreground = Brushes.Yellow;
                }

                // Si es un grupo, cargar sus elementos recursivamente
                if (elemento.Name == "Grupo")
                {
                    // Recursivamente cargar los elementos del grupo actual
                    CargarElementosDelGrupo(elemento, treeViewItem);
                }

                // Agregar el elemento al TreeViewItem del grupo
                treeViewGroup.Items.Add(treeViewItem);
            }
        }

        private void ListaDeProgramas_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            try
            {
                // Ruta del archivo XML
                string xmlFilePath = "Config.xml";
                // Cargar el archivo XML
                XDocument xmlDoc = XDocument.Load(xmlFilePath);
                if (ListaDeProgramas.SelectedItem != null && xmlDoc != null)
                {
                    TreeViewItem selectedItem = (TreeViewItem)ListaDeProgramas.SelectedItem;
                    string selectedContent = selectedItem.Header.ToString();

                    // Mostrar el nombre del nodo en ProgramaBox
                    ProgramaBox.Text = selectedContent;

                    // Buscar el elemento correspondiente en el XML
                    XElement selectedItemXml = xmlDoc.Descendants()
                        .FirstOrDefault(x => x.Attribute("Nombre")?.Value == selectedContent);

                    if (selectedItemXml != null)
                    {
                        string descripcion = selectedItemXml.Element("Descripcion")?.Value;
                        DescripcionBox.Text = descripcion ?? string.Empty;
                        ProgramaBox.Text = selectedContent; // Mostrar el nombre en el TextBox ProgramaBox
                                                            // Habilitar el botón Ejecutar
                        ButtonEjecutar.IsEnabled = true;
                    }
                }
                else
                {
                    ProgramaBox.Text = string.Empty;
                    DescripcionBox.Text = string.Empty;
                    // Deshabilitar el botón Ejecutar
                    ButtonEjecutar.IsEnabled = false;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la configuración: " + ex.Message);
            }
        }
        private void Buscador_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Llamar al método que se ejecuta cuando se hace clic en el botón de búsqueda
                BotonBuscador_Click(sender, e);
            }
        }
        private void BotonBuscador_Click(object sender, RoutedEventArgs e)
        {
            // Obtener el texto ingresado en el TextBox Buscador y convertirlo a minúsculas
            string textoBuscado = Buscador.Text.ToLower();

            // Verificar si el campo de búsqueda está vacío
            if (string.IsNullOrWhiteSpace(textoBuscado))
            {
                // Si el campo de búsqueda está vacío, cerrar todas las ramas del árbol y mostrar todos los elementos
                CerrarTodasRamas(ListaDeProgramas.Items);
                MostrarTodosElementos(ListaDeProgramas.Items);
                return;
            }

            // Recorrer todos los elementos en el árbol y filtrar los que contienen la palabra buscada
            foreach (var item in ListaDeProgramas.Items)
            {
                FiltrarElementos(item as TreeViewItem, textoBuscado);
            }
        }

        private bool FiltrarElementos(TreeViewItem item, string textoBuscado)
        {
            if (item == null)
            {
                return false;
            }

            // Verificar si el nombre del elemento contiene el texto buscado
            bool encontrado = item.Header != null && item.Header.ToString().ToLower().Contains(textoBuscado);

            // Recorrer los elementos secundarios del grupo o del ítem (si los tiene)
            bool encontradoEnHijos = false;
            foreach (var subItem in item.Items)
            {
                encontradoEnHijos = FiltrarElementos(subItem as TreeViewItem, textoBuscado) || encontradoEnHijos;
            }

            // Mostrar u ocultar el elemento según corresponda
            if (encontrado || encontradoEnHijos)
            {
                MostrarElemento(item);
                if (encontradoEnHijos)
                {
                    item.IsExpanded = true;
                }
            }
            else
            {
                OcultarElemento(item);
            }

            return encontrado || encontradoEnHijos;
        }

        private void OcultarElemento(TreeViewItem item)
        {
            // Ocultar el elemento
            item.Visibility = Visibility.Collapsed;

            // Cerrar los nodos hijos
            item.IsExpanded = false;
        }

        private void MostrarElemento(TreeViewItem item)
        {
            // Mostrar el elemento
            item.Visibility = Visibility.Visible;
        }

        private void ExpandirNodosPadres(TreeViewItem item)
        {
            // Expandir los nodos padres del elemento
            TreeViewItem parentItem = item.Parent as TreeViewItem;
            while (parentItem != null)
            {
                parentItem.IsExpanded = true;
                parentItem = parentItem.Parent as TreeViewItem;
            }
        }

        private void CerrarTodasRamas(ItemCollection items)
        {
            // Recorrer todos los elementos en la colección y cerrarlos
            foreach (TreeViewItem item in items)
            {
                item.IsExpanded = false;
                OcultarElemento(item);
            }
        }

        private void MostrarTodosElementos(ItemCollection items)
        {
            foreach (TreeViewItem item in items)
            {
                MostrarElemento(item);
                MostrarTodosElementos(item.Items);
            }
        }

        private void ButtonEjecutar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Ruta del archivo XML
                string xmlFilePath = "Config.xml";
                // Cargar el archivo XML
                XDocument xmlDoc = XDocument.Load(xmlFilePath);

                if (ListaDeProgramas.SelectedItem != null)
                {
                    TreeViewItem selectedItem = (TreeViewItem)ListaDeProgramas.SelectedItem;
                    string selectedContent = selectedItem.Header.ToString();

                    // Buscar el elemento correspondiente en el XML
                    XElement selectedItemXml = xmlDoc.Descendants()
                        .FirstOrDefault(x => x.Attribute("Nombre")?.Value == selectedContent);

                    if (selectedItemXml != null)
                    {

                        // Verificar si es un comando
                        var comandoElement = selectedItemXml.Element("Comando");
                        if (comandoElement != null)
                        {
                            string comando = comandoElement.Value;
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = comando,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al ejecutar la acción: " + ex.Message);
            }
        }

    }
}
