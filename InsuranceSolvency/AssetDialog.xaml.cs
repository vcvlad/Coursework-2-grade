using System;
using System.Data;
using System.Windows;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace InsuranceApp
{
    public partial class AssetDialog : Window
    {
        private SolvencyService _service;
        private int _companyId;
        private int? _assetId;

        public AssetDialog(SolvencyService service, int companyId, int? assetId = null)
        {
            InitializeComponent();
            _service = service;
            _companyId = companyId;
            _assetId = assetId;
            Owner = Application.Current.MainWindow;

            // Устанавливаем текущую дату по умолчанию
            dpAssetDate.SelectedDate = DateTime.Now;

            // Загружаем типы активов из справочника
            LoadAssetTypes();

            if (assetId.HasValue)
            {
                Title = "Редактирование актива";
                LoadAssetData();
            }
            else
            {
                Title = "Добавление актива";
            }
        }

        private void LoadAssetTypes()
        {
            try
            {
                DataTable dt = _service.GetAssetTypes();
                cmbAssetType.ItemsSource = dt.DefaultView;

                if (dt.Rows.Count > 0 && !_assetId.HasValue)
                {
                    cmbAssetType.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов активов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAssetData()
        {
            try
            {
                string sql = @"SELECT a.*, at.TypeName 
                              FROM Assets a
                              LEFT JOIN AssetTypes at ON a.AssetTypeId = at.Id
                              WHERE a.Id = @id";
                using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@id", _assetId)))
                {
                    if (reader.Read())
                    {
                        // Устанавливаем тип актива
                        int assetTypeId = Convert.ToInt32(reader["AssetTypeId"]);
                        cmbAssetType.SelectedValue = assetTypeId;

                        dpAssetDate.SelectedDate = Convert.ToDateTime(reader["AssetDate"]);
                        txtAmount.Text = reader["Amount"].ToString();

                        if (reader["Note"] != DBNull.Value)
                            txtNote.Text = reader["Note"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных актива: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (cmbAssetType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип актива!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                MessageBox.Show("Введите сумму актива!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!dpAssetDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int assetTypeId = (int)cmbAssetType.SelectedValue;
                DateTime assetDate = dpAssetDate.SelectedDate.Value;
                decimal amount = Convert.ToDecimal(txtAmount.Text);
                string note = txtNote.Text.Trim();

                if (amount < 0)
                {
                    MessageBox.Show("Сумма не может быть отрицательной!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_assetId.HasValue)
                {
                    // Обновление существующего актива
                    string sql = @"UPDATE Assets SET 
                                  AssetTypeId = @typeId, 
                                  AssetDate = @date, 
                                  Amount = @amount,
                                  Note = @note
                                  WHERE Id = @id";
                    DBHelper.ExecuteNonQuery(sql,
                        new MySqlParameter("@typeId", assetTypeId),
                        new MySqlParameter("@date", assetDate),
                        new MySqlParameter("@amount", amount),
                        new MySqlParameter("@note", note),
                        new MySqlParameter("@id", _assetId));
                }
                else
                {
                    // Добавление нового актива
                    string sql = @"INSERT INTO Assets (CompanyId, AssetDate, AssetTypeId, Amount, Note) 
                                  VALUES (@companyId, @date, @typeId, @amount, @note)";
                    DBHelper.ExecuteNonQuery(sql,
                        new MySqlParameter("@companyId", _companyId),
                        new MySqlParameter("@date", assetDate),
                        new MySqlParameter("@typeId", assetTypeId),
                        new MySqlParameter("@amount", amount),
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