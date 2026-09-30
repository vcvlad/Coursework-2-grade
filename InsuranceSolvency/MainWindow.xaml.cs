using System;
using System.Data;
using System.Windows;

namespace InsuranceApp
{
    public partial class MainWindow : Window
    {
        private SolvencyService service = new SolvencyService();

        public MainWindow()
        {
            InitializeComponent();
            LoadCompanies();
        }

        private void LoadCompanies()
        {
            DataTable companies = service.GetAllCompanies();
            dgCompanies.ItemsSource = companies.DefaultView;

            // Обновляем счетчик компаний
            tbCompaniesCount.Text = companies.Rows.Count.ToString();
            
        }

        // Вспомогательный метод для получения значения из DataRowView
        private T GetValueFromSelectedItem<T>(object selectedItem, string columnName)
        {
            if (selectedItem is DataRowView rowView)
            {
                object value = rowView[columnName];
                if (value != DBNull.Value)
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }
            return default(T);
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadCompanies();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CompanyDialog();
            if (dialog.ShowDialog() == true)
            {
                service.AddCompany(dialog.CompanyName, dialog.CompanyUnp, dialog.CompanyAddress);
                LoadCompanies();
                MessageBox.Show("Компания успешно добавлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgCompanies.SelectedItem == null)
            {
                MessageBox.Show("Выберите компанию для редактирования!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string name = GetValueFromSelectedItem<string>(dgCompanies.SelectedItem, "Name");
            string unp = GetValueFromSelectedItem<string>(dgCompanies.SelectedItem, "Unp");
            string address = GetValueFromSelectedItem<string>(dgCompanies.SelectedItem, "LegalAddress");
            int id = GetValueFromSelectedItem<int>(dgCompanies.SelectedItem, "Id");

            var dialog = new CompanyDialog(name, unp, address);
            if (dialog.ShowDialog() == true)
            {
                service.UpdateCompany(id, dialog.CompanyName, dialog.CompanyUnp, dialog.CompanyAddress);
                LoadCompanies();
                MessageBox.Show("Компания успешно обновлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgCompanies.SelectedItem == null)
            {
                MessageBox.Show("Выберите компанию для удаления!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранную компанию?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    int id = GetValueFromSelectedItem<int>(dgCompanies.SelectedItem, "Id");
                    string name = GetValueFromSelectedItem<string>(dgCompanies.SelectedItem, "Name");

                    service.DeleteCompany(id);
                    LoadCompanies();
                    MessageBox.Show($"Компания \"{name}\" успешно удалена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSolvency_Click(object sender, RoutedEventArgs e)
        {
            var solvencyWindow = new SolvencyWindow(service);
            solvencyWindow.ShowDialog();
        }

        private void BtnContracts_Click(object sender, RoutedEventArgs e)
        {
            if (dgCompanies.SelectedItem == null)
            {
                MessageBox.Show("Выберите компанию для просмотра данных!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int companyId = GetValueFromSelectedItem<int>(dgCompanies.SelectedItem, "Id");
            string companyName = GetValueFromSelectedItem<string>(dgCompanies.SelectedItem, "Name");

            var contractWindow = new ContractWindow(service, companyId, companyName);
            contractWindow.ShowDialog();
        }

        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            var reportWindow = new ReportWindow(service);
            reportWindow.ShowDialog();
        }
    }
}