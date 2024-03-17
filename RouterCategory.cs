using Microsoft.AspNetCore.Http.HttpResults;

public static class CategoryRouteBuilder {
    public static RouteGroupBuilder MapCategoriesAPI(this RouteGroupBuilder group) {
        group.MapGet("/", (DbObject db) => {
            //Returns a list of all categories 
            var listOfCategoryResults = db.SelectCategories();

            return TypedResults.Ok(listOfCategoryResults);
        });

        group.MapGet("/{id}", Results<Ok<Category>, BadRequest<Dictionary<string, string[]>>> (HttpContext context, DbObject db, int id) => {
            //Returns a category by its id 
            try {
                Category singleCategory = db.SelectCategoryById(id);
                return TypedResults.Ok(singleCategory);

            } catch (Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "Category id: " + id, [e.Message] }
                };
                return TypedResults.BadRequest(errors);
            }
            
        });

        group.MapPost("/", Results<Created, BadRequest<Dictionary<string, string[]>>> (DbObject db, NameInput category) => {
            //Creates a new category (the category model should be passed in the request body)
            try {
                db.InsertCategory(category.Name.Trim());
                return TypedResults.Created("/categories");
            } catch(Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "CategoryName: " + category.Name, [e.Message] }
                };
                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var categoryNameArgument = context.GetArgument<NameInput>(1).Name;
            var errors = new Dictionary<string, string[]>();

            if(categoryNameArgument.Trim().Length < 1) {//kollar så att namnet inte är för kort aka ifall någon känner för att skicka in en sträng med endast spaces
                errors.Add(nameof(categoryNameArgument), ["Category name must not be empty"]);
            }
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapPut("/{id}", Results<Ok, BadRequest<Dictionary<string, string[]>>>(DbObject db, int id, NameInput categoryName) => {
            //Updates a category 
            Category category = new Category(id, categoryName.Name.Trim());
            try {
                db.UpdateCategory(category);
                return TypedResults.Ok();
            } catch(Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "Category id: " + id + " CategoryName: " + category.Name, [e.Message] }
                };
                return TypedResults.BadRequest(errors);
            }
        })
        .AddEndpointFilter(async (context, next) => {
            var categoryName = context.GetArgument<NameInput>(2).Name;
            var errors = new Dictionary<string, string[]>();

            if(categoryName.Trim().Length < 1) {//kollar namnets längd så det inte bara är fyllt med massa spaces
                errors.Add(nameof(categoryName), ["Field: CategoryName must not be empty"]);
            }
            if(errors.Count > 0) {
                return Results.ValidationProblem(errors);
            }

            return await next(context);
        });

        group.MapDelete("/{id}", Results<NoContent, BadRequest<Dictionary<string, string[]>>> (DbObject db, int id) => {
            //Deletes a category by its id 
            try {
                db.DeleteCategoryById(id);
                return TypedResults.NoContent();
            } catch (Exception e) {
                Console.WriteLine(e);
                Console.WriteLine(e.Message);
                var errors = new Dictionary<string, string[]>{
                    { "Category id: " + id, [e.Message] }
                };
                return TypedResults.BadRequest(errors);
            }
        });

        return group;
    }
}