using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Management;
using System.Windows.Markup;
using System.Collections.ObjectModel;

namespace wmibrowser
{
    /// <summary>
    /// Interaction logic for ClassWindow.xaml
    /// </summary>
    public partial class ClassWindow : Window
    {
        public ManagementClass ClassInstance;
        public ManagementObject[] Collection;
        public PropertyData[] Properties;
        public MethodData[] Methods;

        public ClassWindow()
        {
            InitializeComponent();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                if (ClassInstance != null)
                {
                    ClassInstance.Dispose();
                }

                for(int i = 0; i < Collection.Length; i++)
                {
                    Collection[i].Dispose();
                }

                Properties = default;
                Methods = default;

                GC.Collect(1);
            });
        }

        public void LoadClass(ManagementClass classInstance)
        {
            Task.Run(delegate
            {
                if (classInstance == null)
                {
                    MessageBox.Show("Instance is null. Probably eaten by garbage collection. Search again and retry.", "How");
                    this.Close();
                    return;
                }

                ClassInstance = classInstance;

                try
                {
                    classInstance.Get();
                } catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Get error");
                    return;
                }

                try
                {
                    UpdateInstances();
                } catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Enum failure");
                    Collection = new ManagementObject[0];
                }

                if (ClassInstance.Methods.Count > 0)
                {
                    Methods = ClassInstance.Methods.OfType<MethodData>().ToArray();
                } else
                {
                    Methods = new MethodData[0];
                }

                if (ClassInstance.Properties.Count > 0)
                {
                    Properties = ClassInstance.Properties.OfType<PropertyData>().ToArray();
                } else
                {
                    Properties = new PropertyData[0];
                }

                this.Dispatcher.Invoke(delegate
                {
                    foreach(ManagementObject instance in Collection)
                    {
                        comboBox.Items.Add(instance.ToString().Split(':')[1]);
                    }

                    for (int i = 0; i < Properties.Length; i++)
                    {
                        dataGrid.Items.Add(new PropDataBind { PropType = BuildDataType(Properties[i]), PropName = Properties[i].Name, PropValue = string.Empty });
                    }

                    if (comboBox.Items.Count > 0)
                    {
                        comboBox.SelectedIndex = 0;
                    }

                    if (Methods.Length == 0)
                    {
                        methodDataGrid.Height = 0;
                        return;
                    }

                    StringBuilder builder = new StringBuilder();
                    ManagementBaseObject methodBase;

                    for (int i = 0; i < Methods.Length; i++)
                    {
                        builder.Append(Methods[i].Name);
                        builder.Append("(");

                        methodBase = Methods[i].InParameters;

                        if (methodBase != null)
                        {
                            int propIndex = 0;
                            foreach(PropertyData data in methodBase.Properties)
                            {
                                if (propIndex != 0)
                                {
                                    builder.Append(", ");
                                }

                                builder.Append(Enum.GetName(typeof(CimType), data.Type));
                                builder.Append(" ");
                                builder.Append(data.Name);
                                propIndex++;
                            }
                        }

                        builder.Append(")");

                        methodDataGrid.Items.Add(new MethodDataBind { MethodName = builder.ToString() });

                        builder.Clear();
                    }
                });
            });
        }

        private void UpdateInstances()
        {
            try
            {
                GetInstances();
            }
            catch
            {
                QueryInstances();
            }
        }

        private void GetInstances()
        {
            using (ManagementObjectCollection collection = ClassInstance.GetInstances())
            {
                Collection = collection.OfType<ManagementObject>().ToArray();
            }
        }

        private void QueryInstances()
        {
            string namespacePath = ClassInstance.ClassPath.NamespacePath;
            string className = ClassInstance.Path.ClassName;

            if (!Config.WhitelistValidate(className))
            {
                MessageBox.Show("Banned characters found in class name", "How");
                throw new Exception("Invalid query");
            }

            StringBuilder queryBuilder = new StringBuilder();
            queryBuilder.Append("SELECT * FROM ");
            queryBuilder.Append(className);

            string query = queryBuilder.ToString();

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(namespacePath, query, Config.Enum))
            {
                using (ManagementObjectCollection collection = searcher.Get())
                {
                    Collection = CollectionToArray(collection);
                }
            }
        }

        private ManagementObject[] CollectionToArray(ManagementObjectCollection collection)
        {
            if (collection.Count > 0)
            {
                return collection.OfType<ManagementObject>().ToArray();
            } else
            {
                return new ManagementObject[0];
            }
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox.SelectedIndex > Collection.Length)
            {
                MessageBox.Show("Selected item index is out of bounds", "How");
                return;
            }

            ManagementObject instance = Collection[comboBox.SelectedIndex];

            dataGrid.Items.Clear();

            for (int i = 0; i < Properties.Length; i++)
            {
                string name = Properties[i].Name;
                PropertyData data = instance.Properties[name];

                if (data == null)
                {
                    continue;
                }

                PropDataBind bind = new PropDataBind { PropType = BuildDataType(data), PropName = data.Name };

                if (data.Value == null)
                {
                    bind.PropValue = string.Empty;
                }
                else if (data.IsArray)
                {
                    bind.PropValue = BuildArray(data);
                }
                else
                {
                    if (data.Value is ManagementBaseObject)
                    {
                        bind.PropValue = ((ManagementBaseObject)data.Value).ClassPath.ClassName;
                    } else
                    {
                        bind.PropValue = data.Value.ToString();
                    }
                }

                dataGrid.Items.Add(bind);
            }

            dataGrid.Items.Refresh();
        }

        private string BuildDataType(PropertyData data)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Enum.GetName(typeof(CimType), data.Type));
            if (data.IsArray)
            {
                builder.Append("[]");
            }
            return builder.ToString();
        }

        private string BuildArray(PropertyData data)
        {
            switch (data.Type)
            {
                case CimType.String:
                    return string.Join(", ", (string[])data.Value);
                case CimType.UInt16:
                    return string.Join(", ", (UInt16[])data.Value);
                case CimType.UInt32:
                    return string.Join(", ", (UInt32[])data.Value);
                case CimType.UInt64:
                    return string.Join(", ", (UInt64[])data.Value);
                default:
                    return data.Value.ToString();
            }
        }
    }
}
