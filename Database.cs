using System.Data;
using Microsoft.Data.Sqlite;


public class DbObject {
    private SqliteConnection connection;
    public DbObject() {
        connection = new("Data Source=LibraryCatalog.db");

        CreateTables();
    }

    private string ReturnTitleWithAcronym(string title) {
        string abbreviation = "";

        for(int i = 0; i < title.Length; i++) {
            if(i == 0) {
                abbreviation += title[i];
            } else if (char.IsNumber(title[i])) {
                abbreviation += title[i];
            } else if (title[i-1] == ' ') {
                abbreviation += title[i];
            }
        }
        return $"{title} ({abbreviation})";
    }

    private LibraryItem NewLibraryItemFromReader(SqliteDataReader reader) {
        

        return new LibraryItem((long)reader["id"], 
        (long)reader["categoryId"], 
        reader["title"] != DBNull.Value ? ReturnTitleWithAcronym((string?)reader["title"]!): null, 
        (string)reader["type"], 
        reader["author"] != DBNull.Value ? (string?)reader["author"] : null, 
        reader["pages"] != DBNull.Value ? (long?)reader["pages"] : null, 
        reader["runTimeMinutes"] != DBNull.Value ? (long?)reader["runTimeMinutes"] : null, 
        (long)reader["isBorrowable"] == 1 ? true : false, 
        reader["borrower"] != DBNull.Value ? (string?)reader["borrower"] : null, 
        reader["date"] != DBNull.Value ? DateTime.UnixEpoch.AddSeconds((double)reader["date"]).ToString("dd/MM/yyyy") : null);
    }

    private Category NewCategoryFromReader(SqliteDataReader reader) {
        return new Category((long)reader["id"], (string)reader["name"]);
    }

    private List<Category> ListOfCategoriesFromReader(SqliteDataReader reader) {
        var entries = new List<Category>();

        while(reader.Read()) {
            entries.Add(NewCategoryFromReader(reader));
        }
        return entries;
    }

    private List<LibraryItem> ListOfLibraryItemsFromReader(SqliteDataReader reader) {
        var entries = new List<LibraryItem>();
        //public record LibraryItem(int Id, int CategoryId, string Title, string Type, string Author, int Pages, int RunTimeMinutes, bool IsBorrowable, string Borrower, DateTime Date);
        while(reader.Read()) {
            entries.Add(NewLibraryItemFromReader(reader));
        }
        return entries;
    }

    private Category SingleCategoryFromReader(SqliteDataReader reader) {
        reader.Read();
        return NewCategoryFromReader(reader);
    }

    private LibraryItem SingleLibraryItemFromReader(SqliteDataReader reader) {
        reader.Read();
        return NewLibraryItemFromReader(reader);
    }

