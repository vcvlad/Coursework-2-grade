using System;
using System.Data;
using System.Linq;
using System.Windows;
using MySql.Data.MySqlClient;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace InsuranceApp
{
    public partial class ContractWindow : Window
    {
        private SolvencyService _service;
        private int _companyId;
        private string _companyName;
        private PlotModel _chartModel;

        public ContractWindow(SolvencyService service, int companyId, string companyName)
        {
            InitializeComponent();
            _service = service;
            _companyId = companyId;
            _companyName = companyName;
            Owner = Application.Current.MainWindow;

            tbCompanyName.Text = $"Страховая компания: {_companyName}";

            LoadAllData();
        }

        private void LoadAllData()
        {
            LoadContracts();
            LoadAssets();
            LoadLiabilities();
            LoadPremiumChart();
        }

        private void LoadContracts()
        {
            try
            {
                DataTable dt = _service.GetContractsByCompany(_companyId);
                dgContracts.ItemsSource = dt.DefaultView;

                decimal totalPremium = 0;
                foreach (DataRow row in dt.Rows)
                {
                    totalPremium += Convert.ToDecimal(row["PremiumAmount"]);
                }
                tbTotalPremium.Text = $"{totalPremium:N2} руб.";
                tbContractsCount.Text = dt.Rows.Count.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки договоров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAssets()
        {
            try
            {
                DataTable dt = _service.GetAssetsByCompany(_companyId);
                dgAssets.ItemsSource = dt.DefaultView;

                decimal totalAssets = 0;
                foreach (DataRow row in dt.Rows)
                {
                    totalAssets += Convert.ToDecimal(row["Amount"]);
                }
                tbTotalAssets.Text = $"{totalAssets:N2} руб.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки активов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLiabilities()
        {
            try
            {
                DataTable dt = _service.GetLiabilitiesByCompany(_companyId);
                dgLiabilities.ItemsSource = dt.DefaultView;

                decimal totalLiabilities = 0;
                foreach (DataRow row in dt.Rows)
                {
                    totalLiabilities += Convert.ToDecimal(row["Amount"]);
                }
                tbTotalLiabilities.Text = $"{totalLiabilities:N2} руб.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки обязательств: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPremiumChart()
        {
            try
            {
                DataTable dt = _service.GetMonthlyPremium(_companyId);

                // Создаём модель графика
                _chartModel = new PlotModel { Title = "Динамика страховых премий по месяцам" };

                // Для BarSeries категории должны быть на оси Y (вертикальной)
                var categoryAxis = new CategoryAxis
                {
                    Position = AxisPosition.Left,  // Категории слева
                    Title = "Месяц",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.None
                };

                // Ось значений на X (горизонтальная)
                var valueAxis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,  // Значения снизу
                    Title = "Сумма премий (руб.)",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    Minimum = 0
                };

                // Создаём серию столбцов (BarSeries - горизонтальные столбцы)
                var series = new BarSeries
                {
                    Title = "Страховые премии",
                    FillColor = OxyColors.SteelBlue,
                    StrokeColor = OxyColors.DarkBlue,
                    StrokeThickness = 1,
                    LabelFormatString = "{0:N0}" // Формат отображения значений на столбцах
                };

                decimal maxPremium = 0;
                decimal minPremium = decimal.MaxValue;
                string maxMonth = "";
                string minMonth = "";

                // Заполняем данные
                foreach (DataRow row in dt.Rows)
                {
                    string yearMonth = row["YearMonth"].ToString();
                    decimal monthlyPremium = Convert.ToDecimal(row["MonthlyPremium"]);

                    // Добавляем значение в серию
                    series.Items.Add(new BarItem { Value = (double)monthlyPremium });

                    // Добавляем метку категории (название месяца)
                    categoryAxis.Labels.Add(yearMonth);

                    // Поиск максимума и минимума для статистики
                    if (monthlyPremium > maxPremium)
                    {
                        maxPremium = monthlyPremium;
                        maxMonth = yearMonth;
                    }
                    if (monthlyPremium < minPremium)
                    {
                        minPremium = monthlyPremium;
                        minMonth = yearMonth;
                    }
                }

                // Добавляем оси и серию в модель
                _chartModel.Axes.Add(categoryAxis);
                _chartModel.Axes.Add(valueAxis);
                _chartModel.Series.Add(series);

                // Обновляем статистику
                if (dt.Rows.Count > 0)
                {
                    tbMaxMonth.Text = $"{maxMonth} ({maxPremium:N2} руб.)";
                    tbMinMonth.Text = $"{minMonth} ({minPremium:N2} руб.)";
                }
                else
                {
                    tbMaxMonth.Text = "нет данных";
                    tbMinMonth.Text = "нет данных";
                }

                // Устанавливаем модель графика
                PremiumChart.Model = _chartModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки графика премий: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetIdFromSelectedItem(object selectedItem)
        {
            if (selectedItem is DataRowView rowView)
            {
                object value = rowView["Id"];
                if (value != DBNull.Value)
                {
                    return Convert.ToInt32(value);
                }
            }
            return -1;
        }

        // ========== Обработчики для договоров ==========
        private void BtnAddContract_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContractDialog(_service, _companyId);
            if (dialog.ShowDialog() == true)
            {
                LoadContracts();
                LoadPremiumChart();
                LoadLiabilities();
            }
        }

        private void BtnEditContract_Click(object sender, RoutedEventArgs e)
        {
            if (dgContracts.SelectedItem == null)
            {
                MessageBox.Show("Выберите договор для редактирования!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int contractId = GetIdFromSelectedItem(dgContracts.SelectedItem);
            if (contractId == -1)
            {
                MessageBox.Show("Не удалось определить идентификатор договора!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new ContractDialog(_service, _companyId, contractId);
            if (dialog.ShowDialog() == true)
            {
                LoadContracts();
                LoadPremiumChart();
            }
        }

        private void BtnDeleteContract_Click(object sender, RoutedEventArgs e)
        {
            if (dgContracts.SelectedItem == null)
            {
                MessageBox.Show("Выберите договор для удаления!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранный договор?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    int contractId = GetIdFromSelectedItem(dgContracts.SelectedItem);
                    if (contractId == -1)
                    {
                        MessageBox.Show("Не удалось определить идентификатор договора!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string sql = "DELETE FROM Contracts WHERE Id = @id";
                    DBHelper.ExecuteNonQuery(sql, new MySqlParameter("@id", contractId));

                    LoadContracts();
                    LoadPremiumChart();
                    LoadLiabilities();

                    MessageBox.Show("Договор успешно удалён!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== Обработчики для активов ==========
        private void BtnAddAsset_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AssetDialog(_service, _companyId);
            if (dialog.ShowDialog() == true)
            {
                LoadAssets();
            }
        }

        private void BtnEditAsset_Click(object sender, RoutedEventArgs e)
        {
            if (dgAssets.SelectedItem == null)
            {
                MessageBox.Show("Выберите актив для редактирования!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int assetId = GetIdFromSelectedItem(dgAssets.SelectedItem);
            if (assetId == -1)
            {
                MessageBox.Show("Не удалось определить идентификатор актива!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new AssetDialog(_service, _companyId, assetId);
            if (dialog.ShowDialog() == true)
            {
                LoadAssets();
            }
        }

        private void BtnDeleteAsset_Click(object sender, RoutedEventArgs e)
        {
            if (dgAssets.SelectedItem == null)
            {
                MessageBox.Show("Выберите актив для удаления!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранный актив?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    int assetId = GetIdFromSelectedItem(dgAssets.SelectedItem);
                    if (assetId == -1)
                    {
                        MessageBox.Show("Не удалось определить идентификатор актива!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string sql = "DELETE FROM Assets WHERE Id = @id";
                    DBHelper.ExecuteNonQuery(sql, new MySqlParameter("@id", assetId));

                    LoadAssets();

                    MessageBox.Show("Актив успешно удалён!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ========== Обработчики для обязательств ==========
        private void BtnAddLiability_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new LiabilityDialog(_service, _companyId);
            if (dialog.ShowDialog() == true)
            {
                LoadLiabilities();
            }
        }

        private void BtnEditLiability_Click(object sender, RoutedEventArgs e)
        {
            if (dgLiabilities.SelectedItem == null)
            {
                MessageBox.Show("Выберите обязательство для редактирования!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int liabilityId = GetIdFromSelectedItem(dgLiabilities.SelectedItem);
            if (liabilityId == -1)
            {
                MessageBox.Show("Не удалось определить идентификатор обязательства!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new LiabilityDialog(_service, _companyId, liabilityId);
            if (dialog.ShowDialog() == true)
            {
                LoadLiabilities();
            }
        }

        private void BtnDeleteLiability_Click(object sender, RoutedEventArgs e)
        {
            if (dgLiabilities.SelectedItem == null)
            {
                MessageBox.Show("Выберите обязательство для удаления!", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранное обязательство?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    int liabilityId = GetIdFromSelectedItem(dgLiabilities.SelectedItem);
                    if (liabilityId == -1)
                    {
                        MessageBox.Show("Не удалось определить идентификатор обязательства!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string sql = "DELETE FROM Liabilities WHERE Id = @id";
                    DBHelper.ExecuteNonQuery(sql, new MySqlParameter("@id", liabilityId));

                    LoadLiabilities();

                    MessageBox.Show("Обязательство успешно удалено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllData();
            MessageBox.Show("Данные обновлены!", "Обновление",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}