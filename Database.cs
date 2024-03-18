using System.Data;
using Microsoft.Data.Sqlite;


public class DbObject {
    private SqliteConnection connection;
    public DbObject() {
        connection = new("Data Source=LibraryCatalog.db");

        CreateTables();
    }

    private string ReturnTitleWithAcronym(string title) {//Skicka in en titel sträng som kommer och få tillbaka titeln + en förkortning
        string abbreviation = "";

        for(int i = 0; i < title.Length; i++) {
            if(i == 0) {//första tecknet från strängen
                abbreviation += title[i];
            } else if (char.IsNumber(title[i])) {//om ett tecken är en siffra
                abbreviation += title[i];
            } else if (title[i-1] == ' ' && title[i] != ' ') {//om tecknet före var ett mellanslag
                abbreviation += title[i];
            }
        }
        return $"{title} ({abbreviation})";
    }

    private LibraryItem NewLibraryItemFromReader(SqliteDataReader reader) {//returnerar läser datan från en SqliteDataReader och konverterar det till ett LibraryItem
        return new LibraryItem((long)reader["id"], 
        (long)reader["categoryId"], 
        reader["title"] != DBNull.Value ? ReturnTitleWithAcronym((string?)reader["title"]!): null, 
        (string)reader["type"], 
        reader["author"] != DBNull.Value ? (string?)reader["author"] : null, 
        reader["pages"] != DBNull.Value ? (long?)reader["pages"] : null, 
        reader["runTimeMinutes"] != DBNull.Value ? (long?)reader["runTimeMinutes"] : null, 
        (long)reader["isBorrowable"] == 1 ? true : false, 
        reader["borrower"] != DBNull.Value ? (string?)reader["borrower"] : null, 
        reader["borrowDate"] != DBNull.Value ? DateTime.UnixEpoch.AddSeconds((long)reader["borrowDate"]).ToString("dd/MM/yyyy") : null);
    }

    private Category NewCategoryFromReader(SqliteDataReader reader) {//returnerar läser datan från en SqliteDataReader och konverterar det till en Category
        return new Category((long)reader["id"], (string)reader["name"]);
    }

    private List<Category> ListOfCategoriesFromReader(SqliteDataReader reader) {//returnerar en lista av Category från en SqliteDataReader
        var entries = new List<Category>();

        while(reader.Read()) {
            entries.Add(NewCategoryFromReader(reader));
        }
        return entries;
    }

    private List<LibraryItem> ListOfLibraryItemsFromReader(SqliteDataReader reader) {//returnerar en lista av LibraryItems från en SqliteDataReader
        var entries = new List<LibraryItem>();
        //public record LibraryItem(int Id, int CategoryId, string Title, string Type, string Author, int Pages, int RunTimeMinutes, bool IsBorrowable, string Borrower, DateTime Date);
        while(reader.Read()) {
            entries.Add(NewLibraryItemFromReader(reader));
        }
        return entries;
    }

    private Category SingleCategoryFromReader(SqliteDataReader reader) {//returnerar en Category från en SqliteDataReader
        reader.Read();
        return NewCategoryFromReader(reader);
    }

    private LibraryItem SingleLibraryItemFromReader(SqliteDataReader reader) {//returnerar ett LibraryItem från en SqliteDataReader
        reader.Read();
        return NewLibraryItemFromReader(reader);
    }

