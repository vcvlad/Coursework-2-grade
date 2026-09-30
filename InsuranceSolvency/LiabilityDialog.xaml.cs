using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace InsuranceApp
{
    public partial class LiabilityDialog : Window
    {
        private SolvencyService _service;
        private int _companyId;
        private int? _liabilityId;

        public LiabilityDialog(SolvencyService service, int companyId, int? liabilityId = null)
        {
            InitializeComponent();
            _service = service;
            _companyId = companyId;
            _liabilityId = liabilityId;
            Owner = Application.Current.MainWindow;

            // Устанавливаем текущую дату по умолчанию
            dpLiabilityDate.SelectedDate = DateTime.Now;

            // Загружаем типы обязательств из справочника
            LoadLiabilityTypes();

            if (liabilityId.HasValue)
            {
                Title = "Редактирование обязательства";
                LoadLiabilityData();
            }
            else
            {
                Title = "Добавление обязательства";
            }
        }

        private void LoadLiabilityTypes()
        {
            try
            {
                DataTable dt = _service.GetLiabilityTypes();
                cmbReserveType.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count > 0 && !_liabilityId.HasValue)
                {
                    cmbReserveType.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов обязательств: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLiabilityData()
        {
            try
            {
                string sql = @"SELECT l.*, lt.TypeName 
                              FROM Liabilities l
                              LEFT JOIN LiabilityTypes lt ON l.LiabilityTypeId = lt.Id
                              WHERE l.Id = @id";
                using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@id", _liabilityId)))
                {
                    if (reader.Read())
                    {
                        // Устанавливаем тип резерва
                        int liabilityTypeId = Convert.ToInt32(reader["LiabilityTypeId"]);
                        cmbReserveType.SelectedValue = liabilityTypeId;

                        dpLiabilityDate.SelectedDate = Convert.ToDateTime(reader["LiabilityDate"]);
                        txtAmount.Text = reader["Amount"].ToString();

                        if (reader["Description"] != DBNull.Value)
                            txtDescription.Text = reader["Description"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных обязательства: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (cmbReserveType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип резерва!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("Введите сумму обязательства!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpLiabilityDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int liabilityTypeId = (int)cmbReserveType.SelectedValue;
                DateTime liabilityDate = dpLiabilityDate.SelectedDate.Value;
                decimal amount = Convert.ToDecimal(txtAmount.Text);
                string description = txtDescription.Text.Trim();

                if (amount < 0)
                {
                    MessageBox.Show("Сумма не может быть отрицательной!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_liabilityId.HasValue)
                {
                    // Обновление существующего обязательства
                    string sql = @"UPDATE Liabilities SET 
                                  LiabilityTypeId = @typeId, 
                                  LiabilityDate = @date, 
                                  Amount = @amount,
                                  Description = @description
                                  WHERE Id = @id";
                    DBHelper.ExecuteNonQuery(sql,
                        new MySqlParameter("@typeId", liabilityTypeId),
                        new MySqlParameter("@date", liabilityDate),
                        new MySqlParameter("@amount", amount),
                        new MySqlParameter("@description", description),
                        new MySqlParameter("@id", _liabilityId));
                }
                else
                {
                    // Добавление нового обязательства
                    string sql = @"INSERT INTO Liabilities (CompanyId, LiabilityDate, LiabilityTypeId, Amount, Description) 
                                  VALUES (@companyId, @date, @typeId, @amount, @description)";
                    DBHelper.ExecuteNonQuery(sql,
                        new MySqlParameter("@companyId", _companyId),
                        new MySqlParameter("@date", liabilityDate),
                        new MySqlParameter("@typeId", liabilityTypeId),
                        new MySqlParameter("@amount", amount),
                        new MySqlParameter("@description", description));
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