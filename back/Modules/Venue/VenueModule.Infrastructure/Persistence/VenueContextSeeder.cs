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

        AddTags(TagCategory.VenueStyle, [
            "cocktail_bar", "wine_bar", "craft_beer_bar", "sports_bar", "speakeasy", "dive_bar", "taproom"
        ], isSingleSelect: false);

        AddHierarchicalTags(TagCategory.DrinkCategory, new Dictionary<string, string[]>
        {
            { "beer", ["lager", "ipa", "stout", "sour_beer", "wheat_beer"] },
            { "wine", ["red_wine", "white_wine", "orange_wine", "sparkling_wine"] },
            { "spirits", ["vodka", "whiskey", "gin", "rum", "tequila", "mezcal", "brandy"] },
            { "cocktails", ["classic_cocktails", "signature_cocktails", "spritz"] },
            { "other", ["cider", "mead", "sake", "coffee", "tea", "non_alcoholic_options"] }
        }, isSingleSelect: false);

        AddTags(TagCategory.Specialty, [
            "craft_beer", "wine", "agave_spirits", "whiskey", "classic_cocktails", "signature_cocktails"
        ], isSingleSelect: false);

        AddTags(TagCategory.HospitalityAndRules, [
            "pet_friendly", "kids_friendly", "coworking_friendly"
        ], isSingleSelect: false);

        AddTags(TagCategory.FoodAndKitchen, [
            "kitchen_full", "kitchen_snacks_only", "kitchen_external_allowed", "vegan_options"
        ], isSingleSelect: false);

        AddTags(TagCategory.InfrastructureAndAmenities, [
            "terrace_summer", "terrace_all_year", "parking_available", "bike_rack", "rooftop"
        ], isSingleSelect: false);

        AddTags(TagCategory.SmokingPolicy, [
            "smoking_allowed_inside", "smoking_allowed_outside_only", "smoking_totally_forbidden", "smoking_dedicated_room_inside"
        ], isSingleSelect: true);

        AddTags(TagCategory.PriceLevel, [
            "budget", "average", "premium"
        ], isSingleSelect: true);

        modelBuilder.Entity<Tag>().HasData(tags);
    }
}
