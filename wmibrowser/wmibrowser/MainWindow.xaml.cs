using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Management;
using System.Management.Instrumentation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace wmibrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ManagementScope Scope = new ManagementScope()
        {
            Options = Config.Connect
        };

        private List<ManagementObject> NInstances;
        private List<NTreeViewItem> NItems;

        private List<ManagementObject> CInstances;
        private List<CTreeViewItem> CItems;

        private static Predicate<object> FilterAction = (item) =>
        {
            TreeViewItem viewItem = (TreeViewItem)item;
            string content = ((string)viewItem.Header).ToLower();

            if (content.Contains(FilterString))
            {
                return true;
            }
            else
            {
                return false;
            }
        };

        private static string FilterString = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            searchButton.IsEnabled = false;

            try
            {
                StringBuilder pathBuilder = new StringBuilder(pathTextbox.Text);
                int pathLength = pathBuilder.Length - 1;
                if (pathBuilder[pathLength] == '\\')
                {
                    pathBuilder.Remove(pathLength, 1);
                }
                Scope.Path.Path = pathBuilder.ToString();
                Scope.Connect();
            } catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Scope failure");
            }

            if (Scope.IsConnected)
            {
                try
                {
                    UpdateItems(Scope, Document.Items);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Task failure");
                }
            }

            searchButton.IsEnabled = true;
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            Document.Items.Clear();
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            string search = searchTextBox.Text.ToLower();

            if (string.IsNullOrEmpty(search))
            {
                FilterString = string.Empty;
                Document.Items.Filter = null;
            }
            else
            {
                FilterString = search;
                Document.Items.Filter = FilterAction;
            }
        }

        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Document.SelectedItem is NTreeViewItem)
            {
                NTreeViewItem node = (NTreeViewItem)Document.SelectedItem;
                UpdateItems(node.Instance, node.Items);
            }
            else if (Document.SelectedItem is CTreeViewItem)
            {
                CTreeViewItem node = (CTreeViewItem)Document.SelectedItem;
                ClassWindow window = new ClassWindow();
                window.Show();
                window.LoadClass(node.Instance);
            }
        }

        private void UpdateItems(ManagementScope scope, ItemCollection container)
        {
            if (container.Count > 0)
            {
                container.Clear();
            }

            Task.Run(delegate
            {
                Task.WaitAll(new Task[]
                {
                    Task.Run(delegate
                    {
                        NInstances = GetInstances(Queries.Namespaces);
                    }),
                    Task.Run(delegate
                    {
                        CInstances = GetInstances(Queries.Classes);
                    })
                });

                this.Dispatcher.Invoke(delegate
                {
                    NItems = CreateNItems();
                    CItems = CreateCItems();

                    for (int i = 0; i < NItems.Count; i++)
                    {
                        container.Add(NItems[i]);
                    }
                    for (int i = 0; i < CItems.Count; i++)
                    {
                        container.Add(CItems[i]);
                    }
                });
            });
        }

        private ManagementScope CreateScope(ManagementScope scope, ManagementObject instance)
        {
            StringBuilder builder = new StringBuilder(scope.Path.Path);
            builder.Append("\\");
            builder.Append(instance.Properties["Name"].Value);
            return new ManagementScope(builder.ToString(), Config.Connect);
        }

        private List<NTreeViewItem> CreateNItems()
        {
            List<NTreeViewItem> items = NInstances.Select(instance => new NTreeViewItem()
            {
                Header = (string)instance.Properties["Name"].Value,
                Instance = CreateScope(Scope, instance),
                Foreground = Brushes.Wheat
            }).ToList();
            return items;
        }

        private List<CTreeViewItem> CreateCItems()
        {
            List<CTreeViewItem> items = CInstances.Select(instance => new CTreeViewItem()
            {
                Header = instance.ToString().Split(':')[1],
                Instance = new ManagementClass(instance.ToString()),
                Foreground = Brushes.Gainsboro
            }).ToList();
            return items;
        }

        private List<ManagementObject> GetInstances(ObjectQuery query)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(Scope, query, Config.Enum);
            ManagementObjectCollection collection;

            try
            {
                collection = searcher.Get();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Enum failure");
                searcher.Dispose();
                return new List<ManagementObject>();
            }

            if (collection.Count < 0)
            {
                return new List<ManagementObject>();
            }

            return collection.OfType<ManagementObject>().OrderBy(x => x.ToString()).ToList();
        }

        public class NTreeViewItem : TreeViewItem
        {
            public ManagementScope Instance;
        }

        public class CTreeViewItem : TreeViewItem
        {
            public ManagementClass Instance;
        }
    }
}
