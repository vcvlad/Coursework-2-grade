using System;
using System.Data;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using System.Text;
using MySql.Data.MySqlClient;

namespace InsuranceApp
{
    public partial class SolvencyWindow : Window
    {
        private SolvencyService _service;
        private DataTable _solvencyData;

        public SolvencyWindow(SolvencyService service)
        {
            InitializeComponent();
            _service = service;
            Owner = Application.Current.MainWindow;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _solvencyData = _service.GetSolvencyView();
                dgSolvency.ItemsSource = _solvencyData.DefaultView;

                int totalCompanies = _solvencyData.Rows.Count;
                int solventCount = 0;
                int insolventCount = 0;

                foreach (DataRow row in _solvencyData.Rows)
                {
                    string status = row["Status"].ToString();
                    if (status == "Платежеспособна")
                        solventCount++;
                    else if (status == "Не платежеспособна")
                        insolventCount++;
                }

                Title = $"Анализ платежеспособности | Всего: {totalCompanies} | Платежеспособны: {solventCount} | Не платежеспособны: {insolventCount}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DataTable CalculateSolvencyDirectly()
        {
            DataTable result = new DataTable();
            result.Columns.Add("Id", typeof(int));
            result.Columns.Add("Name", typeof(string));
            result.Columns.Add("Unp", typeof(string));
            result.Columns.Add("TotalAssets", typeof(decimal));
            result.Columns.Add("TotalLiabilities", typeof(decimal));
            result.Columns.Add("ActualMargin", typeof(decimal));
            result.Columns.Add("AnnualPremium", typeof(decimal));
            result.Columns.Add("NormativeMargin", typeof(decimal));
            result.Columns.Add("Status", typeof(string));

            try
            {
                string companiesSql = "SELECT Id, Name, Unp, LegalAddress FROM Companies";
                DataTable companies = new DataTable();
                using (var reader = DBHelper.ExecuteReader(companiesSql))
                {
                    companies.Load(reader);
                }

                foreach (DataRow company in companies.Rows)
                {
                    int companyId = Convert.ToInt32(company["Id"]);
                    string companyName = company["Name"].ToString();
                    string unp = company["Unp"].ToString();

                    
                    decimal totalAssets = 0;
                    string assetsSql = "SELECT COALESCE(SUM(Amount), 0) AS Total FROM Assets WHERE CompanyId = @cid";
                    using (var reader = DBHelper.ExecuteReader(assetsSql, new MySqlParameter("@cid", companyId)))
                    {
                        if (reader.Read())
                        {
                            totalAssets = Convert.ToDecimal(reader["Total"]);
                        }
                    }

                    
                    decimal totalLiabilities = 0;
                    string liabilitiesSql = "SELECT COALESCE(SUM(Amount), 0) AS Total FROM Liabilities WHERE CompanyId = @cid";
                    using (var reader = DBHelper.ExecuteReader(liabilitiesSql, new MySqlParameter("@cid", companyId)))
                    {
                        if (reader.Read())
                        {
                            totalLiabilities = Convert.ToDecimal(reader["Total"]);
                        }
                    }

                    
                    decimal annualPremium = 0;
                    string premiumSql = @"SELECT COALESCE(SUM(PremiumAmount), 0) AS Total 
                                         FROM Contracts 
                                         WHERE CompanyId = @cid 
                                         AND ContractDate >= DATE_SUB(CURDATE(), INTERVAL 1 YEAR)";
                    using (var reader = DBHelper.ExecuteReader(premiumSql, new MySqlParameter("@cid", companyId)))
                    {
                        if (reader.Read())
                        {
                            annualPremium = Convert.ToDecimal(reader["Total"]);
                        }
                    }

                    // Расчет показателей
                    decimal actualMargin = totalAssets - totalLiabilities;
                    decimal normativeMargin = annualPremium * 0.16m; // 16% от годовой премии
                    string status = actualMargin > normativeMargin ? "Платежеспособна" : "Не платежеспособна";

                    // Добавляем строку в результат
                    result.Rows.Add(companyId, companyName, unp, totalAssets, totalLiabilities,
                                    actualMargin, annualPremium, normativeMargin, status);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }

            return result;
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt",
                    DefaultExt = "csv",
                    FileName = $"Solvency_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    ExportToCsv(saveDialog.FileName);
                    MessageBox.Show($"Данные успешно экспортированы в файл:\n{saveDialog.FileName}",
                        "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToCsv(string filePath)
        {
            StringBuilder sb = new StringBuilder();

            if (_solvencyData != null && _solvencyData.Rows.Count > 0)
            {
                // Заголовки колонок на русском языке
                sb.Append("ID;Название компании;УНП;Активы (руб.);Обязательства (руб.);Фактический запас (руб.);Годовая премия (руб.);Нормативный запас (руб.);Статус");
                sb.AppendLine();

                // Данные
                foreach (DataRow row in _solvencyData.Rows)
                {
                    sb.Append($"{row["Id"]};");
                    sb.Append($"\"{row["Name"]}\";");
                    sb.Append($"{row["Unp"]};");
                    sb.Append($"{Convert.ToDecimal(row["TotalAssets"]):F2};");
                    sb.Append($"{Convert.ToDecimal(row["TotalLiabilities"]):F2};");
                    sb.Append($"{Convert.ToDecimal(row["ActualMargin"]):F2};");
                    sb.Append($"{Convert.ToDecimal(row["AnnualPremium"]):F2};");
                    sb.Append($"{Convert.ToDecimal(row["NormativeMargin"]):F2};");
                    sb.Append($"{row["Status"]}");
                    sb.AppendLine();
                }

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
            else
            {
                MessageBox.Show("Нет данных для экспорта!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}