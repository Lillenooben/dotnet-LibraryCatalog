using Microsoft.Data.Sqlite;


public class DbObject {
    private SqliteConnection connection;
    public DbObject() {
        connection = new("Data Source=LibraryCatalog.db");

        CreateTables();
    }

    public List<Category> ListOfCategoriesFromReader(SqliteDataReader reader){
        var entries = new List<Category>();

        while(reader.Read()) {
            entries.Add(new Category((long)reader["id"], (string)reader["name"]));
        }
        return entries;
    }

    public List<LibraryItem> ListOfLibraryItemsFromReader(SqliteDataReader reader){
        var entries = new List<LibraryItem>();
        //public record LibraryItem(int Id, int CategoryId, string Title, string Type, string Author, int Pages, int RunTimeMinutes, bool IsBorrowable, string Borrower, DateTime Date);
        while(reader.Read()) {
            entries.Add(new LibraryItem((long)reader["id"], (int)reader["categoryId"], (string?)reader["title"], (string)reader["type"], (string?)reader["author"], (int?)reader["pages"], (int?)reader["runTimeMinutes"], (bool?)reader["isBorrowable"], (string?)reader["borrower"], (DateTime?)reader["date"]));
        }
        return entries;
    }

    public void CreateTables() {
        connection.Open();
        var createCategoryTableCommand = connection.CreateCommand();
        createCategoryTableCommand.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS category_table(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE
            )
        ";
        createCategoryTableCommand.ExecuteReader();
        
        var createLibraryItemsTableCommand = connection.CreateCommand();
        createLibraryItemsTableCommand.CommandText =
        @"
            CREATE TABLE IF NOT EXISTS libraryitems_table(
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                categoryId INTEGER NOT NULL,
                title TEXT,
                type TEXT NOT NULL,
                author TEXT,
                pages INTEGER,
                runTimeMinutes INTEGER,
                isBorrowable INTEGER,
                borrower TEXT,
                date TEXT
            )
        ";
        createLibraryItemsTableCommand.ExecuteReader();
        //public record LibraryItem(int Id, int CategoryId, string Title, string Type, string Author, int Pages, int RunTimeMinutes, bool IsBorrowable, string Borrower, DateTime Date);
    }

    public void InsertCategory(string categoryName) {
        try {
            connection.Open();
            var insertIntoCategoryCommand = connection.CreateCommand();
            insertIntoCategoryCommand.CommandText = 
            @"
                INSERT INTO category_table VALUES (NULL, @Entry);
            ";
            insertIntoCategoryCommand.Parameters.AddWithValue("@Entry", categoryName);
            insertIntoCategoryCommand.ExecuteReader();
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

    public List<Category> SelectCategories() {
        try {
            connection.Open();
            var selectCategoriesCommand = connection.CreateCommand();
            selectCategoriesCommand.CommandText =
            @"
                SELECT * FROM category_table;
            ";
            var queryResult = selectCategoriesCommand.ExecuteReader();
            return ListOfCategoriesFromReader(queryResult);

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public List<Category> SelectCategoryById(int id) {
        try {
            connection.Open();
            var selectCategoryByIdCommand = connection.CreateCommand();
            selectCategoryByIdCommand.CommandText =
            @"
                SELECT * FROM category_table
                WHERE id = $id;
            ";
            selectCategoryByIdCommand.Parameters.AddWithValue("$id", id);
            var queryResult = selectCategoryByIdCommand.ExecuteReader();

            return ListOfCategoriesFromReader(queryResult);
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void UpdateCategory(Category category) {
        try {
            connection.Open();

            var updateCategoryCommand = connection.CreateCommand();
            updateCategoryCommand.CommandText =
            @"
                UPDATE category_table
                SET name = $name
                WHERE id = $id;
            ";
            updateCategoryCommand.Parameters.AddWithValue("$name", category.Name);
            updateCategoryCommand.Parameters.AddWithValue("$id", category.Id);
            updateCategoryCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void DeleteCategoryById(int id) {
        try {
            connection.Open();

            var deleteCategoryById = connection.CreateCommand();
            deleteCategoryById.CommandText =
            @"
                DELETE FROM category_table
                WHERE id = $id;
            ";
            deleteCategoryById.Parameters.AddWithValue("$id", id);
            deleteCategoryById.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
    
    public List<LibraryItem> SelectLibraryItems(string? sortType) {
        try {
            connection.Open();

            var entries = new List<LibraryItem>();
            var selectLibraryItems = connection.CreateCommand();
            if(sortType != null) {
                selectLibraryItems.CommandText =
                @"
                    SELECT * FROM libraryitems_table
                    ORDER BY @sortType ASC;
                ";
                selectLibraryItems.Parameters.AddWithValue("@sortType", sortType);
            } else {
                selectLibraryItems.CommandText =
                @"
                    SELECT * FROM libraryitems_table;
                ";
            }
            var queryResult = selectLibraryItems.ExecuteReader();
            return ListOfLibraryItemsFromReader(queryResult);

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
}