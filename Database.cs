using Microsoft.Data.Sqlite;


public class DbObject {
    private SqliteConnection connection;
    public DbObject() {
        connection = new("Data Source=LibraryCatalog.db");

        CreateTable();
    }

    public void CreateTable() {
        connection.Open();
        var createTableCommand = connection.CreateCommand();
        createTableCommand.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS category_table(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE
            )
        ";

        createTableCommand.ExecuteReader();
    }

    public SqliteDataReader InsertCategory(string categoryName) {
        try {
            connection.Open();
            var insertIntoCategoryCommand = connection.CreateCommand();
            insertIntoCategoryCommand.CommandText = 
            @"
                INSERT INTO category_table VALUES (NULL, @Entry);
            ";
            insertIntoCategoryCommand.Parameters.AddWithValue("@Entry", categoryName);
            return insertIntoCategoryCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            if(e.ErrorCode == -2147467259) {
                throw new Exception("Category already exists");
            }
            throw;
        }
    }

    public SqliteDataReader SelectCategories() {
        try {
            connection.Open();
            var selectCategoriesCommand = connection.CreateCommand();
            selectCategoriesCommand.CommandText =
            @"
                SELECT * FROM category_table;
            ";
            return selectCategoriesCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public SqliteDataReader SelectCategoryById(int id) {
        try {
            connection.Open();
            var selectCategoryByIdCommand = connection.CreateCommand();
            selectCategoryByIdCommand.CommandText =
            @"
                SELECT * FROM category_table
                WHERE id = $id;
            ";
            selectCategoryByIdCommand.Parameters.AddWithValue("$id", id);
            return selectCategoryByIdCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
}