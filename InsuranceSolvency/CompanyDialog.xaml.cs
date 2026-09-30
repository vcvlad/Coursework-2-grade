using System.Windows;

namespace InsuranceApp
{
    public partial class CompanyDialog : Window
    {
        public string CompanyName { get; set; }
        public string CompanyUnp { get; set; }
        public string CompanyAddress { get; set; }

        public CompanyDialog(string name = "", string unp = "", string address = "")
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;

            // Заполняем поля, если это редактирование
            if (!string.IsNullOrEmpty(name))
                txtName.Text = name;

            if (!string.IsNullOrEmpty(unp))
                txtUnp.Text = unp;

            if (!string.IsNullOrEmpty(address))
                txtAddress.Text = address;

            // Устанавливаем фокус на первое поле
            Loaded += (s, e) => txtName.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения обязательных полей
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название компании!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUnp.Text))
            {
                MessageBox.Show("Введите УНП компании!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUnp.Focus();
                return;
            }

            // Сохраняем данные
            CompanyName = txtName.Text.Trim();
            CompanyUnp = txtUnp.Text.Trim();
            CompanyAddress = txtAddress.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}