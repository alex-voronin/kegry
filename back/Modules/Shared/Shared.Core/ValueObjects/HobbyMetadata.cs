namespace SharedCore.ValueObjects;

public static class HobbyMetadata
{
    static HobbyMetadata()
    {
        var missing = Enum.GetValues<Hobby>()
            .Except(Categories.Keys)
            .ToList();

        if (missing.Count != 0)
        {
            throw new InvalidOperationException(
                $"Missing category mapping for: {string.Join(", ", missing)}");
        }
    }

    private static readonly IReadOnlyDictionary<Hobby, HobbyCategory> Categories =
        new Dictionary<Hobby, HobbyCategory>
        {
            // sport & fitness
            { Hobby.Gym, HobbyCategory.SportAndFitness },
            { Hobby.Running, HobbyCategory.SportAndFitness },
            { Hobby.Swimming, HobbyCategory.SportAndFitness },
            { Hobby.Cycling, HobbyCategory.SportAndFitness },
            { Hobby.Yoga, HobbyCategory.SportAndFitness },
            { Hobby.Boxing, HobbyCategory.SportAndFitness },
            { Hobby.Football, HobbyCategory.SportAndFitness },
            { Hobby.Basketball, HobbyCategory.SportAndFitness },
            { Hobby.Volleyball, HobbyCategory.SportAndFitness },
            { Hobby.Tennis, HobbyCategory.SportAndFitness },
            { Hobby.Crossfit, HobbyCategory.SportAndFitness },
            { Hobby.Pilates, HobbyCategory.SportAndFitness },
            { Hobby.Skating, HobbyCategory.SportAndFitness },
            { Hobby.Surfing, HobbyCategory.SportAndFitness },
            { Hobby.MartialArts, HobbyCategory.SportAndFitness },
            { Hobby.Pickleball, HobbyCategory.SportAndFitness },
            { Hobby.Meditation, HobbyCategory.SportAndFitness },
            { Hobby.Stretching, HobbyCategory.SportAndFitness },
            { Hobby.Powerlifting, HobbyCategory.SportAndFitness },
            { Hobby.Bodybuilding, HobbyCategory.SportAndFitness },
            { Hobby.TableTennis, HobbyCategory.SportAndFitness },
            { Hobby.Golf, HobbyCategory.SportAndFitness },
            { Hobby.Triathlon, HobbyCategory.SportAndFitness },

            // active leisure
            { Hobby.Camping, HobbyCategory.ActiveLeisure },
            { Hobby.Hiking, HobbyCategory.ActiveLeisure },
            { Hobby.Fishing, HobbyCategory.ActiveLeisure },
            { Hobby.Hunting, HobbyCategory.ActiveLeisure },
            { Hobby.Skiing, HobbyCategory.ActiveLeisure },
            { Hobby.Snowboarding, HobbyCategory.ActiveLeisure },
            { Hobby.Traveling, HobbyCategory.ActiveLeisure },
            { Hobby.Climbing, HobbyCategory.ActiveLeisure },
            { Hobby.Sailing, HobbyCategory.ActiveLeisure },
            { Hobby.RoadTrips, HobbyCategory.ActiveLeisure },
            { Hobby.Picnics, HobbyCategory.ActiveLeisure },
            { Hobby.Kayaking, HobbyCategory.ActiveLeisure },
            { Hobby.Paddleboarding, HobbyCategory.ActiveLeisure },
            { Hobby.Backpacking, HobbyCategory.ActiveLeisure },
            { Hobby.ScubaDiving, HobbyCategory.ActiveLeisure },

            // food & drinks
            { Hobby.Cooking, HobbyCategory.FoodAndDrinks },
            { Hobby.Baking, HobbyCategory.FoodAndDrinks },
            { Hobby.Restaurants, HobbyCategory.FoodAndDrinks },
            { Hobby.Vegetarianism, HobbyCategory.FoodAndDrinks },
            { Hobby.StreetFood, HobbyCategory.FoodAndDrinks },
            { Hobby.Pastry, HobbyCategory.FoodAndDrinks },
            { Hobby.Bbq, HobbyCategory.FoodAndDrinks },
            { Hobby.Seafood, HobbyCategory.FoodAndDrinks },
            { Hobby.Desserts, HobbyCategory.FoodAndDrinks },
            { Hobby.SocialDrinking, HobbyCategory.FoodAndDrinks },
            { Hobby.Coffee, HobbyCategory.FoodAndDrinks },
            { Hobby.Tea, HobbyCategory.FoodAndDrinks },

            // culture
            { Hobby.Reading, HobbyCategory.Culture },
            { Hobby.Drawing, HobbyCategory.Culture },
            { Hobby.Photography, HobbyCategory.Culture },
            { Hobby.Dancing, HobbyCategory.Culture },
            { Hobby.Painting, HobbyCategory.Culture },
            { Hobby.Singing, HobbyCategory.Culture },
            { Hobby.MusicalInstruments, HobbyCategory.Culture },
            { Hobby.Music, HobbyCategory.Culture },
            { Hobby.Writing, HobbyCategory.Culture },
            { Hobby.Design, HobbyCategory.Culture },
            { Hobby.Fashion, HobbyCategory.Culture },
            { Hobby.Tattoos, HobbyCategory.Culture },
            { Hobby.Movies, HobbyCategory.Culture },
            { Hobby.Animation, HobbyCategory.Culture },
            { Hobby.Podcasts, HobbyCategory.Culture },
            { Hobby.Theatre, HobbyCategory.Culture },
            { Hobby.Blogging, HobbyCategory.Culture },

            // geek stuff
            { Hobby.Gaming, HobbyCategory.GeekStuff },
            { Hobby.BoardGames, HobbyCategory.GeekStuff },
            { Hobby.Chess, HobbyCategory.GeekStuff },
            { Hobby.ItAndComputerScience, HobbyCategory.GeekStuff },
            { Hobby.Anime, HobbyCategory.GeekStuff },
            { Hobby.Business, HobbyCategory.GeekStuff },
            { Hobby.Puzzles, HobbyCategory.GeekStuff },
            { Hobby.LanguageLearning, HobbyCategory.GeekStuff },
            { Hobby.Collecting, HobbyCategory.GeekStuff },
            { Hobby.Astronomy, HobbyCategory.GeekStuff },
            { Hobby.Comics, HobbyCategory.GeekStuff },

            // vehicles
            { Hobby.Cars, HobbyCategory.Vehicles },
            { Hobby.Motorcycles, HobbyCategory.Vehicles },

            // pets
            { Hobby.Dogs, HobbyCategory.Pets },
            { Hobby.Cats, HobbyCategory.Pets },
            { Hobby.Reptiles, HobbyCategory.Pets },
            { Hobby.Birds, HobbyCategory.Pets },
            { Hobby.Aquariums, HobbyCategory.Pets },

            // crafts
            { Hobby.Gardening, HobbyCategory.Crafts },
            { Hobby.Knitting, HobbyCategory.Crafts },
            { Hobby.Sewing, HobbyCategory.Crafts },
            { Hobby.ThreeDPrinting, HobbyCategory.Crafts },
            { Hobby.Diy, HobbyCategory.Crafts }
        };

    public static HobbyCategory GetCategory(Hobby hobby)
        => Categories.TryGetValue(hobby, out var category)
            ? category
            : throw new InvalidOperationException($"Category not configured for {hobby}");

    public static IReadOnlyCollection<Hobby> All =>
        Enum.GetValues<Hobby>();
}