using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace InsuranceApp
{
    public partial class ContractDialog : Window
    {
        private SolvencyService _service;
        private int _companyId;
        private int? _contractId;

        public ContractDialog(SolvencyService service, int companyId, int? contractId = null)
        {
            InitializeComponent();
            _service = service;
            _companyId = companyId;
            _contractId = contractId;
            Owner = Application.Current.MainWindow;

            // Устанавливаем даты по умолчанию
            dpStartDate.SelectedDate = DateTime.Now;
            dpEndDate.SelectedDate = DateTime.Now.AddYears(1);

            // Загружаем виды страхования из справочника
            LoadInsuranceTypes();

            if (contractId.HasValue)
            {
                Title = "Редактирование договора";
                LoadContractData();
            }
            else
            {
                Title = "Добавление договора";
            }
        }

        private void LoadInsuranceTypes()
        {
            try
            {
                DataTable dt = _service.GetInsuranceTypes();
                cmbInsuranceType.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count > 0 && !_contractId.HasValue)
                {
                    cmbInsuranceType.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки видов страхования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadContractData()
        {
            try
            {
                string sql = @"SELECT c.*, it.TypeName 
                              FROM Contracts c
                              LEFT JOIN InsuranceTypes it ON c.InsuranceTypeId = it.Id
                              WHERE c.Id = @id";
                using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@id", _contractId)))
                {
                    if (reader.Read())
                    {
                        // Устанавливаем вид страхования
                        int insuranceTypeId = Convert.ToInt32(reader["InsuranceTypeId"]);
                        cmbInsuranceType.SelectedValue = insuranceTypeId;

                        // Устанавливаем даты
                        dpStartDate.SelectedDate = Convert.ToDateTime(reader["StartDate"]);
                        dpEndDate.SelectedDate = Convert.ToDateTime(reader["EndDate"]);

                        // Устанавливаем сумму премии
                        txtPremiumAmount.Text = reader["PremiumAmount"].ToString();

                        // Устанавливаем примечание (если есть)
                        if (reader["Note"] != DBNull.Value)
                            txtNote.Text = reader["Note"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных договора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (cmbInsuranceType.SelectedItem == null)
            {
                MessageBox.Show("Выберите вид страхования!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPremiumAmount.Text))
            {
                MessageBox.Show("Введите сумму премии!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpStartDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату начала договора!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpEndDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату окончания договора!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = dpStartDate.SelectedDate.Value;
            DateTime endDate = dpEndDate.SelectedDate.Value;

            if (endDate <= startDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты начала!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int insuranceTypeId = (int)cmbInsuranceType.SelectedValue;
                decimal premium = Convert.ToDecimal(txtPremiumAmount.Text);
                string note = txtNote.Text.Trim();

                if (premium <= 0)
                {
                    MessageBox.Show("Сумма премии должна быть больше 0!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_contractId.HasValue)
                {
                    // Обновление существующего договора
                    string sql = @"UPDATE Contracts SET 
                                  InsuranceTypeId = @typeId,
                                  StartDate = @startDate,
                                  EndDate = @endDate,
                                  PremiumAmount = @premium,
                                  Note = @note
                                  WHERE Id = @id";
                    DBHelper.ExecuteNonQuery(sql,
                        new MySqlParameter("@typeId", insuranceTypeId),
                        new MySqlParameter("@startDate", startDate),
                        new MySqlParameter("@endDate", endDate),
                        new MySqlParameter("@premium", premium),
                        new MySqlParameter("@note", note),
                        new MySqlParameter("@id", _contractId));
                }
                else
                {
                    // Добавление нового договора
                    string sql = @"INSERT INTO Contracts (CompanyId, InsuranceTypeId, StartDate, EndDate, PremiumAmount, Note) 
                                  VALUES (@companyId, @typeId, @startDate, @endDate, @premium, @note)";
                    DBHelper.ExecuteNonQuery(sql,
                        new MySqlParameter("@companyId", _companyId),
                        new MySqlParameter("@typeId", insuranceTypeId),
                        new MySqlParameter("@startDate", startDate),
                        new MySqlParameter("@endDate", endDate),
                        new MySqlParameter("@premium", premium),
                        new MySqlParameter("@note", note));
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Валидация ввода чисел
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.' && c != ',')
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}