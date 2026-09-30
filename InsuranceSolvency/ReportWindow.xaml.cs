using System;
using System.Data;
using System.Windows;
using Microsoft.Win32;

namespace InsuranceApp
{
    public partial class ReportWindow : Window
    {
        private SolvencyService _service;
        private DataTable _companies;

        public ReportWindow(SolvencyService service)
        {
            InitializeComponent();
            _service = service;
            LoadCompanies();
        }

        private void LoadCompanies()
        {
            _companies = _service.GetAllCompanies();
            cmbCompanies.ItemsSource = _companies.DefaultView;
        }

        private void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (cmbCompanies.SelectedItem == null)
            {
                MessageBox.Show("Выберите компанию!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int companyId = (int)cmbCompanies.SelectedValue;
            string companyName = (cmbCompanies.SelectedItem as DataRowView)["Name"].ToString();

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Word документы (*.docx)|*.docx",
                DefaultExt = "docx",
                FileName = $"Report_{companyName}_{DateTime.Now:yyyyMMdd_HHmmss}.docx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    _service.GenerateCompanyReportToWord(companyId, saveDialog.FileName);
                    MessageBox.Show($"Отчёт успешно создан!\n{saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}