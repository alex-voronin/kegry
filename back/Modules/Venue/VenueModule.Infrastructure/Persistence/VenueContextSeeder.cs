using Microsoft.EntityFrameworkCore;
using SharedCore.Extensions;
using VenueModule.Domain.Aggregates;
using VenueModule.Domain.ValueObjects;

namespace VenueModule.Infrastructure.Persistence;

public static class VenueContextSeeder
{
    public static void SeedTags(ModelBuilder modelBuilder)
    {
        var tags = new List<Tag>();

        void AddTags(TagCategory category, string[] values, bool isSingleSelect)
        {
            var sortOrder = 0;
            foreach (var value in values)
            {
                tags.Add(new Tag
                {
                    Id = value.ToDeterministicGuid(),
                    Value = value,
                    Category = category,
                    IsSingleSelect = isSingleSelect,
                    SortOrder = sortOrder++,
                    ParentId = null
                });
            }
        }

        void AddHierarchicalTags(TagCategory category, Dictionary<string, string[]> hierarchy, bool isSingleSelect)
        {
            var sortOrder = 0;
            foreach (var parent in hierarchy.Keys)
            {
                var parentId = parent.ToDeterministicGuid();
                tags.Add(new Tag
                {
                    Id = parentId,
                    Value = parent,
                    Category = category,
                    IsSingleSelect = isSingleSelect,
                    SortOrder = sortOrder++,
                    ParentId = null
                });

                foreach (var child in hierarchy[parent])
                {
                    tags.Add(new Tag
                    {
                        Id = child.ToDeterministicGuid(),
                        Value = child,
                        Category = category,
                        IsSingleSelect = isSingleSelect,
                        SortOrder = sortOrder++,
                        ParentId = parentId
                    });
                }
            }
        }

        AddHierarchicalTags(TagCategory.DrinkCategory, new Dictionary<string, string[]>
        {
            { "beer", ["beer_craft", "beer_lager", "beer_ipa", "beer_stout", "beer_sour", "beer_wheat", "beer_fruit"] },
            { "cider", ["cider_apple", "cider_pear", "cider_dry", "cider_sweet"] },
            { "wine", ["wine_red", "wine_white", "wine_orange", "wine_sparkling"] },
            {
                "spirit", [
                    "spirit_vodka", "spirit_whiskey", "spirit_gin", "spirit_rum", "spirit_brandy", "spirit_liqueur",
                    "spirit_tincture", "spirit_tequila", "spirit_mezcal", "spirit_absinthe"
                ]
            },
            { "cocktail", ["cocktail_classic", "cocktail_signature", "cocktail_spritz"] },
            { "non_alcoholic", ["coffee", "tea", "beverages"] }
        }, isSingleSelect: false);

        AddTags(TagCategory.HospitalityAndRules, [
            "pet_friendly", "kids_friendly", "coworking_friendly"
        ], isSingleSelect: false);

        AddTags(TagCategory.FoodAndKitchen, [
            "kitchen_hot_meals", "kitchen_snacks", "kitchen_external_allowed", "vegan_options"
        ], isSingleSelect: false);

        AddTags(TagCategory.InfrastructureAndAmenities, [
            "terrace_summer", "terrace_all_year", "parking_available", "bike_rack", "rooftop"
        ], isSingleSelect: false);

        AddTags(TagCategory.SmokingPolicy, [
            "smoking_allowed_inside", "smoking_allowed_outside_only", "smoking_totally_forbidden",
            "smoking_dedicated_room_inside"
        ], isSingleSelect: true);

        AddTags(TagCategory.PriceLevel, [
            "price_budget", "price_average", "price_premium"
        ], isSingleSelect: true);

        modelBuilder.Entity<Tag>().HasData(tags);
    }
}