    private void CreateTables() {//kör queries som skapar båda databas "tabellerna"
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
                borrowDate INTEGER
            )
        ";
        createLibraryItemsTableCommand.ExecuteReader();
        //har inte tid, orkar inte bygga om, men hade velat ha en separat bool för isBorrowable och isCurrentlyBorrowed
        //hade även velat ha borrowDate och borrowedUntilDate så man vet när den blev lånad, och hur när den ska tillbaka.
        //bestämde mig att ha borrowDate som när den ska returneras eftersom det känns mest relevant när man vill leta upp information om ett libraryItem
        
    }

    public void InsertCategory(string categoryName) {//tar emot en sträng categoryName och skapar en ny category med det namnet ifall det inte redan finns
        try {
            connection.Open();

            var listOfCategoryResults = SelectCategories(); //används för att kolla ifall namnet redan finns oavsett case
            if(listOfCategoryResults.Exists(x => x.Name.Equals(categoryName, StringComparison.CurrentCultureIgnoreCase))) {
                throw new Exception($"Category named {categoryName} already exist");
            }

            var insertCategoryCommand = connection.CreateCommand();
            insertCategoryCommand.CommandText = 
            @"
                INSERT INTO category_table VALUES (NULL, $Name);
            ";
            insertCategoryCommand.Parameters.AddWithValue("$Name", categoryName);
            insertCategoryCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            if(e.ErrorCode == -2147467259) {//Lämnar kvar denna här, men är osäker på om jag bör köra den övre metoden eller vänta på att databasen kör queryn och märker av problemet själv
                throw new Exception("Category already exists");
            }
            throw;
        }
    }

    public List<Category> SelectCategories() {//kör en query som får tillbaka alla categories i databasen
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

    public Category SelectCategoryById(int id) {//kör en query som får tillbaka en category med det id
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

            if(queryResult.HasRows) {//skickar tillbaka resultaten ifall det finns
                return SingleCategoryFromReader(queryResult);
            } else {//annars skickar vi tillbaka en exception som varnar att det inte finns(kan skötas i frontend också)
                throw new Exception($"No category with id: {id}");
            }
            
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void UpdateCategory(Category category) {//uppdaterar en category, i detta fallet kan egentligen endast namn ändras
        try {
            connection.Open();

            var listOfCategoryResults = SelectCategories();
    
            if(!listOfCategoryResults.Exists(x => x.Id == category.Id)) { //kollar så att id faktiskt finns först
                throw new Exception($"Category with id {category.Id} does not exist");
            }
            if(listOfCategoryResults.Exists(x => x.Name.Equals(category.Name, StringComparison.CurrentCultureIgnoreCase))) { //används för att kolla ifall namnet redan finns oavsett case
                throw new Exception($"Category named {category.Name} already exist");
            }

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

    public void DeleteCategoryById(int id) {//tar bort en category med id
        try {
            connection.Open();

            var listOfLibraryItemsWithCategoryId = SelectLibraryItemsByCategoryId(id); //ser till att categorin är tom först
            if(listOfLibraryItemsWithCategoryId.Count > 0)
                throw new Exception("Category is not empty, please empty the category before deleting");

            var deleteCategoryById = connection.CreateCommand();
            deleteCategoryById.CommandText =
            @"
                DELETE FROM category_table
                WHERE id = $id;
            ";
            deleteCategoryById.Parameters.AddWithValue("$id", id);

            var reader = deleteCategoryById.ExecuteReader();

            if(reader.RecordsAffected == 0)//skickar tillbaka ett exception ifall queryn inte gjorde något (vill varna användaren ifall dom kanske skrev fel id nummer)
                throw new Exception($"Category with id {id} does not exist");

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
    
    public List<LibraryItem> SelectLibraryItems(bool hasSortTypeValue, string? sortType) {//skickar en query som antigen sorterar efter category, type, eller titel, i bokstavsordning
        try {
            connection.Open();
            var selectLibraryItems = connection.CreateCommand();
            if(hasSortTypeValue) {//kollar ifall querysträngen är tom
                if(sortType == "category") //rättar till sortType strängen eftersom category columnen i libraryitems_table heter categoryId
                    sortType = sortType + "Id";

                selectLibraryItems.CommandText =
                @"
                    SELECT * FROM libraryitems_table
                    ORDER BY "+ sortType +" ASC; ";
            } else { //sortera efter titel om querysträngen är tom
                selectLibraryItems.CommandText =
                @"
                    SELECT * FROM libraryitems_table
                    ORDER BY title ASC;
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

    public List<LibraryItem> SelectLibraryItemsByCategoryId(int id) {//skickar en query som får tillbaka alla libraryitems som matchar med categoryId == id
        try {
            connection.Open();
            var selectLibraryItemsByCategoryId = connection.CreateCommand();
            selectLibraryItemsByCategoryId.CommandText =
            @"
                SELECT * FROM libraryitems_table
                WHERE categoryId = $categoryId; 
            ";
            selectLibraryItemsByCategoryId.Parameters.AddWithValue("$categoryId", id);
            var queryResult = selectLibraryItemsByCategoryId.ExecuteReader();

            return ListOfLibraryItemsFromReader(queryResult);

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public LibraryItem SelectLibraryItemById(int id) {//skickar en query som får tillbaka ett libraryItem där id matchar
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

            if(queryResult.HasRows) {//skickar tillbaka en Exception som varnar ifall föremålet inte finns(kan argumentera för att frontend borde sköta detta, men eftersom jag inte gör en frontend i denna uppgiften så ville jag lägga till detta)
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

    public void InsertLibraryItem(LibraryItemInput libraryItemInput) {//skickar in ett libraryItem till libraryitems_table
        try {
            connection.Open();
            var insertLibraryItemCommand = connection.CreateCommand();
            //long Id, int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, bool IsBorrowable, string? Borrower, DateTime? BorrowDate

            insertLibraryItemCommand.CommandText = 
            @"
                INSERT INTO libraryitems_table VALUES (NULL, $CategoryId, $Title, $Type, $Author, $Pages, $RunTimeMinutes, $IsBorrowable, NULL, NULL);
            ";
            insertLibraryItemCommand.Parameters.AddWithValue("$CategoryId", libraryItemInput.CategoryId);
            insertLibraryItemCommand.Parameters.AddWithValue("$Title", libraryItemInput.Title != null ? libraryItemInput.Title.Trim() : DBNull.Value); //vissa fält får vara tomma
            insertLibraryItemCommand.Parameters.AddWithValue("$Type", libraryItemInput.Type.ToUpper());
            insertLibraryItemCommand.Parameters.AddWithValue("$Author", libraryItemInput.Author != null ? libraryItemInput.Author.Trim() : DBNull.Value);
            insertLibraryItemCommand.Parameters.AddWithValue("$Pages", libraryItemInput.Pages != null ? libraryItemInput.Pages : DBNull.Value);
            insertLibraryItemCommand.Parameters.AddWithValue("$RunTimeMinutes", libraryItemInput.RunTimeMinutes != null ? libraryItemInput.RunTimeMinutes : DBNull.Value);

            Enum.TryParse(libraryItemInput.Type.ToUpper(), out LibraryItemType libraryItemType);//får fram libraryItem.Type
            insertLibraryItemCommand.Parameters.AddWithValue("$IsBorrowable", libraryItemType != LibraryItemType.REFERENCEBOOK ? 1 : 0);//kollar ifall den är av typ REFERENCE book, och sätter isBorrowable därefter

            insertLibraryItemCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void BorrowLibraryItem(int id, string name) {//skickar en query lånar ett libraryitem genom att sätta ett namn på borrower och sätta ett date = 2 veckor fram
        try {
            connection.Open();
            var borrowLibraryItemCommand = connection.CreateCommand();

            var singleLibraryItem = SelectLibraryItemById(id);
            if(!singleLibraryItem.IsBorrowable)//kollar så att library item faktiskt går att låna
                throw new Exception("Item type is not borrowable");
            if(singleLibraryItem.BorrowDate != null)
                throw new Exception("Item is currently borrowed by: " + singleLibraryItem.Borrower);


            var dueDate = DateTimeOffset.Now.AddDays(14);//saker får lånas i 2 veckor
            var dueDateUnix = dueDate.ToUnixTimeSeconds();

            borrowLibraryItemCommand.CommandText = 
            @"
                UPDATE libraryitems_table
                SET borrower = $name, borrowDate = $borrowDate
                WHERE id = $id
            ";

            borrowLibraryItemCommand.Parameters.AddWithValue("$name", name.Trim());
            borrowLibraryItemCommand.Parameters.AddWithValue("$borrowDate", dueDateUnix);
            borrowLibraryItemCommand.Parameters.AddWithValue("$id", id);

            borrowLibraryItemCommand.ExecuteReader();

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void ReturnLibraryItem(int id) {//skickar en query som nollställer borrower och date då föremålet har returnerats
        try {
            connection.Open();
            var borrowLibraryItemCommand = connection.CreateCommand();

            var singleLibraryItem = SelectLibraryItemById(id);

            if(singleLibraryItem.BorrowDate == null && singleLibraryItem.Borrower == null)//throwar tidigt ifall föremålet inte är lånat
                throw new Exception("Item is not currently borrowed by anyone");

            borrowLibraryItemCommand.CommandText = 
            @"
                UPDATE libraryitems_table
                SET borrower = NULL, borrowDate = NULL
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

            var singleLibraryItem = SelectLibraryItemById(id);

            if(singleLibraryItem.Borrower != null) { //vill inte att man ska kunna uppdatera ifall libraryItem är utlånat
                throw new Exception("Cant update item that is currently borrowed");
            }

            updateLibraryItemCommand.CommandText = 
            @"
                UPDATE libraryitems_table 
                SET categoryId = $CategoryId, title = $Title, type = $Type, author = $Author, pages = $Pages, runTimeMinutes = $RunTimeMinutes, isBorrowable = $IsBorrowable, borrower = NULL, borrowDate = NULL
                WHERE id = $id;
            ";
            updateLibraryItemCommand.Parameters.AddWithValue("$CategoryId", libraryItemInput.CategoryId);
            updateLibraryItemCommand.Parameters.AddWithValue("$Title", libraryItemInput.Title != null ? libraryItemInput.Title.Trim() : DBNull.Value);//vissa fält får vara tomma
            updateLibraryItemCommand.Parameters.AddWithValue("$Type", libraryItemInput.Type.ToUpper());
            updateLibraryItemCommand.Parameters.AddWithValue("$Author", libraryItemInput.Author != null ? libraryItemInput.Author.Trim() : DBNull.Value);
            updateLibraryItemCommand.Parameters.AddWithValue("$Pages", libraryItemInput.Pages != null ? libraryItemInput.Pages : DBNull.Value);
            updateLibraryItemCommand.Parameters.AddWithValue("$RunTimeMinutes", libraryItemInput.RunTimeMinutes != null ? libraryItemInput.RunTimeMinutes : DBNull.Value);

            Enum.TryParse(libraryItemInput.Type.ToUpper(), out LibraryItemType libraryItemType);//får fram libraryItem.Type
            updateLibraryItemCommand.Parameters.AddWithValue("$IsBorrowable", libraryItemType != LibraryItemType.REFERENCEBOOK ? 1 : 0);//kollar ifall den är av typ REFERENCE book, och sätter isBorrowable därefter
            updateLibraryItemCommand.Parameters.AddWithValue("$id", id);

            updateLibraryItemCommand.ExecuteReader();
        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }

    public void DeleteLibraryItemById(int id) {//tar bort ett libraryItem med id som matchar
        try {
            connection.Open();

            var deleteLibraryItemById = connection.CreateCommand();
            deleteLibraryItemById.CommandText =
            @"
                DELETE FROM libraryitems_table
                WHERE id = $id;
            ";
            deleteLibraryItemById.Parameters.AddWithValue("$id", id);
            var reader = deleteLibraryItemById.ExecuteReader();

            if(reader.RecordsAffected == 0)//ifall queryn inte tog bort något så vill jag varna användaren
                throw new Exception($"LibraryItem with id {id} does not exist");

        } catch (SqliteException e) {
            Console.WriteLine(e);
            Console.WriteLine(e.ErrorCode);
            connection.Dispose();
            throw;
        }
    }
}