    private void CreateTables() {
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
                date INTEGER
            )
        ";
        createLibraryItemsTableCommand.ExecuteReader();
        //public record LibraryItem(int Id, int CategoryId, string Title, string Type, string Author, int Pages, int RunTimeMinutes, bool IsBorrowable, string Borrower, DateTime Date);
    }

    public void InsertCategory(string categoryName) {
        try {
            connection.Open();
            var insertCategoryCommand = connection.CreateCommand();
            insertCategoryCommand.CommandText = 
            @"
                INSERT INTO category_table VALUES (NULL, $Name);
            ";
            insertCategoryCommand.Parameters.AddWithValue("$Name", categoryName.Trim());
            insertCategoryCommand.ExecuteReader();
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

    public Category SelectCategoryById(int id) {
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

            if(queryResult.HasRows) {
                return SingleCategoryFromReader(queryResult);
            } else {
                throw new Exception("No results");
            }

            
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

            var listOfCategoryResults = SelectCategories();
            var trimmedName = category.Name.Trim();
    
            if(!listOfCategoryResults.Exists(x => x.Id == category.Id)) {
                throw new Exception($"Category with id {category.Id} does not exist");
            }
            if(listOfCategoryResults.Exists(x => x.Name == trimmedName)) {
                throw new Exception($"Category named {trimmedName} already exist");
            }

            var updateCategoryCommand = connection.CreateCommand();
            updateCategoryCommand.CommandText =
            @"
                UPDATE category_table
                SET name = $name
                WHERE id = $id;
            ";
            updateCategoryCommand.Parameters.AddWithValue("$name", trimmedName);
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

            var listOfLibraryItemsWithCategoryId = SelectLibraryItemsByCategoryId(id);

            if(listOfLibraryItemsWithCategoryId.Count > 0)
                throw new Exception("Category is not empty");

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
            var selectLibraryItems = connection.CreateCommand();
            if(sortType != null) {
                if(sortType == "category")
                    sortType = sortType + "Id";
                selectLibraryItems.CommandText =
                @"
                    SELECT * FROM libraryitems_table
                    ORDER BY "+ sortType +" ASC; ";
                Console.WriteLine("\n"+ sortType);
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

    public List<LibraryItem> SelectLibraryItemsByCategoryId(int categoryId) {
        try {
            connection.Open();
            var selectLibraryItemsByCategoryId = connection.CreateCommand();
            selectLibraryItemsByCategoryId.CommandText =
            @"
                SELECT * FROM libraryitems_table
                WHERE categoryId = $categoryId; 
            ";
            
            var queryResult = selectLibraryItemsByCategoryId.ExecuteReader();
            return ListOfLibraryItemsFromReader(queryResult);

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public LibraryItem SelectLibraryItemById(int id) {
        try {
            connection.Open();

            var selectLibraryItemById = connection.CreateCommand();
            selectLibraryItemById.CommandText = 
            @"
                SELECT * FROM libraryitems_table
                WHERE id = $id;
            ";
            selectLibraryItemById.Parameters.AddWithValue("$id", id);

            var queryResult = selectLibraryItemById.ExecuteReader();

            if(queryResult.HasRows) {
                return SingleLibraryItemFromReader(queryResult);
            } else {
                throw new Exception("No results for id: " + id);
            }


        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void InsertLibraryItem(LibraryItemInput libraryItemInput) {
        try {
            connection.Open();
            var insertLibraryItemCommand = connection.CreateCommand();
            //long Id, int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, bool IsBorrowable, string? Borrower, DateTime? BorrowDate

            insertLibraryItemCommand.CommandText = 
            @"
                INSERT INTO libraryitems_table VALUES (NULL, $CategoryId, $Title, $Type, $Author, $Pages, $RunTimeMinutes, $IsBorrowable, NULL, NULL);
            ";
            insertLibraryItemCommand.Parameters.AddWithValue("$CategoryId", libraryItemInput.CategoryId);
            insertLibraryItemCommand.Parameters.AddWithValue("$Title", libraryItemInput.Title != null ? libraryItemInput.Title.Trim() : DBNull.Value);
            insertLibraryItemCommand.Parameters.AddWithValue("$Type", libraryItemInput.Type);
            insertLibraryItemCommand.Parameters.AddWithValue("$Author", libraryItemInput.Author != null ? libraryItemInput.Author.Trim() : DBNull.Value);
            insertLibraryItemCommand.Parameters.AddWithValue("$Pages", libraryItemInput.Pages != null ? libraryItemInput.Pages : DBNull.Value);
            insertLibraryItemCommand.Parameters.AddWithValue("$RunTimeMinutes", libraryItemInput.RunTimeMinutes != null ? libraryItemInput.RunTimeMinutes : DBNull.Value);

            Enum.TryParse(libraryItemInput.Type.ToUpper(), out LibraryItemType libraryItemType);
            insertLibraryItemCommand.Parameters.AddWithValue("$IsBorrowable", libraryItemType != LibraryItemType.REFERENCEBOOK ? 1 : 0);

            insertLibraryItemCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void BorrowLibraryItem(int id, string name) {
        try {
            connection.Open();
            var borrowLibraryItemCommand = connection.CreateCommand();

            var singleLibraryitem = SelectLibraryItemById(id);
            if(!singleLibraryitem.IsBorrowable)
                throw new Exception("Item type is not borrowable");
            if(singleLibraryitem.BorrowDate != null)
                throw new Exception("Item is currently borrowed by: " + singleLibraryitem.Borrower);


            var dueDate = DateTimeOffset.Now.AddDays(14);
            var dueDateUnix = dueDate.ToUnixTimeSeconds();

            borrowLibraryItemCommand.CommandText = 
            @"
                UPDATE libraryitems_table
                SET borrower = $name, date = $date
                WHERE id = $id
            ";

            borrowLibraryItemCommand.Parameters.AddWithValue("$name", name.Trim());
            borrowLibraryItemCommand.Parameters.AddWithValue("$date", dueDateUnix);
            borrowLibraryItemCommand.Parameters.AddWithValue("$id", id);

            borrowLibraryItemCommand.ExecuteReader();

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void ReturnLibraryItem(int id) {
        try {
            connection.Open();
            var borrowLibraryItemCommand = connection.CreateCommand();

            var singleLibraryitem = SelectLibraryItemById(id);
            if(!singleLibraryitem.IsBorrowable)
                throw new Exception("Item type is not borrowable");
            if(singleLibraryitem.BorrowDate == null)
                throw new Exception("Item is not currently borrowed by anyone");

            borrowLibraryItemCommand.CommandText = 
            @"
                UPDATE libraryitems_table
                SET borrower = NULL, date = NULL
                WHERE id = $id
            ";
            borrowLibraryItemCommand.Parameters.AddWithValue("$id", id);

            borrowLibraryItemCommand.ExecuteReader();

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
    
    public void UpdateLibraryItem(int id, LibraryItemInput libraryItemInput) {
        try {
            connection.Open();
            var updateLibraryItemCommand = connection.CreateCommand();
            //long Id, int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, bool IsBorrowable, string? Borrower, DateTime? BorrowDate

            var singleLibraryitem = SelectLibraryItemById(id);

            if(singleLibraryitem.Borrower != null) {
                throw new Exception("Cant update item that is currently borrowed");
            }
            if(libraryItemInput.Type != singleLibraryitem.Type) {

            }

            updateLibraryItemCommand.CommandText = 
            @"
                UPDATE libraryitems_table 
                SET categoryId = $CategoryId, title = $Title, type = $Type, author = $Author, pages = $Pages, runTimeMinutes = $RunTimeMinutes, isBorrowable = $IsBorrowable, borrower = NULL, date = NULL
                WHERE id = $id;
            ";
            updateLibraryItemCommand.Parameters.AddWithValue("$CategoryId", libraryItemInput.CategoryId);
            updateLibraryItemCommand.Parameters.AddWithValue("$Title", libraryItemInput.Title != null ? libraryItemInput.Title.Trim() : DBNull.Value);
            updateLibraryItemCommand.Parameters.AddWithValue("$Type", libraryItemInput.Type);
            updateLibraryItemCommand.Parameters.AddWithValue("$Author", libraryItemInput.Author != null ? libraryItemInput.Author.Trim() : DBNull.Value);
            updateLibraryItemCommand.Parameters.AddWithValue("$Pages", libraryItemInput.Pages != null ? libraryItemInput.Pages : DBNull.Value);
            updateLibraryItemCommand.Parameters.AddWithValue("$RunTimeMinutes", libraryItemInput.RunTimeMinutes != null ? libraryItemInput.RunTimeMinutes : DBNull.Value);

            Enum.TryParse(libraryItemInput.Type.ToUpper(), out LibraryItemType libraryItemType);
            updateLibraryItemCommand.Parameters.AddWithValue("$IsBorrowable", libraryItemType != LibraryItemType.REFERENCEBOOK ? 1 : 0);
            updateLibraryItemCommand.Parameters.AddWithValue("$id", id);

            updateLibraryItemCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void DeleteLibraryItemById(int id) {
        try {
            connection.Open();

            var deleteLibraryItemById = connection.CreateCommand();
            deleteLibraryItemById.CommandText =
            @"
                DELETE FROM libraryitems_table
                WHERE id = $id;
            ";
            deleteLibraryItemById.Parameters.AddWithValue("$id", id);
            deleteLibraryItemById.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
}