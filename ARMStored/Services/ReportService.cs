using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using ARMStored.Models;

namespace ARMStored.Services;

public class ReportService
{
    private readonly string _connectionString;

    public ReportService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgresConnection")
            ?? throw new InvalidOperationException("Строка подключения не найдена");
    }

    private NpgsqlConnection GetConnection() => new(_connectionString);

    // ==================== ОТЧЁТ ПО ОСТАТКАМ ====================
    public List<StockReportRow> GetStockReport()
    {
        var result = new List<StockReportRow>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT p.name, p.article, c.name as category, p.quantity, 
                   p.min_quantity, p.unit,
                   CASE 
                       WHEN p.quantity <= p.min_quantity THEN 'Критический'
                       WHEN p.quantity <= p.min_quantity * 1.5 THEN 'Низкий'
                       ELSE 'Норма'
                   END as status,
                   p.price, p.quantity * p.price as total_value
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id
            ORDER BY p.quantity ASC", conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new StockReportRow
            {
                ProductName = reader.GetString(0),
                Article = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Category = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Quantity = reader.GetInt32(3),
                MinQuantity = reader.GetInt32(4),
                Unit = reader.GetString(5),
                Status = reader.GetString(6),
                Price = reader.GetDecimal(7),
                TotalValue = reader.GetDecimal(8)
            });
        }
        return result;
    }

    // ==================== ОТЧЁТ ПО ДВИЖЕНИЮ ====================
    public List<TurnoverReportRow> GetTurnoverReport(DateTime from, DateTime to)
    {
        var result = new List<TurnoverReportRow>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT p.name, c.name as category,
                COALESCE(SUM(CASE WHEN wo.operation_type = 1 THEN wo.quantity END), 0) as incoming,
                COALESCE(SUM(CASE WHEN wo.operation_type = 2 THEN wo.quantity END), 0) as outgoing,
                COALESCE(SUM(CASE WHEN wo.operation_type = 4 THEN wo.quantity END), 0) as writeoff,
                COALESCE(AVG(wo.unit_price), 0) as avg_price,
                COALESCE(SUM(wo.quantity * wo.unit_price), 0) as turnover
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id
            LEFT JOIN warehouse_operations wo ON p.id = wo.product_id 
                AND wo.operation_date BETWEEN @from AND @to
            GROUP BY p.id, p.name, c.name
            ORDER BY turnover DESC", conn);

        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new TurnoverReportRow
            {
                ProductName = reader.GetString(0),
                Category = reader.IsDBNull(1) ? "" : reader.GetString(1),
                IncomingQty = reader.GetInt64(2),
                OutgoingQty = reader.GetInt64(3),
                WriteOffQty = reader.GetInt64(4),
                AvgPrice = reader.GetDecimal(5),
                Turnover = reader.GetDecimal(6)
            });
        }
        return result;
    }

    // ==================== ОТЧЁТ ПО ОПЕРАЦИЯМ ====================
    public List<OperationReportRow> GetOperationsReport(DateTime from, DateTime to, int? operationType = null)
    {
        var result = new List<OperationReportRow>();
        using var conn = GetConnection();
        conn.Open();

        var sql = @"
            SELECT wo.operation_date, wo.operation_type, p.name as product_name,
                   wo.quantity, wo.unit_price, wo.quantity * wo.unit_price as total,
                   u.full_name as user_name, wo.notes
            FROM warehouse_operations wo
            LEFT JOIN products p ON wo.product_id = p.id
            LEFT JOIN users u ON wo.user_id = u.id
            WHERE wo.operation_date BETWEEN @from AND @to";

        if (operationType.HasValue)
            sql += " AND wo.operation_type = @type";

        sql += " ORDER BY wo.operation_date DESC";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);
        if (operationType.HasValue)
            cmd.Parameters.AddWithValue("@type", operationType.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var type = (OperationType)reader.GetInt32(1);
            result.Add(new OperationReportRow
            {
                OperationDate = reader.GetDateTime(0),
                TypeName = type switch
                {
                    OperationType.Incoming => "Приход",
                    OperationType.Outgoing => "Расход",
                    OperationType.Transfer => "Перемещение",
                    OperationType.WriteOff => "Списание",
                    _ => "Неизвестно"
                },
                ProductName = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                UnitPrice = reader.GetDecimal(4),
                TotalAmount = reader.GetDecimal(5),
                UserName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                Notes = reader.IsDBNull(7) ? "" : reader.GetString(7)
            });
        }
        return result;
    }

    // ==================== ЭКСПОРТ В CSV ====================
    public void ExportToCsv<T>(List<T> data, string filePath)
    {
        if (data == null || data.Count == 0) return;

        var sb = new StringBuilder();
        var properties = typeof(T).GetProperties();

        sb.AppendLine(string.Join(";", properties.Select(p => p.Name)));

        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                if (value == null) return "";

                var str = value.ToString() ?? "";
                bool needsQuotes = str.Contains(';') || str.Contains('"') || str.Contains('\n') || str.Contains('\r');

                if (str.Contains('"'))
                {
                    str = str.Replace("\"", "\"\"");
                    needsQuotes = true;
                }

                if (needsQuotes)
                    str = "\"" + str + "\"";

                if (value is decimal dec)
                    return dec.ToString("F2", CultureInfo.InvariantCulture);

                if (value is double dbl)
                    return dbl.ToString("F2", CultureInfo.InvariantCulture);

                if (value is float flt)
                    return flt.ToString("F2", CultureInfo.InvariantCulture);

                if (value is DateTime dt)
                    return dt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

                return str;
            });

            sb.AppendLine(string.Join(";", values));
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    // ==================== ЭКСПОРТ В EXCEL (.xlsx) ====================
    public void ExportToExcel<T>(List<T> data, string filePath, string sheetName = "Отчёт")
    {
        if (data == null || data.Count == 0) return;

        // Настройка лицензии EPPlus (для некоммерческого использования)
        ExcelPackage.License.SetNonCommercialPersonal("My Name");

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add(sheetName);

        var properties = typeof(T).GetProperties();

        // Заголовки (строка 1)
        for (int i = 0; i < properties.Length; i++)
        {
            var cell = worksheet.Cells[1, i + 1];
            cell.Value = properties[i].Name;
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(40, 53, 147)); // #283593
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        }

        // Данные
        for (int row = 0; row < data.Count; row++)
        {
            for (int col = 0; col < properties.Length; col++)
            {
                var cell = worksheet.Cells[row + 2, col + 1];
                var value = properties[col].GetValue(data[row]);

                if (value == null)
                {
                    cell.Value = "";
                }
                else if (value is DateTime dt)
                {
                    cell.Value = dt;
                    cell.Style.Numberformat.Format = "dd.MM.yyyy HH:mm";
                }
                else if (value is decimal dec)
                {
                    cell.Value = dec;
                    cell.Style.Numberformat.Format = "#,##0.00 ₽";
                }
                else if (value is double dbl)
                {
                    cell.Value = dbl;
                    cell.Style.Numberformat.Format = "#,##0.00";
                }
                else if (value is int intVal)
                {
                    cell.Value = intVal;
                }
                else if (value is long longVal)
                {
                    cell.Value = longVal;
                }
                else
                {
                    cell.Value = value.ToString();
                }

                // Чередование фона строк
                if (row % 2 == 1)
                {
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(245, 245, 245)); // #F5F5F5
                }

                cell.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
        }

        // Автоширина столбцов
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        // Заморозка заголовка
        worksheet.View.FreezePanes(2, 1);

        // Сохранение
        package.SaveAs(new FileInfo(filePath));
    }

    public string GetDefaultFileName(string reportName, string extension = "csv")
    {
        return $"{reportName}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";
    }
}