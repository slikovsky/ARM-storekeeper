using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;
using ARMStored.Models;

namespace ARMStored.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PostgresConnection") 
            ?? throw new InvalidOperationException("Строка подключения не найдена");
    }

    public NpgsqlConnection GetConnection() => new(_connectionString);

    // ==================== ПОЛЬЗОВАТЕЛИ ====================
    
    public User? GetUserByUsername(string username)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(
            "SELECT id, username, password_hash, full_name, \"role\", is_active, created_at, last_login " +
            "FROM users WHERE username = @username AND is_active = true", conn);
        cmd.Parameters.AddWithValue("@username", username);
        
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                PasswordHash = reader.GetString(2),
                FullName = reader.GetString(3),
                Role = reader.GetString(4),  // "role" в кавычках в SQL
                IsActive = reader.GetBoolean(5),
                CreatedAt = reader.GetDateTime(6),
                LastLogin = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            };
        }
        return null;
    }

    public void UpdateLastLogin(int userId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(
            "UPDATE users SET last_login = NOW() WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", userId);
        cmd.ExecuteNonQuery();
    }

    // ==================== ТОВАРЫ ====================

    public List<Product> GetAllProducts()
    {
        var products = new List<Product>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT p.id, p.name, p.article, p.barcode, p.category_id, c.name as category_name,
                   p.supplier_id, s.name as supplier_name, p.price, p.quantity, 
                   p.min_quantity, p.unit, p.description, p.created_at, p.updated_at
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id
            LEFT JOIN suppliers s ON p.supplier_id = s.id
            ORDER BY p.name", conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            products.Add(new Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Article = reader.IsDBNull(2) ? null : reader.GetString(2),
                Barcode = reader.IsDBNull(3) ? null : reader.GetString(3),
                CategoryId = reader.GetInt32(4),
                CategoryName = reader.IsDBNull(5) ? "" : reader.GetString(5),
                SupplierId = reader.GetInt32(6),
                SupplierName = reader.IsDBNull(7) ? "" : reader.GetString(7),
                Price = reader.GetDecimal(8),
                Quantity = reader.GetInt32(9),
                MinQuantity = reader.GetInt32(10),
                Unit = reader.GetString(11),
                Description = reader.IsDBNull(12) ? null : reader.GetString(12),
                CreatedAt = reader.GetDateTime(13),
                UpdatedAt = reader.GetDateTime(14)
            });
        }
        return products;
    }

    public void AddProduct(Product product)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO products (name, article, barcode, category_id, supplier_id, price, 
                                 quantity, min_quantity, unit, description, created_at, updated_at)
            VALUES (@name, @article, @barcode, @category_id, @supplier_id, @price,
                    @quantity, @min_quantity, @unit, @description, NOW(), NOW())", conn);

        cmd.Parameters.AddWithValue("@name", product.Name);
        cmd.Parameters.AddWithValue("@article", string.IsNullOrEmpty(product.Article) ? DBNull.Value : product.Article);
        cmd.Parameters.AddWithValue("@barcode", string.IsNullOrEmpty(product.Barcode) ? DBNull.Value : product.Barcode);
        cmd.Parameters.AddWithValue("@category_id", product.CategoryId);
        cmd.Parameters.AddWithValue("@supplier_id", product.SupplierId);
        cmd.Parameters.AddWithValue("@price", product.Price);
        cmd.Parameters.AddWithValue("@quantity", product.Quantity);
        cmd.Parameters.AddWithValue("@min_quantity", product.MinQuantity);
        cmd.Parameters.AddWithValue("@unit", product.Unit);
        cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(product.Description) ? DBNull.Value : product.Description);
        cmd.ExecuteNonQuery();
    }

    public void UpdateProduct(Product product)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            UPDATE products 
            SET name = @name, article = @article, barcode = @barcode, category_id = @category_id,
                supplier_id = @supplier_id, price = @price, quantity = @quantity,
                min_quantity = @min_quantity, unit = @unit, description = @description, updated_at = NOW()
            WHERE id = @id", conn);

        cmd.Parameters.AddWithValue("@id", product.Id);
        cmd.Parameters.AddWithValue("@name", product.Name);
        cmd.Parameters.AddWithValue("@article", string.IsNullOrEmpty(product.Article) ? DBNull.Value : product.Article);
        cmd.Parameters.AddWithValue("@barcode", string.IsNullOrEmpty(product.Barcode) ? DBNull.Value : product.Barcode);
        cmd.Parameters.AddWithValue("@category_id", product.CategoryId);
        cmd.Parameters.AddWithValue("@supplier_id", product.SupplierId);
        cmd.Parameters.AddWithValue("@price", product.Price);
        cmd.Parameters.AddWithValue("@quantity", product.Quantity);
        cmd.Parameters.AddWithValue("@min_quantity", product.MinQuantity);
        cmd.Parameters.AddWithValue("@unit", product.Unit);
        cmd.Parameters.AddWithValue("@description", string.IsNullOrEmpty(product.Description) ? DBNull.Value : product.Description);
        cmd.ExecuteNonQuery();
    }

    public void DeleteProduct(int productId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand("DELETE FROM products WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("@id", productId);
        cmd.ExecuteNonQuery();
    }

    // ==================== КАТЕГОРИИ ====================

    public List<Category> GetAllCategories()
    {
        var categories = new List<Category>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand("SELECT id, name, description FROM categories ORDER BY name", conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }
        return categories;
    }

    // ==================== ПОСТАВЩИКИ ====================

    public List<Supplier> GetAllSuppliers()
    {
        var suppliers = new List<Supplier>();
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand("SELECT id, name, contact_person, phone, email, address FROM suppliers ORDER BY name", conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            suppliers.Add(new Supplier
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ContactPerson = reader.IsDBNull(2) ? null : reader.GetString(2),
                Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                Address = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return suppliers;
    }

    // ==================== ОПЕРАЦИИ ====================

    public List<WarehouseOperation> GetAllOperations()
    {
        var operations = new List<WarehouseOperation>();
        using var conn = GetConnection();
        conn.Open();

        using var cmd = new NpgsqlCommand(@"
        SELECT wo.id, wo.operation_type, wo.product_id, p.name as product_name,
               wo.quantity, wo.unit_price, wo.reason, wo.user_id, u.full_name as user_name,
               wo.operation_date, wo.notes
        FROM warehouse_operations wo
        LEFT JOIN products p ON wo.product_id = p.id
        LEFT JOIN users u ON wo.user_id = u.id
        ORDER BY wo.operation_date DESC", conn);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            operations.Add(new WarehouseOperation
            {
                Id = reader.GetInt32(0),
                Type = (OperationType)reader.GetInt32(1),  // Теперь читаем напрямую как int
                ProductId = reader.GetInt32(2),
                ProductName = reader.GetString(3),
                Quantity = reader.GetInt32(4),
                UnitPrice = reader.GetDecimal(5),
                Reason = reader.IsDBNull(6) ? null : reader.GetString(6),
                UserId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                UserName = reader.IsDBNull(8) ? "" : reader.GetString(8),
                OperationDate = reader.GetDateTime(9),
                Notes = reader.IsDBNull(10) ? null : reader.GetString(10)
            });
        }
        return operations;
    }

    public void AddOperation(WarehouseOperation operation, int currentUserId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO warehouse_operations (operation_type, product_id, quantity, unit_price, 
                                                 reason, user_id, operation_date, notes)
                VALUES (@type, @product_id, @quantity, @unit_price, @reason, @user_id, NOW(), @notes)
                RETURNING id", conn, transaction);

            cmd.Parameters.AddWithValue("@type", (int)operation.Type);
            cmd.Parameters.AddWithValue("@product_id", operation.ProductId);
            cmd.Parameters.AddWithValue("@quantity", operation.Quantity);
            cmd.Parameters.AddWithValue("@unit_price", operation.UnitPrice);
            cmd.Parameters.AddWithValue("@reason", string.IsNullOrEmpty(operation.Reason) ? DBNull.Value : operation.Reason);
            cmd.Parameters.AddWithValue("@user_id", currentUserId);
            cmd.Parameters.AddWithValue("@notes", string.IsNullOrEmpty(operation.Notes) ? DBNull.Value : operation.Notes);
            operation.Id = (int)(cmd.ExecuteScalar() ?? 0);

            string updateQuery = operation.Type == OperationType.Incoming 
                ? "UPDATE products SET quantity = quantity + @qty, updated_at = NOW() WHERE id = @id"
                : "UPDATE products SET quantity = quantity - @qty, updated_at = NOW() WHERE id = @id";

            using var updateCmd = new NpgsqlCommand(updateQuery, conn, transaction);
            updateCmd.Parameters.AddWithValue("@qty", operation.Quantity);
            updateCmd.Parameters.AddWithValue("@id", operation.ProductId);
            updateCmd.ExecuteNonQuery();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ==================== СТАТИСТИКА ====================

    public DataTable GetLowStockProducts()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT p.name, p.quantity, p.min_quantity, c.name as category
            FROM products p
            LEFT JOIN categories c ON p.category_id = c.id
            WHERE p.quantity <= p.min_quantity
            ORDER BY p.quantity ASC", conn);

        var dt = new DataTable();
        dt.Load(cmd.ExecuteReader());
        return dt;
    }

    public DataTable GetOperationsByDateRange(DateTime from, DateTime to)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = new NpgsqlCommand(@"
            SELECT wo.operation_date, p.name as product, wo.quantity, wo.unit_price, 
                   wo.operation_type, u.full_name as user
            FROM warehouse_operations wo
            LEFT JOIN products p ON wo.product_id = p.id
            LEFT JOIN users u ON wo.user_id = u.id
            WHERE wo.operation_date BETWEEN @from AND @to
            ORDER BY wo.operation_date DESC", conn);

        cmd.Parameters.AddWithValue("@from", from);
        cmd.Parameters.AddWithValue("@to", to);
        var dt = new DataTable();
        dt.Load(cmd.ExecuteReader());
        return dt;
    }
}