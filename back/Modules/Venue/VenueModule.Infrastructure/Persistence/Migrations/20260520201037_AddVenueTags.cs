using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VenueModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVenueTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                schema: "venue",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_single_select = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_tags_tags_parent_id",
                        column: x => x.parent_id,
                        principalSchema: "venue",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "venue_tag_links",
                schema: "venue",
                columns: table => new
                {
                    venue_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_venue_tag_links", x => new { x.venue_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_venue_tag_links_tags_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "venue",
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_venue_tag_links_venues_venue_id",
                        column: x => x.venue_id,
                        principalSchema: "venue",
                        principalTable: "venues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "venue",
                table: "tags",
                columns: new[] { "id", "category", "is_single_select", "parent_id", "sort_order", "value" },
                values: new object[,]
                {
                    { new Guid("2a33b3e7-f921-3c84-4871-5e9bef7431f4"), "PriceLevel", true, null, 0, "price_budget" },
                    { new Guid("36e36a69-4628-0015-15c3-2de06813cfdd"), "FoodAndKitchen", false, null, 2, "kitchen_external_allowed" },
                    { new Guid("40d55709-2f76-c4c6-5fa2-b90eaadb2e1b"), "DrinkCategory", false, null, 33, "non_alcoholic" },
                    { new Guid("45f17384-9654-92d1-2daa-ceebeb28c314"), "DrinkCategory", false, null, 8, "cider" },
                    { new Guid("47c9c781-62cc-5559-3681-0400989871de"), "FoodAndKitchen", false, null, 3, "vegan_options" },
                    { new Guid("4bc7f567-0332-5a72-180e-cf791e13af47"), "SmokingPolicy", true, null, 0, "smoking_allowed_inside" },
                    { new Guid("60162d41-3159-84f1-7bbe-ee546bde7603"), "InfrastructureAndAmenities", false, null, 3, "bike_rack" },
                    { new Guid("64a4464a-1318-b53f-0b94-8fa4e161fea0"), "PriceLevel", true, null, 1, "price_average" },
                    { new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), "DrinkCategory", false, null, 18, "spirit" },
                    { new Guid("76e12ab0-f701-659e-b323-61744e59ad7d"), "InfrastructureAndAmenities", false, null, 0, "terrace_summer" },
                    { new Guid("799d1dad-1b9e-058b-134a-846574a6bf0f"), "HospitalityAndRules", false, null, 2, "coworking_friendly" },
                    { new Guid("7e07b343-2cea-b4a2-57d4-8dabded0ac03"), "PriceLevel", true, null, 2, "price_premium" },
                    { new Guid("931e314f-23a7-bd93-015e-6f26569ed5f1"), "SmokingPolicy", true, null, 1, "smoking_allowed_outside_only" },
                    { new Guid("963d1448-9929-bbb4-b460-563e7fcf9097"), "SmokingPolicy", true, null, 2, "smoking_totally_forbidden" },
                    { new Guid("aabb8fb7-aa7b-4d04-6f66-8fab78deb886"), "InfrastructureAndAmenities", false, null, 2, "parking_available" },
                    { new Guid("acb2386a-23fd-abc9-a854-caa70ecdccd6"), "SmokingPolicy", true, null, 3, "smoking_dedicated_room_inside" },
                    { new Guid("acc92cdb-3700-7c11-d9fa-98239fc95074"), "HospitalityAndRules", false, null, 1, "kids_friendly" },
                    { new Guid("ad6267a1-7912-6697-e66d-5511084294c4"), "DrinkCategory", false, null, 29, "cocktail" },
                    { new Guid("adc84cbe-6146-51b5-9e16-6ddbe1469fc2"), "InfrastructureAndAmenities", false, null, 1, "terrace_all_year" },
                    { new Guid("d070ff2e-7147-9139-89b4-1ee7f033fe32"), "HospitalityAndRules", false, null, 0, "pet_friendly" },
                    { new Guid("d1bc1266-b3b8-1fc1-01b0-2687fa96af1a"), "InfrastructureAndAmenities", false, null, 4, "rooftop" },
                    { new Guid("d26b203f-fbd4-6f8b-bef3-e79e1f6c3964"), "DrinkCategory", false, null, 13, "wine" },
                    { new Guid("e2ba1c26-e2ac-3757-980f-42940dcece80"), "FoodAndKitchen", false, null, 1, "kitchen_snacks" },
                    { new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), "DrinkCategory", false, null, 0, "beer" },
                    { new Guid("fe89dde6-e66a-f301-db22-f37818086245"), "FoodAndKitchen", false, null, 0, "kitchen_hot_meals" },
                    { new Guid("00b3710c-f045-372d-0ffc-46349a2d8e1e"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 20, "spirit_whiskey" },
                    { new Guid("03b559b2-4048-a646-1d3a-4e056423fbae"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 4, "beer_stout" },
                    { new Guid("127c17bc-4b5f-40f7-53cc-94aa02f540ce"), "DrinkCategory", false, new Guid("d26b203f-fbd4-6f8b-bef3-e79e1f6c3964"), 15, "wine_white" },
                    { new Guid("1668bacf-91b1-3ab3-d708-19c25e1cf544"), "DrinkCategory", false, new Guid("45f17384-9654-92d1-2daa-ceebeb28c314"), 11, "cider_dry" },
                    { new Guid("1f42dbd8-6003-e581-23f2-2de658059118"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 19, "spirit_vodka" },
                    { new Guid("21c5cdaf-1028-3e4d-3964-b181a5fa710f"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 5, "beer_sour" },
                    { new Guid("2bea3972-c95d-f643-1f3c-0a0276c20974"), "DrinkCategory", false, new Guid("40d55709-2f76-c4c6-5fa2-b90eaadb2e1b"), 35, "tea" },
                    { new Guid("2cad8987-d4dd-95d7-ca04-bd210e22c556"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 26, "spirit_tequila" },
                    { new Guid("37235d80-38ce-2aa1-73eb-cdb18b3c46ca"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 3, "beer_ipa" },
                    { new Guid("39335e9e-2828-1c7a-82d3-f06f9468debc"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 1, "beer_craft" },
                    { new Guid("47f77f8e-421c-6670-df97-f69bf6422a0e"), "DrinkCategory", false, new Guid("45f17384-9654-92d1-2daa-ceebeb28c314"), 10, "cider_pear" },
                    { new Guid("887cadfc-be69-d15d-c432-5b88b2f4f44e"), "DrinkCategory", false, new Guid("ad6267a1-7912-6697-e66d-5511084294c4"), 30, "cocktail_classic" },
                    { new Guid("8ab8486c-8aef-5f7b-b14a-ce980c610a5e"), "DrinkCategory", false, new Guid("ad6267a1-7912-6697-e66d-5511084294c4"), 32, "cocktail_spritz" },
                    { new Guid("8db0f02a-878e-0d33-c9f0-853fba91f84c"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 24, "spirit_liqueur" },
                    { new Guid("a58f9276-3431-2a3c-9c4f-f0a83ce3a27c"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 7, "beer_fruit" },
                    { new Guid("a85e5935-5b84-ae58-8fa0-2f17a77d97c7"), "DrinkCategory", false, new Guid("ad6267a1-7912-6697-e66d-5511084294c4"), 31, "cocktail_signature" },
                    { new Guid("b0e232f4-d185-3c02-ad47-2787ce730b50"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 25, "spirit_tincture" },
                    { new Guid("c33d69eb-96e0-bb66-339b-e2c419d0478e"), "DrinkCategory", false, new Guid("40d55709-2f76-c4c6-5fa2-b90eaadb2e1b"), 36, "beverages" },
                    { new Guid("cc4a9c7e-01ae-af84-8a63-d8f5361b6bef"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 28, "spirit_absinthe" },
                    { new Guid("d105eb24-1883-2dac-b8b2-b959315d10f2"), "DrinkCategory", false, new Guid("40d55709-2f76-c4c6-5fa2-b90eaadb2e1b"), 34, "coffee" },
                    { new Guid("d6ec4ee0-c3e0-adb7-edd3-e75230c8a7cc"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 23, "spirit_brandy" },
                    { new Guid("d751e693-eb3e-827f-0146-f6b873035cf6"), "DrinkCategory", false, new Guid("45f17384-9654-92d1-2daa-ceebeb28c314"), 9, "cider_apple" },
                    { new Guid("dafb091b-0015-5bc3-3edb-55d09f525462"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 22, "spirit_rum" },
                    { new Guid("de9232e9-1b5f-577a-a52e-a514acf33455"), "DrinkCategory", false, new Guid("d26b203f-fbd4-6f8b-bef3-e79e1f6c3964"), 16, "wine_orange" },
                    { new Guid("e166a0a5-df7e-7eed-c0c7-a16417f0b62f"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 2, "beer_lager" },
                    { new Guid("e53a063a-cb94-ca3f-9651-eede9936df9a"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 21, "spirit_gin" },
                    { new Guid("e56242a5-a42b-3b6c-f27e-4d4a02dd0957"), "DrinkCategory", false, new Guid("d26b203f-fbd4-6f8b-bef3-e79e1f6c3964"), 14, "wine_red" },
                    { new Guid("e8db0631-35b1-2514-08a7-3d4215fa0bf5"), "DrinkCategory", false, new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), 6, "beer_wheat" },
                    { new Guid("edf94ae1-e9c3-d56c-8fa6-51dcbb077db1"), "DrinkCategory", false, new Guid("45f17384-9654-92d1-2daa-ceebeb28c314"), 12, "cider_sweet" },
                    { new Guid("edfa534e-8347-8d14-0bfc-c2c6ffe6cfb2"), "DrinkCategory", false, new Guid("d26b203f-fbd4-6f8b-bef3-e79e1f6c3964"), 17, "wine_sparkling" },
                    { new Guid("f4317d8b-e073-675b-260c-ce60fc3d7600"), "DrinkCategory", false, new Guid("718a8607-476d-d712-771f-6414d2b8bf8a"), 27, "spirit_mezcal" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_tags_category",
                schema: "venue",
                table: "tags",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_tags_parent_id",
                schema: "venue",
                table: "tags",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_tags_value",
                schema: "venue",
                table: "tags",
                column: "value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_venue_tag_links_tag_id",
                schema: "venue",
                table: "venue_tag_links",
                column: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "venue_tag_links",
                schema: "venue");

            migrationBuilder.DropTable(
                name: "tags",
                schema: "venue");
        }
    }
}
