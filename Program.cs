using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DbObject>(new DbObject());

var app = builder.Build();

app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started.");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Finished.");
});

app.MapGet("/", () => "Hello World!");

//---------CATEGORIES ENDPOINTS---------

app.MapGet("/categories", (DbObject db) => {
    //Returns a list of all categories 
    var listOfCategoryResults = db.SelectCategories();

    return TypedResults.Ok(listOfCategoryResults);
});

app.MapGet("/libraryjson", (HttpContext context) => {
    return TypedResults.Ok(new LibraryItemInput(7, "fanta", "Book", "Mr.Tolk", 100, null, null, null));
});

app.MapGet("/categories/{id}", Results<Ok<Category>, BadRequest<Dictionary<string, string[]>>> (HttpContext context, DbObject db, int id) => {
    //Returns a category by its id 
    try {
        Category singleCategory = db.SelectCategoryById(id);
        return TypedResults.Ok(singleCategory);

    } catch (Exception e) {
        Console.WriteLine(e);
        var errors = new Dictionary<string, string[]>{
            { context.Request.GetDisplayUrl() , [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
    
});

app.MapPost("/categories", Results<Created, BadRequest<Dictionary<string, string[]>>> (DbObject db, NameInput category) => {
    //Creates a new category (the category model should be passed in the request body)

    try {
        db.InsertCategory(category.Name);
        return TypedResults.Created("/categories");
    } catch(Exception e) {
        Console.WriteLine(e);
        Console.WriteLine(e.Message);
        var errors = new Dictionary<string, string[]>{
            { nameof(category)+"."+nameof(category.Name), [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var categoryNameArgument = context.GetArgument<NameInput>(1).Name.Trim();
    var errors = new Dictionary<string, string[]>();

    if(categoryNameArgument.Length < 1) {
        errors.Add(nameof(categoryNameArgument), ["Category name must not be empty"]);
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPut("/categories/{id}", Results<Ok, BadRequest<Dictionary<string, string[]>>>(DbObject db, int id, NameInput categoryName) => {
    //Updates a category 
    Category category = new Category(id, categoryName.Name);
    try {
        db.UpdateCategory(category);
        return TypedResults.Ok();
    } catch(Exception e) {
        var errors = new Dictionary<string, string[]>{
            { nameof(category)+"."+nameof(category.Name), [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var categoryName = context.GetArgument<NameInput>(2).Name.Trim();
    var errors = new Dictionary<string, string[]>();

    if(categoryName.Length < 1) {
        errors.Add(nameof(categoryName), ["Field: CategoryName must not be empty"]);
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/categories/{id}", Results<NoContent, BadRequest<Dictionary<string, string[]>>> (HttpContext context, DbObject db, int id) => {
    //Deletes a category by its id 
    try {
        db.DeleteCategoryById(id);
        return TypedResults.NoContent();
    } catch (Exception e) {
        var errors = new Dictionary<string, string[]>{
            { context.Request.GetDisplayUrl(), [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
});

//---------LIBRARYITEMS ENDPOINTS---------

app.MapGet("/libraryitems", Results<Ok<List<LibraryItem>>, BadRequest<Dictionary<string, string[]>>>(HttpContext context, DbObject db) => {
    //return all libraryitems with sorting
    var hasSortValue = context.Request.Query.TryGetValue("sort", out var sortValue);

    try {
        var listOfLibraryItems = db.SelectLibraryItems(sortValue);
        
        return TypedResults.Ok(listOfLibraryItems);
    } catch (Exception e) {
        var errors = new Dictionary<string, string[]>{
            { nameof(e), [e.Message] }
        };

        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var httpContext = context.HttpContext;

    var hasSortValue = httpContext.Request.Query.TryGetValue("sort", out var sortValue);
    List<string> sortTypes = ["type", "category"];

    var errors = new Dictionary<string, string[]>();

    if(hasSortValue) {
        if(!sortTypes.Exists(x => x.Equals(sortValue.ToString(), StringComparison.CurrentCultureIgnoreCase))) {
            errors.Add(httpContext.Request.GetDisplayUrl(), ["Sort query doenst match available sorting types ['type', 'category']"]);
        }
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapGet("/libraryitems/{id}", Results<Ok<LibraryItem>, BadRequest<Dictionary<string, string[]>>> (HttpContext context, DbObject db, int id) => {
    //Returns a specific library item by its id 
    try {
        LibraryItem singleLibraryitem = db.SelectLibraryItemById(id);
        return TypedResults.Ok(singleLibraryitem);

    } catch (Exception e) {
        Console.WriteLine(e);
        var errors = new Dictionary<string, string[]>{
            { context.Request.GetDisplayUrl() , [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
});

app.MapPost("/libraryitems", Results<Created, BadRequest<Dictionary<string, string[]>>> (DbObject db, LibraryItemInput libraryItemInput) => {
    //Creates a new library item. Depending on the type of item being created certain rules may apply.
    //long Id, int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, bool IsBorrowable, string? Borrower, DateTime? BorrowDate
    try {
        db.InsertLibraryItem(libraryItemInput);
        return TypedResults.Created("/categories");
    } catch(Exception e) {
        Console.WriteLine(e);
        Console.WriteLine(e.Message);
        var errors = new Dictionary<string, string[]>{
            { nameof(libraryItemInput), [e.Message] }
        };
        
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var libraryItem = context.GetArgument<LibraryItemInput>(1);
    var errors = new Dictionary<string, string[]>();

    var validLibraryItem = Enum.TryParse(libraryItem.Type.ToUpper(), out LibraryItemType libraryItemType);

    if(!validLibraryItem) {
        errors.Add(nameof(libraryItem.Type), ["Field: Type is invalid " + libraryItem.Type]);
        return Results.ValidationProblem(errors);
    }

    if(libraryItemType == LibraryItemType.BOOK || libraryItemType == LibraryItemType.REFERENCEBOOK) {
        if(libraryItem.Title == null || libraryItem.Title.Trim().Length <= 0) {
            errors.Add(nameof(libraryItem.Title), ["Field: Title must not be empty for type: " + libraryItem.Type]);
        }
        if(libraryItem.Author == null || libraryItem.Author.Trim().Length <= 0) {
            errors.Add(nameof(libraryItem.Author), ["Field: Author must not be empty for type: " + libraryItem.Type]);
        }
        if(libraryItem.Pages == null || libraryItem.Pages <= 0) {
            errors.Add(nameof(libraryItem.Pages), ["Field: Pages is empty or invalid for type: " + libraryItem.Type]);
        }
    } else { //if Type is DVD or AudioBook
        if(libraryItem.Title == null || libraryItem.Title.Trim().Length <= 0) {
            errors.Add(nameof(libraryItem.Title), ["Field: Title must not be empty for type: " + libraryItem.Type]);
        }
        if(libraryItem.RunTimeMinutes == null || libraryItem.RunTimeMinutes <= 0) {
            errors.Add(nameof(libraryItem.RunTimeMinutes), ["Field: RunTimeMinutes is empty or invalid for type: " + libraryItem.Type]);
        }

    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPost("/libraryitems/{id}/borrow", Results<Ok, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id, [FromBody] NameInput name) => {
    //Borrow a library item. The Borrower name should be passed in the request body.
    try {
        db.BorrowLibraryItem(id, name.Name);
        return TypedResults.Ok();
    } catch (Exception e) {
        Console.WriteLine(e);
        Console.WriteLine(e.Message);
        var errors = new Dictionary<string, string[]>{
            { "Libraryitem id: " + id, [e.Message] }
        };
        
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var libraryItemId = context.GetArgument<int>(1);
    var errors = new Dictionary<string, string[]>();

    var borrowerName = context.GetArgument<NameInput>(2).Name.Trim();

    if(borrowerName.Length <= 0)
        errors.Add(nameof(borrowerName), ["Field: Name is empty"]);
    if(libraryItemId <= 0)
        errors.Add(nameof(libraryItemId), ["Field: Id is out of scope"]);
    
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPost("/libraryitems/{id}/return", Results<Ok, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
    //Returns a borrowed library item. The item should now be able to be borrowed by another user.
    try {
        db.ReturnLibraryItem(id);
        return TypedResults.Ok();
    } catch (Exception e) {
        Console.WriteLine(e);
        Console.WriteLine(e.Message);
        var errors = new Dictionary<string, string[]>{
            { "Libraryitem id: " + id, [e.Message] }
        };
        
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var libraryItemId = context.GetArgument<int>(1);
    var errors = new Dictionary<string, string[]>();

    if(libraryItemId <= 0)
            errors.Add(nameof(libraryItemId), ["Field: Id is out of scope"]);
    
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPut("/libraryitems/{id}", Results<Ok, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id, [FromBody]LibraryItemInput libraryItemInput) => {
    //Updates an existing library item. Depending on the type of item certain rules may apply. 
    try {
        db.UpdateLibraryItem(id, libraryItemInput);
        return TypedResults.Ok();
    } catch (Exception e) {
        Console.WriteLine(e);
        Console.WriteLine(e.Message);
        var errors = new Dictionary<string, string[]>{
            { "Libraryitem id: " + id, [e.Message] }
        };
        
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var libraryItem = context.GetArgument<LibraryItemInput>(2);
    var errors = new Dictionary<string, string[]>();

    var validLibraryItem = Enum.TryParse(libraryItem.Type.ToUpper(), out LibraryItemType libraryItemType);

    if(!validLibraryItem) {
        errors.Add(nameof(libraryItem.Type), ["Field: Type is invalid " + libraryItem.Type]);
        return Results.ValidationProblem(errors);
    }

    if(libraryItemType == LibraryItemType.BOOK || libraryItemType == LibraryItemType.REFERENCEBOOK) {
        if(libraryItem.Title == null || libraryItem.Title.Trim().Length <= 0) {
            errors.Add(nameof(libraryItem.Title), ["Field: Title must not be empty for type: " + libraryItem.Type]);
        }
        if(libraryItem.Author == null || libraryItem.Author.Trim().Length <= 0) {
            errors.Add(nameof(libraryItem.Author), ["Field: Author must not be empty for type: " + libraryItem.Type]);
        }
        if(libraryItem.Pages == null || libraryItem.Pages <= 0) {
            errors.Add(nameof(libraryItem.Pages), ["Field: Pages is empty or invalid for type: " + libraryItem.Type]);
        }
    } else { //if Type is DVD or AudioBook
        if(libraryItem.Title == null || libraryItem.Title.Trim().Length <= 0) {
            errors.Add(nameof(libraryItem.Title), ["Field: Title must not be empty for type: " + libraryItem.Type]);
        }
        if(libraryItem.RunTimeMinutes == null || libraryItem.RunTimeMinutes <= 0) {
            errors.Add(nameof(libraryItem.RunTimeMinutes), ["Field: RunTimeMinutes is empty or invalid for type: " + libraryItem.Type]);
        }

    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/libraryitems/{id}", Results<NoContent, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
    //Deletes a library item by its id 
    try {
        db.DeleteLibraryItemById(id);
        return TypedResults.NoContent();
    } catch (Exception e) {
        var errors = new Dictionary<string, string[]>{
            { nameof(db) , [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
});

app.Run();

public enum LibraryItemType {
    BOOK,
    REFERENCEBOOK,
    DVD,
    AUDIOBOOK
}

public record LibraryItemInput(int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, string? Borrower, string? BorrowDate);

public record NameInput(string Name);

public record Category(long Id, string Name);

public record LibraryItem(long Id, long CategoryId, string? Title, string Type, string? Author, long? Pages, long? RunTimeMinutes, bool IsBorrowable, string? Borrower, string? BorrowDate);
