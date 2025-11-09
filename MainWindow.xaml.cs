using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; 
namespace restaurant_billing_app
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        //  Observable Collections 
        public ObservableCollection<OrderItem> Orders { get; set; }

        private double _subtotal;
        /// Total price of all items before tax.
        public double Subtotal
        {
            get { return _subtotal; }
            set { _subtotal = value; OnPropertyChanged("Subtotal"); }
        }

        private double _tax;
        /// Calculated tax amount (13% assumed).
        public double Tax
        {
            get { return _tax; }
            set { _tax = value; OnPropertyChanged("Tax"); }
        }

        private double _total;
        /// Final bill amount (Subtotal + Tax).
        public double Total
        {
            get { return _total; }
            set { _total = value; OnPropertyChanged("Total"); }
        }

        // Constructor and Initialization 
        public MainWindow()
        {
            InitializeComponent();

            Orders = new ObservableCollection<OrderItem>();
            DataContext = this;

            dgOrder.ItemsSource = Orders;

            LoadMenuItems();
        }

        private void LoadMenuItems()
        {
            cmbBeverage.ItemsSource = new[]
            {
                new MenuItem { Category="Beverage", Name="Soda", Price=1.75 },
                new MenuItem { Category="Beverage", Name="Coffee", Price=2.50 },
                new MenuItem { Category="Beverage", Name="Tea", Price=2.00 },
                new MenuItem { Category="Beverage", Name="Juice", Price=3.00 },
                new MenuItem { Category="Beverage", Name="Mango Lassi", Price=5.00 }
            };

            cmbAppetizer.ItemsSource = new[]
            {
                new MenuItem { Category="Appetizer", Name="Veg Kofta", Price=9.00 },
                new MenuItem { Category="Appetizer", Name="Veg Mix Curry", Price=11.75 },
                new MenuItem { Category="Appetizer", Name="Paneer Curry", Price=12.75 },
                new MenuItem { Category="Appetizer", Name="Paneer 65", Price=11.00 },
                new MenuItem { Category="Appetizer", Name="Chicken Korma", Price=10.50 },
                new MenuItem { Category="Appetizer", Name="Chicken Curry", Price=11.75 },
                new MenuItem { Category="Appetizer", Name="Chicken 65", Price=13.75 },
                new MenuItem { Category="Appetizer", Name="Kadai Chicken", Price=14.75 },
                new MenuItem { Category="Appetizer", Name="Samosa Chat", Price=8.75 },
            };

            cmbMainCourse.ItemsSource = new[]
            {
                new MenuItem { Category="Main Course", Name="Veg Biryani", Price=18.50 },
                new MenuItem { Category="Main Course", Name="Chicken Dum Biryani", Price=14.25 },
                new MenuItem { Category="Main Course", Name="Butter Chicken and Rice", Price=16.00 },
                new MenuItem { Category="Main Course", Name="Daal Makhani and Rice", Price=12.00 },
                new MenuItem { Category="Main Course", Name="Paneer Curry and Butter Naan", Price=12.00 },
                new MenuItem { Category="Main Course", Name="Makhani and Butter", Price=12.00 }
            };

            cmbDessert.ItemsSource = new[]
            {
                new MenuItem { Category="Dessert", Name="Ice Cream", Price=4.00 },
                new MenuItem { Category="Dessert", Name="Mango Rasmalai", Price=5.50 },
                new MenuItem { Category="Dessert", Name="Gulab Jamun", Price=4.75 }
            };
        }

        //  Event Handlers 
        /// Fired when an item is selected from any menu ComboBox.
        private void OnMenuItemSelected(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            MenuItem menuItem = combo.SelectedItem as MenuItem;

            if (menuItem != null)
            {
                AddOrUpdateOrderItem(menuItem);
                // Reset selection so the user can select the same item again
                combo.SelectedIndex = -1;
            }
        }

        /// This is crucial for DataGrid manual edits.
        private void OrderItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Only update the main bill totals if the item's total (Quantity * Price) has changed.
            if (e.PropertyName == "Total")
            {
                UpdateTotals();
            }
        }

        private void AddOrUpdateOrderItem(MenuItem menuItem)
        {
            // Check if the item already exists in the order
            OrderItem existing = Orders.FirstOrDefault(o => o.Name == menuItem.Name);

            if (existing != null)
            {
                // If exists, just increment quantity
                existing.Quantity += 1;
            }
            else
            {
                // If new, create new item and subscribe to its PropertyChanged events
                OrderItem newItem = new OrderItem
                {
                    Name = menuItem.Name,
                    Price = menuItem.Price,
                    Quantity = 1
                };
                newItem.PropertyChanged += OrderItem_PropertyChanged; // Subscribe for DataGrid edits
                Orders.Add(newItem);
            }

            // Always update totals after adding/updating via ComboBox
            UpdateTotals();
        }
        
        //remove items
        private void OnRemoveItemClick(object sender, RoutedEventArgs e)
        {
            OrderItem selected = dgOrder.SelectedItem as OrderItem;

            if (selected != null)
            {
                selected.PropertyChanged -= OrderItem_PropertyChanged;
                Orders.Remove(selected);
                UpdateTotals();
            }
            else
            {
                MessageBox.Show("Please select an item to remove from the order list.",
                    "Remove Item", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// Handles the click event for the 'Clear Bill' button.
        private void OnClearBillClick(object sender, RoutedEventArgs e)
        {
            foreach (var item in Orders)
            {
                item.PropertyChanged -= OrderItem_PropertyChanged;
            }

            Orders.Clear();
            Subtotal = 0;
            Tax = 0;
            Total = 0;
        }

        /// Calculates and updates Subtotal, Tax, and Total based on current orders.
        private void UpdateTotals()
        {
            Subtotal = Orders.Sum(o => o.Total);
            // Assuming a 13% tax rate and rounding to 2 decimal places
            Tax = Math.Round(Subtotal * 0.13, 2);
            Total = Subtotal + Tax;
        }

        // INotifyPropertyChanged Implementation 
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    // Supporting Data Models ---

    /// Represents a single static menu item.
    public class MenuItem
    {
        public string Category { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }

    /// Represents an item currently on the bill, including quantity.
    public class OrderItem : INotifyPropertyChanged
    {
        private int _quantity;

        public string Name { get; set; }
        public double Price { get; set; }

        /// Property used for two-way DataGrid binding.
        /// Raises PropertyChanged for itself and Total when set.
        public int Quantity
        {
            get { return _quantity; }
            set
            {
                // Prevent quantity from dropping below 1
                if (value < 1) value = 1;

                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged("Quantity");
                    OnPropertyChanged("Total");
                }
            }
        }

        /// Calculated property (Price * Quantity).
        public double Total
        {
            get { return Price * Quantity; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}