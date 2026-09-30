using System;
using System.Data;
using MySql.Data.MySqlClient;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace InsuranceApp
{
    public class SolvencyService
    {
        // =====================================================
        // БАЗОВЫЕ CRUD-ОПЕРАЦИИ
        // =====================================================

        public DataTable GetAllCompanies()
        {
            string sql = "SELECT * FROM Companies ORDER BY Name";
            using (var reader = DBHelper.ExecuteReader(sql))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        public void AddCompany(string name, string unp, string address)
        {
            string sql = "INSERT INTO Companies (Name, Unp, LegalAddress) VALUES (@name, @unp, @addr)";
            DBHelper.ExecuteNonQuery(sql,
                new MySqlParameter("@name", name),
                new MySqlParameter("@unp", unp),
                new MySqlParameter("@addr", address));
        }

        public void UpdateCompany(int id, string name, string unp, string address)
        {
            string sql = "UPDATE Companies SET Name=@name, Unp=@unp, LegalAddress=@addr WHERE Id=@id";
            DBHelper.ExecuteNonQuery(sql,
                new MySqlParameter("@name", name),
                new MySqlParameter("@unp", unp),
                new MySqlParameter("@addr", address),
                new MySqlParameter("@id", id));
        }

        public void DeleteCompany(int id)
        {
            string sql = "DELETE FROM Companies WHERE Id=@id";
            DBHelper.ExecuteNonQuery(sql, new MySqlParameter("@id", id));
        }

        // =====================================================
        // ПОЛУЧЕНИЕ ДАННЫХ ПО КОМПАНИЯМ
        // =====================================================

        public DataTable GetContractsByCompany(int companyId)
        {
            // Исправлено: ContractDate → StartDate, EndDate
            string sql = @"SELECT c.*, it.TypeName 
                           FROM Contracts c 
                           JOIN InsuranceTypes it ON c.InsuranceTypeId = it.Id 
                           WHERE c.CompanyId = @cid
                           ORDER BY c.StartDate";
            using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@cid", companyId)))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        public DataTable GetAssetsByCompany(int companyId)
        {
            // Исправлено: добавлено получение названия типа актива
            string sql = @"SELECT a.Id, a.CompanyId, a.AssetDate, at.TypeName AS AssetType, a.Amount
                           FROM Assets a
                           LEFT JOIN AssetTypes at ON a.AssetTypeId = at.Id
                           WHERE a.CompanyId = @cid
                           ORDER BY a.AssetDate";
            using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@cid", companyId)))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        public DataTable GetLiabilitiesByCompany(int companyId)
        {
            // Исправлено: добавлено получение названия типа обязательства
            string sql = @"SELECT l.Id, l.CompanyId, l.LiabilityDate, lt.TypeName AS ReserveType, l.Amount
                           FROM Liabilities l
                           LEFT JOIN LiabilityTypes lt ON l.LiabilityTypeId = lt.Id
                           WHERE l.CompanyId = @cid
                           ORDER BY l.LiabilityDate";
            using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@cid", companyId)))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        // =====================================================
        // ПРЕДСТАВЛЕНИЯ
        // =====================================================

        public DataTable GetSolvencyView()
        {
            // Используем прямое вычисление вместо представления
            string sql = @"
        SELECT 
            c.Id,
            c.Name,
            COALESCE((SELECT SUM(Amount) FROM Assets WHERE CompanyId = c.Id), 0) AS TotalAssets,
            COALESCE((SELECT SUM(Amount) FROM Liabilities WHERE CompanyId = c.Id), 0) AS TotalLiabilities,
            COALESCE((SELECT SUM(Amount) FROM Assets WHERE CompanyId = c.Id), 0) - 
            COALESCE((SELECT SUM(Amount) FROM Liabilities WHERE CompanyId = c.Id), 0) AS ActualMargin,
            COALESCE((SELECT SUM(PremiumAmount) FROM Contracts WHERE CompanyId = c.Id AND StartDate >= DATE_SUB(CURDATE(), INTERVAL 1 YEAR)), 0) AS AnnualPremium,
            ROUND(COALESCE((SELECT SUM(PremiumAmount) FROM Contracts WHERE CompanyId = c.Id AND StartDate >= DATE_SUB(CURDATE(), INTERVAL 1 YEAR)), 0) * 0.16, 2) AS NormativeMargin,
            CASE 
                WHEN (COALESCE((SELECT SUM(Amount) FROM Assets WHERE CompanyId = c.Id), 0) - 
                      COALESCE((SELECT SUM(Amount) FROM Liabilities WHERE CompanyId = c.Id), 0)) > 
                     (COALESCE((SELECT SUM(PremiumAmount) FROM Contracts WHERE CompanyId = c.Id AND StartDate >= DATE_SUB(CURDATE(), INTERVAL 1 YEAR)), 0) * 0.16)
                THEN 'Платежеспособна'
                ELSE 'Не платежеспособна'
            END AS Status
        FROM Companies c
        ORDER BY c.Id";

            using (var reader = DBHelper.ExecuteReader(sql))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        public DataTable GetMonthlyPremium(int companyId)
        {
            string sql = @"SELECT 
                    DATE_FORMAT(StartDate, '%Y-%m') AS YearMonth,
                    SUM(PremiumAmount) AS MonthlyPremium
                   FROM Contracts 
                   WHERE CompanyId = @companyId
                   GROUP BY DATE_FORMAT(StartDate, '%Y-%m')
                   ORDER BY YearMonth";
            using (var reader = DBHelper.ExecuteReader(sql, new MySqlParameter("@companyId", companyId)))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        // =====================================================
        // СПРАВОЧНИКИ
        // =====================================================

        public DataTable GetInsuranceTypes()
        {
            string sql = "SELECT Id, TypeName FROM InsuranceTypes ORDER BY TypeName";
            using (var reader = DBHelper.ExecuteReader(sql))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        public DataTable GetAssetTypes()
        {
            string sql = "SELECT Id, TypeName FROM AssetTypes ORDER BY TypeName";
            using (var reader = DBHelper.ExecuteReader(sql))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        public DataTable GetLiabilityTypes()
        {
            string sql = "SELECT Id, TypeName FROM LiabilityTypes ORDER BY TypeName";
            using (var reader = DBHelper.ExecuteReader(sql))
            {
                DataTable dt = new DataTable();
                dt.Load(reader);
                return dt;
            }
        }

        // =====================================================
        // ОПЕРАЦИИ С АКТИВАМИ
        // =====================================================

        public void AddAsset(int companyId, DateTime assetDate, int assetTypeId, decimal amount, string note = "")
        {
            string sql = @"INSERT INTO Assets (CompanyId, AssetDate, AssetTypeId, Amount, Note) 
                           VALUES (@companyId, @date, @typeId, @amount, @note)";
            DBHelper.ExecuteNonQuery(sql,
                new MySqlParameter("@companyId", companyId),
                new MySqlParameter("@date", assetDate),
                new MySqlParameter("@typeId", assetTypeId),
                new MySqlParameter("@amount", amount),
                new MySqlParameter("@note", note));
        }

        public void UpdateAsset(int assetId, DateTime assetDate, int assetTypeId, decimal amount, string note = "")
        {
            string sql = @"UPDATE Assets SET 
                           AssetDate = @date, 
                           AssetTypeId = @typeId, 
                           Amount = @amount,
                           Note = @note
                           WHERE Id = @id";
            DBHelper.ExecuteNonQuery(sql,
                new MySqlParameter("@date", assetDate),
                new MySqlParameter("@typeId", assetTypeId),
                new MySqlParameter("@amount", amount),
                new MySqlParameter("@note", note),
                new MySqlParameter("@id", assetId));
        }

        // =====================================================
        // ОПЕРАЦИИ С ОБЯЗАТЕЛЬСТВАМИ
        // =====================================================

        public void AddLiability(int companyId, DateTime liabilityDate, int liabilityTypeId, decimal amount, string description = "")
        {
            string sql = @"INSERT INTO Liabilities (CompanyId, LiabilityDate, LiabilityTypeId, Amount, Description) 
                           VALUES (@companyId, @date, @typeId, @amount, @description)";
            DBHelper.ExecuteNonQuery(sql,
                new MySqlParameter("@companyId", companyId),
                new MySqlParameter("@date", liabilityDate),
                new MySqlParameter("@typeId", liabilityTypeId),
                new MySqlParameter("@amount", amount),
                new MySqlParameter("@description", description));
        }

        public void UpdateLiability(int liabilityId, DateTime liabilityDate, int liabilityTypeId, decimal amount, string description = "")
        {
            string sql = @"UPDATE Liabilities SET 
                           LiabilityDate = @date, 
                           LiabilityTypeId = @typeId, 
                           Amount = @amount,
                           Description = @description
                           WHERE Id = @id";
            DBHelper.ExecuteNonQuery(sql,
                new MySqlParameter("@date", liabilityDate),
                new MySqlParameter("@typeId", liabilityTypeId),
                new MySqlParameter("@amount", amount),
                new MySqlParameter("@description", description),
                new MySqlParameter("@id", liabilityId));
        }

        // =====================================================
        // ГЕНЕРАЦИЯ WORD-ОТЧЁТА
        // =====================================================

        public void GenerateCompanyReportToWord(int companyId, string filePath)
        {
            // Получаем данные компании (исправлено: убрано ct.ContractDate)
            string companySql = @"SELECT c.*, 
                                    COALESCE(SUM(a.Amount), 0) AS TotalAssets,
                                    COALESCE(SUM(l.Amount), 0) AS TotalLiabilities,
                                    (COALESCE(SUM(a.Amount), 0) - COALESCE(SUM(l.Amount), 0)) AS ActualMargin,
                                    COALESCE(SUM(ct.PremiumAmount), 0) AS AnnualPremium
                                  FROM Companies c
                                  LEFT JOIN Assets a ON c.Id = a.CompanyId
                                  LEFT JOIN Liabilities l ON c.Id = l.CompanyId
                                  LEFT JOIN Contracts ct ON c.Id = ct.CompanyId 
                                      AND ct.StartDate >= DATE_SUB(CURDATE(), INTERVAL 1 YEAR)
                                  WHERE c.Id = @cid
                                  GROUP BY c.Id";

            DataTable companyData = new DataTable();
            using (var reader = DBHelper.ExecuteReader(companySql, new MySqlParameter("@cid", companyId)))
            {
                companyData.Load(reader);
            }

            if (companyData.Rows.Count == 0)
                throw new Exception("Компания не найдена");

            DataRow company = companyData.Rows[0];
            decimal totalAssets = Convert.ToDecimal(company["TotalAssets"]);
            decimal totalLiabilities = Convert.ToDecimal(company["TotalLiabilities"]);
            decimal actualMargin = Convert.ToDecimal(company["ActualMargin"]);
            decimal annualPremium = Convert.ToDecimal(company["AnnualPremium"]);
            decimal normativeMargin = annualPremium * 0.16m;
            string status = actualMargin > normativeMargin ? "Платежеспособна" : "Не платежеспособна";

            // Получаем списки активов
            string assetsSql = @"SELECT a.*, at.TypeName 
                                 FROM Assets a 
                                 LEFT JOIN AssetTypes at ON a.AssetTypeId = at.Id 
                                 WHERE a.CompanyId = @cid
                                 ORDER BY a.AssetDate";
            DataTable assets = new DataTable();
            using (var reader = DBHelper.ExecuteReader(assetsSql, new MySqlParameter("@cid", companyId)))
            {
                assets.Load(reader);
            }

            // Получаем списки обязательств
            string liabilitiesSql = @"SELECT l.*, lt.TypeName 
                                      FROM Liabilities l 
                                      LEFT JOIN LiabilityTypes lt ON l.LiabilityTypeId = lt.Id 
                                      WHERE l.CompanyId = @cid
                                      ORDER BY l.LiabilityDate";
            DataTable liabilities = new DataTable();
            using (var reader = DBHelper.ExecuteReader(liabilitiesSql, new MySqlParameter("@cid", companyId)))
            {
                liabilities.Load(reader);
            }

            // Получаем список договоров (исправлено: используем StartDate)
            string contractsSql = @"SELECT ct.*, it.TypeName 
                                    FROM Contracts ct 
                                    LEFT JOIN InsuranceTypes it ON ct.InsuranceTypeId = it.Id 
                                    WHERE ct.CompanyId = @cid
                                    ORDER BY ct.StartDate";
            DataTable contracts = new DataTable();
            using (var reader = DBHelper.ExecuteReader(contractsSql, new MySqlParameter("@cid", companyId)))
            {
                contracts.Load(reader);
            }

            // Создаём Word-документ
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document();
                Body body = mainPart.Document.AppendChild(new Body());

                // Заголовок отчёта
                Paragraph headerPara = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
                Run headerRun = headerPara.AppendChild(new Run());
                headerRun.AppendChild(new Text($"Отчёт о платежеспособности страховой компании"));
                headerRun.RunProperties = new RunProperties(new Bold() { Val = true });
                body.AppendChild(headerPara);

                body.AppendChild(new Paragraph());

                // Информация о компании
                Paragraph companyTitlePara = new Paragraph();
                Run companyTitleRun = companyTitlePara.AppendChild(new Run());
                companyTitleRun.AppendChild(new Text("1. Информация о компании"));
                companyTitleRun.RunProperties = new RunProperties(new Bold() { Val = true });
                body.AppendChild(companyTitlePara);

                body.AppendChild(new Paragraph(new Run(new Text($"Наименование: {company["Name"]}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"УНП: {company["Unp"]}"))));
                body.AppendChild(new Paragraph(new Run(new Text($"Юридический адрес: {company["LegalAddress"]}"))));

                body.AppendChild(new Paragraph());

                // Финансовые показатели
                Paragraph financeTitlePara = new Paragraph();
                Run financeTitleRun = financeTitlePara.AppendChild(new Run());
                financeTitleRun.AppendChild(new Text("2. Финансовые показатели"));
                financeTitleRun.RunProperties = new RunProperties(new Bold() { Val = true });
                body.AppendChild(financeTitlePara);

                body.AppendChild(new Paragraph(new Run(new Text($"Сумма активов: {totalAssets:F2} руб."))));
                body.AppendChild(new Paragraph(new Run(new Text($"Сумма обязательств (резервов): {totalLiabilities:F2} руб."))));
                body.AppendChild(new Paragraph(new Run(new Text($"Фактический запас платежеспособности: {actualMargin:F2} руб."))));
                body.AppendChild(new Paragraph(new Run(new Text($"Годовая страховая премия: {annualPremium:F2} руб."))));
                body.AppendChild(new Paragraph(new Run(new Text($"Нормативный запас (16% от годовой премии): {normativeMargin:F2} руб."))));
                body.AppendChild(new Paragraph(new Run(new Text($"Статус: {status}"))));

                body.AppendChild(new Paragraph());

                // Состав активов
                Paragraph assetsTitlePara = new Paragraph();
                Run assetsTitleRun = assetsTitlePara.AppendChild(new Run());
                assetsTitleRun.AppendChild(new Text("3. Состав активов"));
                assetsTitleRun.RunProperties = new RunProperties(new Bold() { Val = true });
                body.AppendChild(assetsTitlePara);

                if (assets.Rows.Count > 0)
                {
                    foreach (DataRow row in assets.Rows)
                    {
                        string assetType = row["TypeName"]?.ToString() ?? "Неизвестный тип";
                        body.AppendChild(new Paragraph(new Run(new Text($"• {assetType}: {Convert.ToDecimal(row["Amount"]):F2} руб. (дата: {Convert.ToDateTime(row["AssetDate"]):dd.MM.yyyy})"))));
                    }
                }
                else
                {
                    body.AppendChild(new Paragraph(new Run(new Text("Нет данных об активах"))));
                }

                body.AppendChild(new Paragraph());

                // Состав обязательств
                Paragraph liabilitiesTitlePara = new Paragraph();
                Run liabilitiesTitleRun = liabilitiesTitlePara.AppendChild(new Run());
                liabilitiesTitleRun.AppendChild(new Text("4. Состав обязательств (страховых резервов)"));
                liabilitiesTitleRun.RunProperties = new RunProperties(new Bold() { Val = true });
                body.AppendChild(liabilitiesTitlePara);

                if (liabilities.Rows.Count > 0)
                {
                    foreach (DataRow row in liabilities.Rows)
                    {
                        string liabilityType = row["TypeName"]?.ToString() ?? "Неизвестный тип";
                        body.AppendChild(new Paragraph(new Run(new Text($"• {liabilityType}: {Convert.ToDecimal(row["Amount"]):F2} руб. (дата: {Convert.ToDateTime(row["LiabilityDate"]):dd.MM.yyyy})"))));
                    }
                }
                else
                {
                    body.AppendChild(new Paragraph(new Run(new Text("Нет данных об обязательствах"))));
                }

                body.AppendChild(new Paragraph());

                // Список договоров страхования
                Paragraph contractsTitlePara = new Paragraph();
                Run contractsTitleRun = contractsTitlePara.AppendChild(new Run());
                contractsTitleRun.AppendChild(new Text("5. Список договоров страхования"));
                contractsTitleRun.RunProperties = new RunProperties(new Bold() { Val = true });
                body.AppendChild(contractsTitlePara);

                if (contracts.Rows.Count > 0)
                {
                    foreach (DataRow row in contracts.Rows)
                    {
                        string startDate = Convert.ToDateTime(row["StartDate"]).ToString("dd.MM.yyyy");
                        string endDate = Convert.ToDateTime(row["EndDate"]).ToString("dd.MM.yyyy");
                        string insuranceType = row["TypeName"]?.ToString() ?? "Неизвестный вид";
                        body.AppendChild(new Paragraph(new Run(new Text($"• {insuranceType} | {startDate} - {endDate} | Премия: {Convert.ToDecimal(row["PremiumAmount"]):F2} руб."))));
                    }
                }
                else
                {
                    body.AppendChild(new Paragraph(new Run(new Text("Нет данных о договорах страхования"))));
                }

                body.AppendChild(new Paragraph());

                // Дата формирования отчёта
                Paragraph datePara = new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Right }));
                datePara.AppendChild(new Run(new Text($"Дата формирования отчёта: {DateTime.Now:dd.MM.yyyy}")));
                body.AppendChild(datePara);
            }
        }
    }
}