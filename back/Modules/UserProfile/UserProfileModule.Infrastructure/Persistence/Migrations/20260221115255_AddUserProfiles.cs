using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace UserProfileModule.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "user_profile");

            migrationBuilder.CreateTable(
                name: "hobby_categories",
                schema: "user_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hobby_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "user_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    avatar_image_s3key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    cover_image_s3key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    birthday = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hobbies",
                schema: "user_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hobbies", x => x.id);
                    table.ForeignKey(
                        name: "fk_hobbies_hobby_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "user_profile",
                        principalTable: "hobby_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_hobby_link",
                schema: "user_profile",
                columns: table => new
                {
                    hobbies_id = table.Column<Guid>(type: "uuid", nullable: false),
                    users_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_hobby_link", x => new { x.hobbies_id, x.users_id });
                    table.ForeignKey(
                        name: "fk_user_hobby_link_hobbies_hobbies_id",
                        column: x => x.hobbies_id,
                        principalSchema: "user_profile",
                        principalTable: "hobbies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_hobby_link_users_users_id",
                        column: x => x.users_id,
                        principalSchema: "user_profile",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "user_profile",
                table: "hobby_categories",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "foodanddrinks" },
                    { new Guid("1041d6d0-56d9-4732-dad8-35d349e9b507"), "pets" },
                    { new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "activeleisure" },
                    { new Guid("8055bb56-9ec0-f3a8-3f72-17873c4736f6"), "vehicles" },
                    { new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "geekstuff" },
                    { new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "culture" },
                    { new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "sportandfitness" },
                    { new Guid("f18e1d84-6c08-94b0-f921-8af74d10490f"), "crafts" }
                });

            migrationBuilder.InsertData(
                schema: "user_profile",
                table: "hobbies",
                columns: new[] { "id", "category_id", "name" },
                values: new object[,]
                {
                    { new Guid("019c6219-c68f-4cdd-433e-cac16e56650d"), new Guid("f18e1d84-6c08-94b0-f921-8af74d10490f"), "knitting" },
                    { new Guid("06a6574a-2306-bd42-ed13-a9f59c536968"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "stretching" },
                    { new Guid("0a8c471e-2262-afd2-c393-099f413bce5f"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "skating" },
                    { new Guid("0ab153f8-3af7-a404-c8b3-5b2ade1f40b9"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "swimming" },
                    { new Guid("0b8c4a38-cef4-8355-14fa-d0f42cd43133"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "bodybuilding" },
                    { new Guid("0dc68446-71dc-39bc-3a1b-f007f5452372"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "boxing" },
                    { new Guid("131fd8e4-6ff9-bcb9-905f-4ad89615032b"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "gym" },
                    { new Guid("16cdc1f1-9738-21b0-c5b4-6086b9c500e4"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "boardgames" },
                    { new Guid("173fc775-855a-e3ce-84f3-2a5608eb67d1"), new Guid("1041d6d0-56d9-4732-dad8-35d349e9b507"), "birds" },
                    { new Guid("178959fb-2868-2202-f627-0e1a260c3637"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "hunting" },
                    { new Guid("1a6bd7fc-be9e-f33b-4c70-690e4f4483a8"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "streetfood" },
                    { new Guid("1abf6aae-2ca0-cf01-e560-b9315db66b07"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "crossfit" },
                    { new Guid("1b2691a8-c043-5450-98f6-440259f77a93"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "picnics" },
                    { new Guid("1d1b7c6a-8c58-1dd5-12ad-44324d614fed"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "painting" },
                    { new Guid("1ddceaec-ce40-61a0-fd59-86f2a109ee93"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "reading" },
                    { new Guid("20c13208-a82d-82d3-318e-329a7c133ea0"), new Guid("1041d6d0-56d9-4732-dad8-35d349e9b507"), "cats" },
                    { new Guid("25becf49-c27f-4145-07e5-b4daf9f83e13"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "restaurants" },
                    { new Guid("27cec153-9c8a-8f19-db49-5b36d4d8723a"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "kayaking" },
                    { new Guid("2b64cfc6-1c8f-11ac-01e2-3a06aa63600e"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "golf" },
                    { new Guid("2bea3972-c95d-f643-1f3c-0a0276c20974"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "tea" },
                    { new Guid("2d69ffa0-30b6-b3ab-0c9e-b40adc3d205a"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "theatre" },
                    { new Guid("2ffa67d5-2ec3-f78e-b736-c121e4975689"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "baking" },
                    { new Guid("304bd710-fbf2-1575-8828-c7fb608d993f"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "triathlon" },
                    { new Guid("33641eaa-6ddc-c48c-c475-849cca2b3ce8"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "traveling" },
                    { new Guid("33b2bf1f-e351-0658-5139-5ab721f5e935"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "tennis" },
                    { new Guid("33bdd4df-28c8-410e-8ea9-95b7c4f2485c"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "pickleball" },
                    { new Guid("351beada-f1e5-09ba-fcd4-3cbba71e2d11"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "collecting" },
                    { new Guid("352d8dd2-fa60-f076-dbb1-a452f8c38169"), new Guid("1041d6d0-56d9-4732-dad8-35d349e9b507"), "dogs" },
                    { new Guid("3766cd6b-fe91-800e-cdbc-7f83b7cbf69d"), new Guid("f18e1d84-6c08-94b0-f921-8af74d10490f"), "gardening" },
                    { new Guid("3a1f9b73-d29b-f3a0-5f27-52d367af3107"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "cooking" },
                    { new Guid("3a3375cb-83f2-d70e-595e-28f03843e149"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "singing" },
                    { new Guid("3e1c4476-d3cf-5f25-a178-fabe13864161"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "skiing" },
                    { new Guid("42f3f8b7-5cd7-6902-9b78-73f497f4251c"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "desserts" },
                    { new Guid("4553ea1c-84d2-62f3-45a9-4301b114b27c"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "writing" },
                    { new Guid("473fc131-87ad-7bdd-aa2d-558a91e0fbb9"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "design" },
                    { new Guid("4d914b4f-b36d-195e-101f-f003c4e7ea3a"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "fashion" },
                    { new Guid("4f126ae6-ab9c-19d3-8d84-dd68c8c87cf7"), new Guid("8055bb56-9ec0-f3a8-3f72-17873c4736f6"), "cars" },
                    { new Guid("519f19d0-72d2-b68d-0119-45145a1b607a"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "basketball" },
                    { new Guid("53e2d7f5-c92c-16ad-bc2a-41222d76f269"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "business" },
                    { new Guid("5472201b-fb7a-e475-9f33-b016751ed360"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "photography" },
                    { new Guid("579a6df0-84b0-7776-e36f-163a7b7fe54a"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "pastry" },
                    { new Guid("5d9828a9-0e77-3227-0844-4bc6cb6bacb9"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "pilates" },
                    { new Guid("5da4c4c8-caa1-d38a-c5ed-b8e6edeae28b"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "blogging" },
                    { new Guid("5efe1d6e-f12b-80d4-c326-f39fe8a224d0"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "tabletennis" },
                    { new Guid("60fde59d-92b7-6794-7fa4-dc3398ee6ec7"), new Guid("f18e1d84-6c08-94b0-f921-8af74d10490f"), "threedprinting" },
                    { new Guid("6287ebd8-7694-8c87-b0ec-2349ae109cde"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "drawing" },
                    { new Guid("65320545-3f72-bd59-84cf-4af2b15def8e"), new Guid("1041d6d0-56d9-4732-dad8-35d349e9b507"), "reptiles" },
                    { new Guid("720fa156-d1b2-6e8d-5438-5a6297b33349"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "vegetarianism" },
                    { new Guid("725ee127-028c-5985-14e5-eb19147f2b75"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "astronomy" },
                    { new Guid("76ea4f16-4ed4-2283-7a64-83ecc8d36349"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "socialdrinking" },
                    { new Guid("78627e38-e0d8-8360-d813-358762e0ac63"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "anime" },
                    { new Guid("7a1f1f61-c90b-f543-eee6-b2411f21be3b"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "dancing" },
                    { new Guid("7a63af51-60ed-1d4c-5376-026ae625111e"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "comics" },
                    { new Guid("818ffa31-37ac-7eaa-25db-20935aa2d11d"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "languagelearning" },
                    { new Guid("8328dfa4-2274-e696-31ad-204732ddc404"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "roadtrips" },
                    { new Guid("86941065-74fd-ca35-e5d9-3d942f6f37de"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "snowboarding" },
                    { new Guid("8cb61354-228a-8c04-a8ee-dd20905ecdb4"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "martialarts" },
                    { new Guid("8df416b0-8c89-5b74-e5ef-382254224582"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "chess" },
                    { new Guid("8e670e19-b413-d61d-757f-4de34946ff87"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "cycling" },
                    { new Guid("8fe21fe0-d16c-0320-e229-8024321d97d7"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "bbq" },
                    { new Guid("90d0fce6-ea5f-e051-05ce-4c318ec7c092"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "backpacking" },
                    { new Guid("9218b5ec-868f-7dc3-fed1-d01fe1d96c88"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "sailing" },
                    { new Guid("989ffdf3-b438-1aed-d60f-b5faca0f7bc6"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "puzzles" },
                    { new Guid("9976d618-2619-d06c-bd6c-d78aa405d5d0"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "music" },
                    { new Guid("9c7d7482-5a1d-7d36-4d22-7b28ed8cf5c6"), new Guid("1041d6d0-56d9-4732-dad8-35d349e9b507"), "aquariums" },
                    { new Guid("9f817691-a824-e32a-7365-73fbddc56c9c"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "musicalinstruments" },
                    { new Guid("a436df38-2318-7ace-fca5-78d3189dbd8f"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "camping" },
                    { new Guid("ace27e8a-fb8c-6fd1-254a-ba3fc32008c2"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "meditation" },
                    { new Guid("ad1176f5-3fd4-fb15-aa6b-d04331da3019"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "seafood" },
                    { new Guid("af079a81-aba4-66c8-0ec9-042038a8c597"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "fishing" },
                    { new Guid("b11f1191-b0f8-4388-8d80-367df81cb6cf"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "volleyball" },
                    { new Guid("b4558a63-9d6d-dafd-9c9c-92bdeb124c36"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "paddleboarding" },
                    { new Guid("ba904fb9-b3c6-7b87-0ca3-b87faba854de"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "surfing" },
                    { new Guid("c4cd7c41-6aad-4cd8-f301-dbfa2c4984c3"), new Guid("f18e1d84-6c08-94b0-f921-8af74d10490f"), "diy" },
                    { new Guid("c544ba55-d348-afeb-d9f7-0e64a7f232b0"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "movies" },
                    { new Guid("c99f6145-92cc-b247-8d24-7676d6c0ba37"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "podcasts" },
                    { new Guid("ca9bb4d3-c4c8-4e6d-5413-02aaacaebd50"), new Guid("8055bb56-9ec0-f3a8-3f72-17873c4736f6"), "motorcycles" },
                    { new Guid("cd1d1075-88fc-5b45-cafc-9e53e0b06689"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "running" },
                    { new Guid("cd597680-3f88-a6c0-3f6c-e615893b3558"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "yoga" },
                    { new Guid("d105eb24-1883-2dac-b8b2-b959315d10f2"), new Guid("03866a63-9b63-1637-c989-a7c9b0a4b2fa"), "coffee" },
                    { new Guid("d1bb4b38-919b-8641-7cba-a5317b3fc635"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "climbing" },
                    { new Guid("d8e2b437-0029-e9d5-4b8d-a524fbeb33c0"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "football" },
                    { new Guid("dce2a53c-6d74-25e1-69bf-92b86049b555"), new Guid("f18e1d84-6c08-94b0-f921-8af74d10490f"), "sewing" },
                    { new Guid("e84f4aea-d67f-c8f5-afb8-779518bc2162"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "hiking" },
                    { new Guid("e9432b74-e787-6fb1-6575-81302535d696"), new Guid("dc53b626-364b-da58-2776-b8686e9a5fea"), "powerlifting" },
                    { new Guid("ebf27e96-6334-a44b-18db-94dab610ba6f"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "gaming" },
                    { new Guid("ed251c6f-2315-2f96-1bbf-9dee9be5092b"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "animation" },
                    { new Guid("ed722902-f621-5043-ff2d-ddfb26a4df51"), new Guid("723e5793-7ddf-62ca-e94c-72c3738a85a6"), "scubadiving" },
                    { new Guid("edfc980d-9520-f9bc-4be8-3f87521cc4eb"), new Guid("8b418e1a-9d66-ba82-3465-f54f6b201e3d"), "itandcomputerscience" },
                    { new Guid("f7449c65-30a2-11de-d35d-5532707cf1f5"), new Guid("a839703f-c036-920d-ecf8-7fd7d338c4db"), "tattoos" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_hobbies_category_id",
                schema: "user_profile",
                table: "hobbies",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_hobbies_name",
                schema: "user_profile",
                table: "hobbies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hobby_categories_name",
                schema: "user_profile",
                table: "hobby_categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_hobby_link_users_id",
                schema: "user_profile",
                table: "user_hobby_link",
                column: "users_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_id",
                schema: "user_profile",
                table: "users",
                column: "id",
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_users_is_public",
                schema: "user_profile",
                table: "users",
                column: "is_public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_hobby_link",
                schema: "user_profile");

            migrationBuilder.DropTable(
                name: "hobbies",
                schema: "user_profile");

            migrationBuilder.DropTable(
                name: "users",
                schema: "user_profile");

            migrationBuilder.DropTable(
                name: "hobby_categories",
                schema: "user_profile");
        }
    }
}
