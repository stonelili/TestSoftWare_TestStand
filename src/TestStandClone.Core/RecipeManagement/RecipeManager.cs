using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.RecipeManagement
{
    public enum RecipeParameterType { String, Integer, Double, Boolean, Enum, DateTime, FilePath }
    public enum RecipeStatus { Draft, Review, Approved, Active, Obsolete, Archived }

    public class RecipeParameter
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RecipeParameterType Type { get; set; } = RecipeParameterType.String;
        public object? Value { get; set; }
        public object? DefaultValue { get; set; }
        public object? MinValue { get; set; }
        public object? MaxValue { get; set; }
        public List<object> AllowedValues { get; set; } = new();
        public string Unit { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
    }

    public class Recipe
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0.0";
        public RecipeStatus Status { get; set; } = RecipeStatus.Draft;
        public string ProductId { get; set; } = string.Empty;
        public List<RecipeParameter> Parameters { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        public object? GetParameterValue(string name) =>
            Parameters.FirstOrDefault(p => p.Name == name)?.Value;

        public void SetParameterValue(string name, object value)
        {
            var param = Parameters.FirstOrDefault(p => p.Name == name);
            if (param != null) { param.Value = value; ModifiedAt = DateTime.Now; }
        }
    }

    public class RecipeValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new();
    }

    public class RecipeManager
    {
        private static readonly Lazy<RecipeManager> _instance = new(() => new RecipeManager());
        public static RecipeManager Instance => _instance.Value;
        private readonly Dictionary<string, Recipe> _recipes = new();
        private readonly object _lock = new();

        public event EventHandler<Recipe>? RecipeActivated;

        private RecipeManager() { }

        public Recipe CreateRecipe(string name, string productId)
        {
            var recipe = new Recipe { Name = name, ProductId = productId };
            lock (_lock) { _recipes[recipe.Id] = recipe; }
            return recipe;
        }

        public Recipe? GetRecipe(string id)
        {
            lock (_lock) { return _recipes.TryGetValue(id, out var r) ? r : null; }
        }

        public Recipe? GetActiveRecipeForProduct(string productId)
        {
            lock (_lock)
            {
                return _recipes.Values.FirstOrDefault(r => 
                    r.ProductId == productId && r.Status == RecipeStatus.Active);
            }
        }

        public RecipeValidationResult ValidateRecipe(Recipe recipe)
        {
            var result = new RecipeValidationResult();
            foreach (var param in recipe.Parameters)
            {
                if (param.IsRequired && param.Value == null && param.DefaultValue == null)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Required parameter '{param.Name}' is missing");
                }
            }
            return result;
        }

        public void ActivateRecipe(string id)
        {
            lock (_lock)
            {
                if (_recipes.TryGetValue(id, out var recipe))
                {
                    foreach (var r in _recipes.Values.Where(r => 
                        r.ProductId == recipe.ProductId && r.Status == RecipeStatus.Active))
                        r.Status = RecipeStatus.Approved;
                    recipe.Status = RecipeStatus.Active;
                    RecipeActivated?.Invoke(this, recipe);
                }
            }
        }

        public Dictionary<string, object> ApplyRecipeToContext(Recipe recipe)
        {
            var context = new Dictionary<string, object>();
            foreach (var param in recipe.Parameters)
                context[param.Name] = param.Value ?? param.DefaultValue ?? string.Empty;
            return context;
        }

        public List<Recipe> GetAllRecipes()
        {
            lock (_lock) { return _recipes.Values.ToList(); }
        }
    }
}
