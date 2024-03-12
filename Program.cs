using System.Collections.Specialized;
using Microsoft.AspNetCore.Http.HttpResults;

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

app.MapGet("/categories/{id}", Results<Ok<List<Category>>, NoContent, IResult> (DbObject db, int id) => {
    //Returns a category by its id 
    var listOfCategoryResults = db.SelectCategoryById(id);

    return listOfCategoryResults.Count > 0 ? TypedResults.Ok(listOfCategoryResults) : TypedResults.NoContent();
});

app.MapPost("/categories", Results<Created, BadRequest<Dictionary<string, string[]>>> (DbObject db, CategoryName category) => {
    //Creates a new category (the category model should be passed in the request body)
    
    try {
        db.InsertCategory(category.Name);
        return TypedResults.Created("/categories");
    } catch(Exception e) {
        var errors = new Dictionary<string, string[]>{
            { nameof(category)+"."+nameof(category.Name), [e.Message] }
        };
        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var categoryNameArgument = context.GetArgument<CategoryName>(1);
    var errors = new Dictionary<string, string[]>();

    if(categoryNameArgument.Name.Length < 1) {
        errors.Add(nameof(categoryNameArgument), ["Category name must not be empty"]);
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapPut("/categories/{id}", Results<Ok, BadRequest<Dictionary<string, string[]>>>(DbObject db, int id, CategoryName categoryName) => {
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
    var id = context.GetArgument<int>(1);
    var categoryName = context.GetArgument<CategoryName>(2);
    Category category = new Category(id, categoryName.Name);
    var db = context.GetArgument<DbObject>(0);
    var listOfCategoryResults = db.SelectCategories();

    var errors = new Dictionary<string, string[]>();
    
    if(!listOfCategoryResults.Exists(x => x.Id == category.Id)) {
        errors.Add(nameof(category.Id), [$"Category with id {category.Id} does not exist"]);
    }
    if(listOfCategoryResults.Exists(x => x.Name == category.Name)) {
        errors.Add(nameof(category.Name), [$"Category named {category.Name} already exist"]);
    }
    if(category.Name.Length < 1) {
        errors.Add(nameof(category), ["Category name must not be empty"]);
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/categories/{id}", Results<NoContent, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
    //Deletes a category by its id 
    try {
        db.DeleteCategoryById(id);
        return TypedResults.NoContent();
    } catch (Exception e) {
        var errors = new Dictionary<string, string[]>{
            { nameof(db), [e.Message] }
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
            { nameof(db), [e.Message] }
        };

        return TypedResults.BadRequest(errors);
    }
})
.AddEndpointFilter(async (context, next) => {
    var httpContext = context.GetArgument<HttpContext>(0);

    var hasSortValue = httpContext.Request.Query.TryGetValue("sort", out var sortValue);
    List<string> sortTypes = ["type", "category"];

    var errors = new Dictionary<string, string[]>();

    if(hasSortValue) {
        if(!sortTypes.Exists(x => x.Equals(sortValue.ToString(), StringComparison.CurrentCultureIgnoreCase))) {
            errors.Add(nameof(sortTypes), [$"Sort query doenst match available sorting types ['type', 'category']"]);
        }
    }
    if(errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapGet("/libraryitems/{id}", (int id) => {
    //Returns a specific library item by its id 
    throw new NotImplementedException();
});

app.MapPost("/libraryitems", (HttpContext context) => {
    //Creates a new library item. Depending on the type of item being created certain rules may apply.
    throw new NotImplementedException();
});

app.MapPost("/libraryitems/{id}/borrow", (int id) => {
    //Borrow a library item. The Borrower name should be passed in the request body.
    throw new NotImplementedException();
});

app.MapPost("/libraryitems/{id}/return", (int id) => {
    //Returns a borrowed library item. The item should now be able to be borrowed by another user.
    throw new NotImplementedException();
});

app.MapPut("/libraryitems/{id}", (int id) => {
    //Updates an existing library item. Depending on the type of item certain rules may apply. 
    throw new NotImplementedException();
});

app.MapDelete("/libraryitems/{id}", (int id) => {
    //Deletes a library item by its id 
    throw new NotImplementedException();
});

app.Run();

public record CategoryName(string Name);

public record Category(long Id, string Name);

public record LibraryItem(long Id, int CategoryId, string? Title, string Type, string? Author, int? Pages, int? RunTimeMinutes, bool? IsBorrowable, string? Borrower, DateTime? Date);
