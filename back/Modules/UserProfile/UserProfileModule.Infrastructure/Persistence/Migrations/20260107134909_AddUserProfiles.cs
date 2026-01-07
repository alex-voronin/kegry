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
                name: "users");

            migrationBuilder.CreateTable(
                name: "hobbies",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hobbies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "profiles",
                schema: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    avatar_image_url = table.Column<string>(type: "text", nullable: false),
                    cover_image_url = table.Column<string>(type: "text", nullable: false),
                    birthday = table.Column<DateOnly>(type: "date", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_profile_hobby_link",
                schema: "users",
                columns: table => new
                {
                    hobbies_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_profiles_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_profile_hobby_link", x => new { x.hobbies_id, x.user_profiles_id });
                    table.ForeignKey(
                        name: "fk_user_profile_hobby_link_hobbies_hobbies_id",
                        column: x => x.hobbies_id,
                        principalSchema: "users",
                        principalTable: "hobbies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_profile_hobby_link_profiles_user_profiles_id",
                        column: x => x.user_profiles_id,
                        principalSchema: "users",
                        principalTable: "profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "users",
                table: "hobbies",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { new Guid("0a8c471e-2262-afd2-c393-099f413bce5f"), "skating" },
                    { new Guid("0ab153f8-3af7-a404-c8b3-5b2ade1f40b9"), "swimming" },
                    { new Guid("0dc68446-71dc-39bc-3a1b-f007f5452372"), "boxing" },
                    { new Guid("131fd8e4-6ff9-bcb9-905f-4ad89615032b"), "gym" },
                    { new Guid("16cdc1f1-9738-21b0-c5b4-6086b9c500e4"), "boardgames" },
                    { new Guid("178959fb-2868-2202-f627-0e1a260c3637"), "hunting" },
                    { new Guid("1a6bd7fc-be9e-f33b-4c70-690e4f4483a8"), "streetfood" },
                    { new Guid("1abf6aae-2ca0-cf01-e560-b9315db66b07"), "crossfit" },
                    { new Guid("1b2691a8-c043-5450-98f6-440259f77a93"), "picnics" },
                    { new Guid("1d1b7c6a-8c58-1dd5-12ad-44324d614fed"), "painting" },
                    { new Guid("25becf49-c27f-4145-07e5-b4daf9f83e13"), "restaurants" },
                    { new Guid("2bea3972-c95d-f643-1f3c-0a0276c20974"), "tea" },
                    { new Guid("2ffa67d5-2ec3-f78e-b736-c121e4975689"), "baking" },
                    { new Guid("33641eaa-6ddc-c48c-c475-849cca2b3ce8"), "traveling" },
                    { new Guid("33b2bf1f-e351-0658-5139-5ab721f5e935"), "tennis" },
                    { new Guid("3a1f9b73-d29b-f3a0-5f27-52d367af3107"), "cooking" },
                    { new Guid("3a3375cb-83f2-d70e-595e-28f03843e149"), "singing" },
                    { new Guid("3e1c4476-d3cf-5f25-a178-fabe13864161"), "skiing" },
                    { new Guid("4553ea1c-84d2-62f3-45a9-4301b114b27c"), "writing" },
                    { new Guid("45f17384-9654-92d1-2daa-ceebeb28c314"), "cider" },
                    { new Guid("473fc131-87ad-7bdd-aa2d-558a91e0fbb9"), "design" },
                    { new Guid("4d914b4f-b36d-195e-101f-f003c4e7ea3a"), "fashion" },
                    { new Guid("4f126ae6-ab9c-19d3-8d84-dd68c8c87cf7"), "cars" },
                    { new Guid("519f19d0-72d2-b68d-0119-45145a1b607a"), "basketball" },
                    { new Guid("5472201b-fb7a-e475-9f33-b016751ed360"), "photography" },
                    { new Guid("579a6df0-84b0-7776-e36f-163a7b7fe54a"), "pastry" },
                    { new Guid("5d9828a9-0e77-3227-0844-4bc6cb6bacb9"), "pilates" },
                    { new Guid("6287ebd8-7694-8c87-b0ec-2349ae109cde"), "drawing" },
                    { new Guid("720fa156-d1b2-6e8d-5438-5a6297b33349"), "vegetarianism" },
                    { new Guid("78627e38-e0d8-8360-d813-358762e0ac63"), "anime" },
                    { new Guid("7a1f1f61-c90b-f543-eee6-b2411f21be3b"), "dancing" },
                    { new Guid("7f20fdeb-e82b-cd73-8d53-671238b95d00"), "whiskey" },
                    { new Guid("8328dfa4-2274-e696-31ad-204732ddc404"), "roadtrips" },
                    { new Guid("8329c7a6-a0f8-02a0-155d-67b12b345629"), "gin" },
                    { new Guid("86941065-74fd-ca35-e5d9-3d942f6f37de"), "snowboarding" },
                    { new Guid("86e3d53d-02dd-bf00-58f7-5ee4f8355760"), "bikes" },
                    { new Guid("8cb61354-228a-8c04-a8ee-dd20905ecdb4"), "martialarts" },
                    { new Guid("8df416b0-8c89-5b74-e5ef-382254224582"), "chess" },
                    { new Guid("8e670e19-b413-d61d-757f-4de34946ff87"), "cycling" },
                    { new Guid("8fe21fe0-d16c-0320-e229-8024321d97d7"), "bbq" },
                    { new Guid("9026a059-03df-8e8e-9547-551cae1e7fa6"), "sweettooth" },
                    { new Guid("9218b5ec-868f-7dc3-fed1-d01fe1d96c88"), "sailing" },
                    { new Guid("9976d618-2619-d06c-bd6c-d78aa405d5d0"), "music" },
                    { new Guid("9cdea6ae-eeba-279d-04dc-f81f4a194991"), "sushi" },
                    { new Guid("9f817691-a824-e32a-7365-73fbddc56c9c"), "musicalinstruments" },
                    { new Guid("a436df38-2318-7ace-fca5-78d3189dbd8f"), "camping" },
                    { new Guid("ad1176f5-3fd4-fb15-aa6b-d04331da3019"), "seafood" },
                    { new Guid("af079a81-aba4-66c8-0ec9-042038a8c597"), "fishing" },
                    { new Guid("b11f1191-b0f8-4388-8d80-367df81cb6cf"), "volleyball" },
                    { new Guid("ba904fb9-b3c6-7b87-0ca3-b87faba854de"), "surfing" },
                    { new Guid("c544ba55-d348-afeb-d9f7-0e64a7f232b0"), "movies" },
                    { new Guid("ccae4e23-fd0e-6044-dfeb-921a50feea08"), "vodka" },
                    { new Guid("cd1d1075-88fc-5b45-cafc-9e53e0b06689"), "running" },
                    { new Guid("cd597680-3f88-a6c0-3f6c-e615893b3558"), "yoga" },
                    { new Guid("d105eb24-1883-2dac-b8b2-b959315d10f2"), "coffee" },
                    { new Guid("d1bb4b38-919b-8641-7cba-a5317b3fc635"), "climbing" },
                    { new Guid("d26b203f-fbd4-6f8b-bef3-e79e1f6c3964"), "wine" },
                    { new Guid("d8e2b437-0029-e9d5-4b8d-a524fbeb33c0"), "football" },
                    { new Guid("e5ef9655-583a-58fe-c28c-1757ef095b23"), "champagne" },
                    { new Guid("e84f4aea-d67f-c8f5-afb8-779518bc2162"), "hiking" },
                    { new Guid("ebf27e96-6334-a44b-18db-94dab610ba6f"), "gaming" },
                    { new Guid("ed251c6f-2315-2f96-1bbf-9dee9be5092b"), "animation" },
                    { new Guid("edfc980d-9520-f9bc-4be8-3f87521cc4eb"), "itandcomputerscience" },
                    { new Guid("f439ef1a-bd7c-e18a-2674-c2ca82700170"), "busyness" },
                    { new Guid("f567183e-e8ae-4530-775f-be355e6a3ce1"), "beer" },
                    { new Guid("f7449c65-30a2-11de-d35d-5532707cf1f5"), "tattoos" },
                    { new Guid("f9c8caa8-714e-c83c-5d51-6a6e94112418"), "cocktails" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_hobbies_name",
                schema: "users",
                table: "hobbies",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_profile_hobby_link_user_profiles_id",
                schema: "users",
                table: "user_profile_hobby_link",
                column: "user_profiles_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profile_hobby_link",
                schema: "users");

            migrationBuilder.DropTable(
                name: "hobbies",
                schema: "users");

            migrationBuilder.DropTable(
                name: "profiles",
                schema: "users");
        }
    }
}
